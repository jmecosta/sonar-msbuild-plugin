/*
 * Sonar MSBuild Plugin :: Squid
 * Copyright (C) 2015-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
package org.sonar.plugins.msbuild.projectchecker;

import org.sonar.api.server.profile.BuiltInQualityProfilesDefinition;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.sonar.plugins.msbuild.projectchecker.MSBuildProjectCheckerRulesDefinition.REPOSITORY_KEY;

public class MSBuildSonarWayProfile implements BuiltInQualityProfilesDefinition {

  private static void activateRule(NewBuiltInQualityProfile profile, String ruleKey, String severity) {
    NewBuiltInActiveRule rule1 = profile.activateRule(REPOSITORY_KEY, ruleKey);
		rule1.overrideSeverity(severity);
  }

  @Override
  public void define(Context context) {
    NewBuiltInQualityProfile profile = context.createBuiltInQualityProfile("MSBuild Rules", MSBuildLanguage.KEY);
		profile.setDefault(true);
    activateRule(profile, "MicrosoftCSharpShouldBeIncludeAlways", "MAJOR");
    activateRule(profile, "FullyQualifiedReferenceRule", "MAJOR");
    activateRule(profile, "AlwaysUserProjectReferencesInSameSolution", "MAJOR");
		
    activateRule(profile, "EmptyTagsRule", "MAJOR");
		activateRule(profile, "MultiplePrivateSet", "MAJOR");
		activateRule(profile, "CannotReadProjectError", "MAJOR");
		activateRule(profile, "IncludeFileNotFound", "BLOCKER");
		activateRule(profile, "IncludeFolderNotUsedDuringInCompilation", "CRITICAL");
		
    profile.done();    
  }
}
