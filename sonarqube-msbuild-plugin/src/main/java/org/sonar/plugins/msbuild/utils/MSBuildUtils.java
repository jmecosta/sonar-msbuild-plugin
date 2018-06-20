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
package org.sonar.plugins.msbuild.utils;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import org.sonar.api.config.Configuration;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;

/**
 * Utility class holding various, well, utilities
 */
public final class MSBuildUtils {
  
  public static final Logger LOG = Loggers.get(MSBuildUtils.class);
  
  private MSBuildUtils() {
    // only static methods
  }

  /**
   * @param file
   * @return Returns file path of provided file, or "null" if file == null
   */
  public static String fileToAbsolutePath(File file) {
    if(file == null) {
      return "null";
    }
    return file.getAbsolutePath();
  }  
  
  public static String getStringArrayProperty(String name, Configuration settings) {
    StringBuilder sb = new StringBuilder();
    for (String n : settings.getStringArray(name)) { 
      if (sb.length() > 0) sb.append(';');
        sb.append(n);
      }
    return sb.toString();
  }  
  
  public static String[] readLines(String filename) throws IOException {
      FileReader fileReader = new FileReader(filename);
      BufferedReader bufferedReader = new BufferedReader(fileReader);
      List<String> lines = new ArrayList<String>();
      String line = null;
      while ((line = bufferedReader.readLine()) != null) {
          lines.add(line);
      }
      bufferedReader.close();
      return lines.toArray(new String[lines.size()]);
  }
  
  public static String readLinesToString(String filename) throws IOException {
      FileReader fileReader = new FileReader(filename);
      BufferedReader bufferedReader = new BufferedReader(fileReader);
      StringBuilder lines = new StringBuilder();
      String line = null;
      while ((line = bufferedReader.readLine()) != null) {
          lines.append(line);
      }
      bufferedReader.close();
      return lines.toString();
  }  
  
  public static void writeStringToFile(String path, String content) throws IOException {
      File file = new File(path);
      BufferedWriter writer = null;
      try {
          writer = new BufferedWriter(new FileWriter(file));
          writer.write(content);
      } finally {
          if (writer != null) writer.close();
      }  
  }
}
