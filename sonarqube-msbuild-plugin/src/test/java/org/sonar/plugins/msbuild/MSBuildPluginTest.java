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
import org.sonar.api.SonarProduct;
import org.sonar.api.SonarQubeSide;
import org.sonar.api.SonarRuntime;
import org.sonar.api.utils.Version;

public class MSBuildPluginTest {
  
  @Test
  public void testGetExtensions() throws Exception {
   Plugin.Context context = new Plugin.Context(new SonarRuntime() {
     @Override
     public Version getApiVersion() {
       return Version.parse("7.2");
     }

     @Override
     public SonarProduct getProduct() {
       return SonarProduct.SONARQUBE;
     }

     @Override
     public SonarQubeSide getSonarQubeSide() {
       return SonarQubeSide.SCANNER;
     }
   });
   MSBuildPlugin plugin = new MSBuildPlugin();
   plugin.define(context);
   assertEquals(24, context.getExtensions().size());
  }
}
