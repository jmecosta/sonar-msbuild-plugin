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

import java.util.Optional;
import org.sonar.plugins.msbuild.MSBuildPlugin;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.hamcrest.core.Is.is;
import static org.junit.Assert.assertThat;
import org.junit.Before;
import org.junit.Test;
import org.sonar.api.config.Configuration;

public class MSBuildLanguageTest {
  
  private Configuration config;
  
  @Before
  public void setup() {
    config = new Configuration() {
      @Override
      public Optional<String> get(String key) {
        String lang = "vcxproj,csproj,fsproj,msbuild,props"; 
        return Optional.of(lang);
      }

      @Override
      public boolean hasKey(String key) {
        return true;
      }

      @Override
      public String[] getStringArray(String key) {
        return new String[] {".vcxproj", ".csproj", ".fsproj", ".msbuild", ".props", ".targets"};
      }
    };
  }
  
  @Test
  public void shouldReturnConfiguredFileSuffixes() {
    MSBuildLanguage msbuild = new MSBuildLanguage(config);

    String[] expected = {".vcxproj", ".csproj", ".fsproj", ".msbuild", ".props", ".targets"};
    
    assertThat(msbuild.getFileSuffixes(), is(expected));
  }
  
    
}
