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

import org.sonar.api.batch.InstantiationStrategy;
import org.sonar.api.batch.bootstrap.ProjectReactor;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.util.Optional;

import org.sonar.api.batch.ScannerSide;
import org.sonar.api.config.Configuration;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonar.plugins.msbuild.dgmlcreator.MSBuildDiagramCreatorSensor;
import org.sonar.plugins.msbuild.projectchecker.MSBuildProjectCheckerExtensionSensor;
import org.sonar.plugins.msbuild.utils.UnZip;

/**
 * Provides reusable code for Xml parsers.
 * 
 * Original from: https://github.com/SonarSource/sonar-csharp
 * Modified to fit msbuild files
 */
@InstantiationStrategy(InstantiationStrategy.PER_BATCH)
@ScannerSide()
public class MSBuildRunnerExtractor {

  public static final Logger LOG = Loggers.get(MSBuildRunnerExtractor.class);
  
  private static final String N_PROJECT = "ProjectTools";
  private static final String N_PROJECT_ZIP = "ProjectChecker.zip";
  
  private static final String N_PROJECT_CHECKER_EXE = "ProjectCheckerRunner.exe";  
  private static final String N_DIAGRAM_CREATOR_EXE = "MSbuildPackageDiagramCreator.exe";
  
  private final Configuration settings;
  private File fileProjectChecker = null;
  private File fileDiagramCreator = null;

  public MSBuildRunnerExtractor(Configuration settings) {
    this.settings = settings;
  }

  public File projectCheckerFile(String rootDir) throws IOException {
    Optional<String> path = settings.get(MSBuildProjectCheckerExtensionSensor.PROJECT_CHECKER_PATH);
    if (path.isPresent() && !path.get().equals("")) {
      return new File(path.get());
    } 
    
    if (fileProjectChecker == null) {
      fileProjectChecker = unzipProjectCheckerFile(N_PROJECT_CHECKER_EXE, rootDir);
    }

    return fileProjectChecker;
  }
  
  public File diagramCreatorFile(String rootDir) throws IOException {
    Optional<String> path = settings.get(MSBuildDiagramCreatorSensor.DIAGRAM_CREATOR_PATH);
    if (path.isPresent() && !path.get().equals("")) {
      return new File(path.get());
    }     
    
    if (fileDiagramCreator == null) {
      LOG.info("Unzip Diagram Creator to: {} => {}", N_DIAGRAM_CREATOR_EXE, rootDir);
      fileDiagramCreator = unzipProjectCheckerFile(N_DIAGRAM_CREATOR_EXE, rootDir);
    }

    return fileDiagramCreator;
  }  

  private File unzipProjectCheckerFile(String fileName, String rootDir) throws IOException {
    File toolWorkingDir = new File(rootDir, N_PROJECT);
    File zipFile = new File(rootDir, N_PROJECT_ZIP);
    
    if (zipFile.exists()) {
      return new File(toolWorkingDir, fileName);
    }

    try {

      LOG.error("Unzip File: {} => {}", N_PROJECT_ZIP, fileName);
      try (InputStream is = getClass().getResourceAsStream("/" + N_PROJECT_ZIP)) {
        Files.copy(is, zipFile.toPath());
      }

      UnZip unZip = new UnZip();
      unZip.unZipIt(zipFile.getAbsolutePath(),toolWorkingDir.getAbsolutePath());
        
      return new File(toolWorkingDir, fileName);
    } catch (IOException e) {
      LOG.error("Unable to unzip File: {} => {}", fileName, e.getMessage());
      throw e;
    }
  }
}
