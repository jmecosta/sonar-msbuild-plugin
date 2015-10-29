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

package org.sonar.plugins.msbuild.projectchecker;

import org.sonar.plugins.msbuild.projectchecker.MSBuildProjectCheckerRulesDefinition;
import org.junit.Test;
import org.sonar.api.server.rule.RulesDefinition.Context;

import static org.fest.assertions.Assertions.assertThat;

public class ProjectCehckerSonarRulesDefinitionTest {

  @Test
  public void test() {
    Context context = new Context();
    assertThat(context.repositories()).isEmpty();

    new MSBuildProjectCheckerRulesDefinition().define(context);

    assertThat(context.repositories()).hasSize(1);
    //assertThat(context.repository("fsharplint").rules()).hasSize(28);
  }

}
