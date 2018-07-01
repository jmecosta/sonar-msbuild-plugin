module SonarHelpers

open System
open System.IO
open System.Reflection
open VSSonarPlugins
open VSSonarPlugins.Types
open SonarRestService

let execPath = Directory.GetParent(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "")).ToString()

let CreateRulesWithDiagnostic(path : string, profiles : System.Collections.Generic.Dictionary<string, Profile>, rest : ISonarRestService, token : ISonarConfiguration) = 
    let checks = MSBuildHelper.LoadChecksFromPath(path)
    let profile = profiles.["msbuild"]
    let customRule = profile.GetAllRules() |> Seq.find(fun x -> x.Key = "msbuildsquid:CustomRuleTemplate")


    let CreateCustomTemplateRule(diag:ProjectTypes.Rule) = 
        let rule = new Rule()
        rule.Severity <- Severity.MAJOR
        let desc = sprintf """<p>%s</p>""" (diag.Description.ToString())
        let markdown = sprintf """*%s*""" (diag.Description.ToString())
        rule.HtmlDescription <- desc
        rule.MarkDownDescription <- markdown
        rule.Key <- customRule.Repo + ":" + diag.Key
        rule.Name <- diag.Key
        rule.Repo <- customRule.Repo
        let mutable errors = new System.Collections.Generic.List<string>()
        let dic = new System.Collections.Generic.Dictionary<string, string>()
        try
            errors <- rest.CreateRule(token, rule, customRule)
            dic.Add("markdown_description", markdown)
        with
        | ex -> errors.Add("Error: " + ex.Message)

        if errors.Count <> 0 then
            printf "Cannot create rule in server %s due %s\n\r" diag.Key errors.[0]
        else    
            printf "Create Rule %s in %s\n\r" rule.Key customRule.Repo

            let errors = rest.UpdateRule(token, rule.Key, dic)
            if errors.Count <> 0 then
                printf "Failed to update markdown %s due %s\n\r" rule.Key errors.[0]

            let errors = rest.ActivateRule(token, rule.Key, rule.Severity.ToString(), profile.Key)
            if errors <> "" then
                printf "Failed to activate rule %s\r\n" errors
                printf "Delete rule, please be sure to not use built in quality profiles\r\n"
                let errors = rest.DeleteRule(token, rule)
                if errors.Count <> 0 then
                    printf "Failed to detele rule, contact sys admin: %A\r\n" errors
                else
                    printf "Rule deleted"
            else
                printf "Rule Activated\r\n"

    let CreateChecks(check:RuleBase.RuleBase) = 
        check.GetRules |> Seq.iter (fun elem -> if profile.GetRule("msbuildsquid:" + elem.Key) = null then CreateCustomTemplateRule(elem))

    checks |> Seq.iter (fun check -> CreateChecks(check))

let GetProfilesFromServer(projectKey : string,
                          service : ISonarRestService,
                          token : VSSonarPlugins.Types.ISonarConfiguration,
                          loaddisablerules : bool) =

    let profileData : System.Collections.Generic.Dictionary<string, Profile> = new System.Collections.Generic.Dictionary<string, Profile>()
    let profiles = service.GetQualityProfilesForProject(token, new Resource(Key=projectKey))
    let profilesKeys = service.GetProfilesUsingRulesApp(token)

    for profKey in profilesKeys do
        for profile in profiles do
            if profile.Name.Equals(profKey.Name) && profile.Language.Equals(profKey.Language) then
                profile.Key <- profKey.Key

    for profile in profiles do
        try
            System.Diagnostics.Debug.WriteLine("Get Profile: " + profile.Name + " : " + profile.Language)
            service.GetRulesForProfileUsingRulesApp(token, profile, true)

            if loaddisablerules then
                service.GetRulesForProfileUsingRulesApp(token, profile, false)

            profileData.Add(profile.Language, profile)
        with
        | ex ->
            printf "[RoslynRunner] profile failed to load: %s\n\r" ex.Message
            printf "[RoslynRunner] TRACE %s \n\r" ex.StackTrace
             
    profileData

let CreateAndAssignProfileInServer(projectKey : string,
                                   service : ISonarRestService,
                                   token : VSSonarPlugins.Types.ISonarConfiguration) =

    let profileData : System.Collections.Generic.Dictionary<string, Profile> = new System.Collections.Generic.Dictionary<string, Profile>()
    let profiles = service.GetQualityProfilesForProject(token, new Resource(Key=projectKey))
    let profilesFinal : System.Collections.Generic.List<Profile> = null
    let profilesKeys = service.GetProfilesUsingRulesApp(token)

    for profKey in profilesKeys do
        for profile in profiles do
            if profile.Name.Equals(profKey.Name) && profile.Language.Equals(profKey.Language) then
                profile.Key <- profKey.Key

    for profile in profiles do
        let repositoryid = ""
        if profile.Language.Equals("cs") || profile.Language.Equals("vbnet") then
            let parentprofileKey = profile.Key
            let profilenew = profile
            if not(profile.Name.Equals("Complete Roslyn Profile : " + projectKey)) then
                // copy profile
                let newKey = service.CopyProfile(token, profile.Key, "Complete Roslyn Profile : " + projectKey)
                profilenew.Key <- newKey
                profilenew.Name <- "Complete Roslyn Profile"
                profilenew.Language <- profile.Language
                let msg = service.AssignProfileToProject(token, profilenew.Key, projectKey)
                if msg <> "" then
                    printf "Failed to assign profile %s to project %s\r\n" profile.Name projectKey

            // sync rules from rule set
            let repoid = 
                if profilenew.Language.Equals("cs") then
                    "roslyn-cs"
                else
                    "roslyn-vbnet"

            service.GetRulesForProfileUsingRulesApp(token, profilenew, false)

            //for dll in diagnostics do
            //    if dll.Value.Length <> 0 then
            //        for diag in dll.Value do
            //            for supdiag in diag.Analyser.SupportedDiagnostics do
            //                let rule = profilenew.GetRule(repoid + ":" + supdiag.Id)
            //                if rule <> null then
            //                    for msg in service.ActivateRule(token, rule.Key, rule.Severity.ToString(), profilenew.Key) do
            //                        printf "Cannot enable rule %s - %s\r\n" rule.Key msg

            // set parent profile so users can manage the rest of the profile
            let msg = service.ChangeParentProfile(token, profilenew.Key, parentprofileKey)
            if msg <> "Failed change parent: NoContent" then
                printf "Change Profile Parent Failed :  %s\r\n" msg
            
                                                       
    GetProfilesFromServer(projectKey, service, token, false)
                       
let AssignProfileToParent(token : ISonarConfiguration, rest : ISonarRestService, profileKey : string, projectKey : string ) = 
    let parentKey = rest.GetParentProfile(token, profileKey)

    if parentKey <> "" then
        let msg = rest.AssignProfileToProject(token, parentKey, projectKey)
        if msg <> "" then
            printf "Failed to assign profile %s to project %s\r\n" parentKey projectKey
                                               
let DeleteCompleteProfile(token : ISonarConfiguration, rest : ISonarRestService, projectKey : string) =
    let profiles = rest.GetProfilesUsingRulesApp(token)

    for profile in profiles do
        if profile.Name = "Complete Roslyn Profile : " + projectKey then
            AssignProfileToParent(token, rest, profile.Key, projectKey)
            let msg = rest.DeleteProfile(token, profile.Key)
            if msg <> "Failed change parent: NoContent" then
                printf "Delete Profile Parent Failed :  %s\r\n" msg
                                  
let SyncRulesInServer(absPath : string, rest : ISonarRestService, token : ISonarConfiguration, projectKey : string) =
    let profiles = GetProfilesFromServer(projectKey, rest, token, true)
    CreateRulesWithDiagnostic(absPath, profiles, rest, token)

let GetConnectionToken(service : ISonarRestService, address : string , userName : string, password : string) = 
    let token = new VSSonarPlugins.Types.ConnectionConfiguration(address, userName, password, 4.5)
    token.SonarVersion <- float (service.GetServerInfo(token))
    token

let DeleteRoslynRulesInProfiles(service : ISonarRestService, token : VSSonarPlugins.Types.ISonarConfiguration, profile : Profile) = 
    let rules = profile.GetAllRules()
    for rule in rules do
        if rule.Key.StartsWith("msbuildsquid-") && not(rule.IsTemplate) then
            let errors = service.DeleteRule(token, rule)
            for error in errors do
                printf "Cannot Delete Rule: %s\r\n" error
