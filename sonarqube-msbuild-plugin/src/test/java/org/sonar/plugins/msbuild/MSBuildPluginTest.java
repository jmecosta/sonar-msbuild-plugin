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
package org.sonar.plugins.msbuild;

import static org.junit.Assert.assertEquals;
import org.junit.Test;
import org.sonar.api.Plugin;
import org.sonar.api.SonarQubeVersion;

public class MSBuildPluginTest {
  
  @Test
  public void testGetExtensions() throws Exception {
   Plugin.Context context = new Plugin.Context(SonarQubeVersion.V5_6);
   MSBuildPlugin plugin = new MSBuildPlugin();
   plugin.define(context);
   assertEquals(23, context.getExtensions().size());
  }
}
