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

import java.lang.annotation.Annotation;
import java.util.List;
import org.sonar.api.rules.AnnotationRuleParser;
import org.sonar.api.rules.Rule;
import org.sonar.api.rules.RuleRepository;
import org.sonar.plugins.msbuild.checks.CheckRepository;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import org.sonar.plugins.msbuild.MSBuildPlugin;

/**
 * Repository for XML rules.
 */
public final class MSBuildRulesRepository extends RuleRepository {

  private final AnnotationRuleParser annotationRuleParser;

  public MSBuildRulesRepository(AnnotationRuleParser annotationRuleParser) {
    super(CheckRepository.REPOSITORY_KEY, MSBuildLanguage.KEY);
    setName(CheckRepository.PROFILE_NAME);
    this.annotationRuleParser = annotationRuleParser;
  }

  @Override
  public List<Rule> createRules() {
    return annotationRuleParser.parse(CheckRepository.REPOSITORY_KEY, CheckRepository.getChecks());
  }  
}
