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
package org.sonar.plugins.msbuild;

import org.sonar.plugins.msbuild.MSBuildPlugin;
import static org.junit.Assert.assertEquals;

import org.junit.Test;

public class MSBuildPluginTest {
  @Test
  public void testGetExtensions() throws Exception {
    MSBuildPlugin plugin = new MSBuildPlugin();
    assertEquals(25, plugin.getExtensions().size());
  }
}
