/*
 * Sonar MSBuild Plugin, open source software quality management tool.
 * Author(s) : Jorge Costa @ jmecsoftware.com
 * 
 * Sonar MSBuild Plugin is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * Sonar MSBuild Plugin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 */
package org.sonar.plugins.msbuild.projectchecker;

import org.sonar.api.rule.Severity;
import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.plugins.msbuild.MSBuildLanguage;

public class MSBuildProjectCheckerRulesDefinition implements RulesDefinition {

  public static final String REPOSITORY_KEY = "tekla-msbuild-guideline";
  public static final String REPOSITORY_NAME = "Tekla";
  
  @Override
  public void define(Context context) {
    NewRepository repository = context
      .createRepository(REPOSITORY_KEY, MSBuildLanguage.KEY)
      .setName(REPOSITORY_NAME);

    repository.createRule("MicrosoftCSharpShouldBeIncludeAlways")
            .setName("Microsoft.CSharp.targets in CSharp Projects")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");

    repository.createRule("FullyQualifiedReferenceRule")
            .setName("Fully Qualify the References, Always specify the Version, Culture, PublicKeyToken attributes to make sure they will resolve to the right version of the DLL.")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");
    repository.createRule("AlwaysUserProjectReferencesInSameSolution")
            .setName("Project References Instead of dll reference for projects in same solution")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");

    repository.createRule("EmptyTagsRule")
            .setName("No Empty Tags in Project File")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");  
    repository.createRule("MultiplePrivateSet")
            .setName("Private Flag set more than once of reference")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");
    
    repository.createRule("CannotReadProjectError")
            .setName("Cannot Read Project File Error")
            .setSeverity(Severity.MAJOR)
            .setHtmlDescription("<p></p>");
               
    // msbuild automation rules
    repository.createRule("IncludeFileNotFound")
            .setName("Include path in project file not found, this will cause unecessary compilation")
            .setSeverity(Severity.BLOCKER)
            .setHtmlDescription("<p></p>");
    repository.createRule("IncludeFolderNotUsedDuringInCompilation")
            .setName("Additional Include Path passed to the compiler can be removed")
            .setSeverity(Severity.CRITICAL)
            .setHtmlDescription("<p></p>");
    
    // custom rule 
    repository.createRule("CustomRuleTemplate")
            .setName("Template for custom Custom rules")
            .setTemplate(true)
            .setHtmlDescription("<p>\n" +
"Follow these steps to make your custom Custom rules available in SonarQube:\n" +
"</p>\n" +
"\n" +
"<ol>\n" +
"  <ol>\n" +
"    <li>Create a new rule in SonarQube by \"copying\" this rule template and specify the <code>CheckId</code> of your custom rule, a title, a description, and a default severity.</li>\n" +
"    <li>Enable the newly created rule in your quality profile</li>\n" +
"  </ol>\n" +
"  <li>Relaunch an analysis on your projects, et voilà, your custom rules are executed!</li>\n" +
"</ol>");
        
    repository.done();
  }

}
