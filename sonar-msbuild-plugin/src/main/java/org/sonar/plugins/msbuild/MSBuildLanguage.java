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

import com.google.common.collect.Lists;
import java.util.List;
import org.apache.commons.lang.StringUtils;
import org.sonar.api.config.Settings;
import org.sonar.api.resources.AbstractLanguage;

/**
 * {@inheritDoc}
 */
public class MSBuildLanguage extends AbstractLanguage {
  public static final String[] DEFAULT_SOURCE_SUFFIXES = {".vcxproj",".csproj",".fsproj",".msbuild",".props",".targets"};
  public static final String LANGUAGE_NAME = "MSBuild";    
  public static final String KEY = "msbuild";
  private final Settings settings;
  
  /**
   * {@inheritDoc}
   * @param settings
   */
  public MSBuildLanguage(Settings settings) {
    super(KEY, LANGUAGE_NAME);
    this.settings = settings;
  }
  
  /**
   * {@inheritDoc}
   * @return 
   */
  public String[] getFileSuffixes() {
    String[] suffixes = filterEmptyStrings(settings.getStringArray(MSBuildPlugin.FILE_SUFFIXES_KEY));
    if (suffixes.length == 0) {
      suffixes = MSBuildLanguage.DEFAULT_SOURCE_SUFFIXES;
    }
    return suffixes;
  }

  private String[] filterEmptyStrings(String[] stringArray) {
    List<String> nonEmptyStrings = Lists.newArrayList();
    for (String string : stringArray) {
      if (StringUtils.isNotBlank(string.trim())) {
        nonEmptyStrings.add(string.trim());
      }
    }
    return nonEmptyStrings.toArray(new String[nonEmptyStrings.size()]);
  }  
}
