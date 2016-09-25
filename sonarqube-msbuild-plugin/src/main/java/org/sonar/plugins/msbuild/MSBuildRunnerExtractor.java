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

import org.sonar.api.batch.BatchSide;
import org.sonar.api.config.Settings;
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
@BatchSide()
public class MSBuildRunnerExtractor {

  public static final Logger LOG = Loggers.get(MSBuildRunnerExtractor.class);
  
  private static final String N_PROJECT = "ProjectTools";
  private static final String N_PROJECT_ZIP = "ProjectChecker.zip";
  
  private static final String N_PROJECT_CHECKER_EXE = "ProjectCheckerRunner.exe";  
  private static final String N_DIAGRAM_CREATOR_EXE = "MSbuildPackageDiagramCreator.exe";
  
  private final ProjectReactor reactor;
  private final Settings settings;
  private File fileProjectChecker = null;
  private File fileDiagramCreator = null;

  public MSBuildRunnerExtractor(ProjectReactor reactor, Settings settings) {
    this.reactor = reactor;
    this.settings = settings;
  }

  public File projectCheckerFile() throws IOException {
    String path = settings.getString(MSBuildProjectCheckerExtensionSensor.PROJECT_CHECKER_PATH);
    if (path != null && !path.equals("")) {
      return new File(path);
    }
    
    if (fileProjectChecker == null) {
      fileProjectChecker = unzipProjectCheckerFile(N_PROJECT_CHECKER_EXE);
    }

    return fileProjectChecker;
  }
  
  public File diagramCreatorFile() throws IOException {
    String path = settings.getString(MSBuildDiagramCreatorSensor.DIAGRAM_CREATOR_PATH);
    if (path != null && !path.equals("")) {
      return new File(path);
    }
    
    if (fileDiagramCreator == null) {
      fileDiagramCreator = unzipProjectCheckerFile(N_DIAGRAM_CREATOR_EXE);
    }

    return fileDiagramCreator;
  }  

  private File unzipProjectCheckerFile(String fileName) throws IOException {
    File workingDir = reactor.getRoot().getWorkDir();
    File toolWorkingDir = new File(workingDir, N_PROJECT);
    File zipFile = new File(workingDir, N_PROJECT_ZIP);
    
    if (zipFile.exists()) {
      return new File(toolWorkingDir, fileName);
    }

    try {

      
      try (InputStream is = getClass().getResourceAsStream("/" + N_PROJECT_ZIP)) {
        Files.copy(is, zipFile.toPath());
      }

      UnZip unZip = new UnZip();
      unZip.unZipIt(zipFile.getAbsolutePath(),toolWorkingDir.getAbsolutePath());
        
      return new File(toolWorkingDir, fileName);
    } catch (IOException e) {
      LOG.error("Unable tp unzip File: {} => {}", fileName, e.getMessage());
      throw e;
    }
  }
}
