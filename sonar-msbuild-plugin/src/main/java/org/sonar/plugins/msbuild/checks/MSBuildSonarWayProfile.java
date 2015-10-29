/*
 * Sonar MSBuild Plugin, open source software quality management tool.
 * Author(s) : Jorge Costa
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
package org.sonar.plugins.msbuild.checks;

import org.sonar.api.profiles.AnnotationProfileParser;
import org.sonar.api.profiles.ProfileDefinition;
import org.sonar.api.profiles.RulesProfile;
import org.sonar.api.utils.ValidationMessages;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import org.sonar.plugins.msbuild.checks.CheckRepository;
import org.sonar.plugins.msbuild.MSBuildPlugin;

/**
 * Default XML profile.
 * 
 * @author Matthijs Galesloot
 */
public final class MSBuildSonarWayProfile extends ProfileDefinition {

  private final AnnotationProfileParser annotationProfileParser;

  public MSBuildSonarWayProfile(AnnotationProfileParser annotationProfileParser) {
    this.annotationProfileParser = annotationProfileParser;
  }

  @Override
  public RulesProfile createProfile(ValidationMessages validation) {
    return annotationProfileParser.parse(
        CheckRepository.REPOSITORY_KEY,
        CheckRepository.PROFILE_NAME,
        MSBuildLanguage.KEY,
        CheckRepository.getChecks(),
        validation);
  }
}
