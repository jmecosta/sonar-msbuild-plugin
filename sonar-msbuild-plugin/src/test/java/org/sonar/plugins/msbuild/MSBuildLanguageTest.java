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
import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.hamcrest.core.Is.is;
import static org.junit.Assert.assertThat;
import org.junit.Before;
import org.junit.Test;
import org.sonar.api.config.Settings;

public class MSBuildLanguageTest {
  
  private Settings config;
  
  @Before
  public void setup() {
    config = new Settings();
  }
  
  @Test
  public void shouldReturnConfiguredFileSuffixes() {
    config.setProperty(MSBuildPlugin.FILE_SUFFIXES_KEY, "vcxproj,csproj,fsproj,msbuild,props");
    MSBuildLanguage msbuild = new MSBuildLanguage(config);

    String[] expected = {"vcxproj", "csproj", "fsproj", "msbuild", "props"};
    String[] expectedSources = {"vcxproj", "csproj", "fsproj", "msbuild", "props"};
    
    assertThat(msbuild.getFileSuffixes(), is(expected));
    assertThat(msbuild.getFileSuffixes(), is(expectedSources));
  }
  
  @Test
  public void shouldReturnDefaultFileSuffixes() {
    MSBuildLanguage msbuild = new MSBuildLanguage(config);
    
    String[] expectedSources = {".vcxproj", ".csproj", ".fsproj", ".msbuild", ".props", ".targets"};
    String[] expectedAll = {".vcxproj", ".csproj", ".fsproj", ".msbuild", ".props", ".targets"};
    
    assertThat(msbuild.getFileSuffixes(), is(expectedAll));
    assertThat(msbuild.getFileSuffixes(), is(expectedSources));
  }       
}
