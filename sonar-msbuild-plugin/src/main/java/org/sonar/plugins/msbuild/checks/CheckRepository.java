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

import com.google.common.collect.ImmutableList;

import java.util.List;

public final class CheckRepository {
  public static final String REPOSITORY_KEY = "MsbuildChecks";
  public static final String PROFILE_NAME = "Sonar way";
  
  private CheckRepository() {
  }

  public static List<Class> getChecks() {
    return ImmutableList.<Class>of(
        IllegalTabCheck.class,
        IndentCheck.class,
        NewlineCheck.class,
        XmlSchemaCheck.class,
        XPathCheck.class);
  }
}
