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
package org.sonar.plugins.msbuild.dgmlcreator;

import org.sonar.plugins.msbuild.projectchecker.*;
import com.google.common.base.Charsets;
import com.google.common.base.Throwables;
import com.google.common.io.Files;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.Sensor;
import org.sonar.api.batch.SensorContext;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.config.Settings;
import org.sonar.api.resources.Project;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.command.StreamConsumer;


import java.io.File;
import java.io.IOException;
import java.util.logging.Level;
import static org.apache.commons.io.FileUtils.readFileToString;
import org.sonar.api.batch.bootstrap.ProjectReactor;
import org.sonar.api.measures.Measure;
import org.sonar.plugins.msbuild.MSBuildRunnerExtractor;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import org.sonar.plugins.msbuild.MSBuildMetrics;
import static org.sonar.plugins.msbuild.MSBuildPlugin.IGNORE_LIST_INCLUDES_FOLDERS;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

public class MSBuildDiagramCreatorSensor implements Sensor {
 
  private static final Logger LOG = LoggerFactory.getLogger(MSBuildDiagramCreatorSensor.class);
  
  private final Settings settings;
  private final MSBuildRunnerExtractor extractor;
  private final FileSystem fs;
  private final ProjectReactor reactor;
  
  public static final String DIAGRAM_CREATOR_PATH = "sonar.msbuild.diagramCreator.path";
  public static final String DIAGRAM_CREATOR_ENABLED = "sonar.msbuild.diagramCreator.enabled";
  public static final String PACKAGES_BASE_PATH  = "sonar.msbuild.packages.basePath";
  
  public static final String SOLUTION_NODE_FILTER  = "sonar.msbuild.solution.node.filter";
  public static final String PLOT_SOLUTION_BUILD_DEPENDENCIES  = "sonar.msbuild.plot.solution.build.dependencies";
  public static final String PLOT_PROJECT_DEPENDENCIES  = "sonar.msbuild.plot.project.dependencies";
  
  public static final String IGNORE_NUGET_PACKAGES = "sonar.msbuild.nuget.filter";
  public static final String PLOT_NUGET_DEPENDENCIES  = "sonar.msbuild.plot.nuget.dependencies";
  
  public static final String HEADER_DEPENDENCY_FILTER  = "sonar.msbuild.header.dependency.filter";
  public static final String PLOT_HEADER_DEPENDENCIES_INSIDE_PROJECT  = "sonar.msbuild.plot.header.dependencies.inside.project";
  public static final String PLOT_HEADER_DEPENDENCIES  = "sonar.msbuild.plot.header.dependencies";
  
  public MSBuildDiagramCreatorSensor(Settings settings, MSBuildRunnerExtractor extractor, FileSystem fs, ProjectReactor reactor) {
    this.settings = settings;
    this.extractor = extractor;
    this.fs = fs;
    this.reactor = reactor;
  }

  @Override
  public boolean shouldExecuteOnProject(Project project) {
    return true;
  }

  @Override
  public void analyse(Project project, SensorContext context) {
    
    if (!settings.getBoolean(DIAGRAM_CREATOR_ENABLED)) {
      LOG.debug("Diagram Creator Skipped - Disabled");
      return;
    }
    
    if (!project.isRoot()) {
      LOG.debug("Diagram Creator Skipped - Not Reactor Project");
      return;
    }
    
    analyze();
    importResults(project, context);
  }
  
  private void analyze() {       
    try {       
      String projectRoot = reactor.getRoot().getBaseDir().getCanonicalPath();      
      String projectRootPackages = new File(projectRoot, "Packages").toString();
      
      if (new File(settings.getString(PACKAGES_BASE_PATH)).isAbsolute()) {
        projectRootPackages = settings.getString(PACKAGES_BASE_PATH);
      }
      
      StringBuilder sb = new StringBuilder();
      appendLine(sb, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
      appendLine(sb, "<Configuration>");
      appendLine(sb, "  <IgnoreNugetPackages>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(IGNORE_NUGET_PACKAGES, this.settings));
      appendLine(sb, "  </IgnoreNugetPackages>");
      appendLine(sb, "  <PackageBasePath>" + projectRootPackages + "</PackageBasePath>");
      appendLine(sb, "  <IgnoreIncludeFolders>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(IGNORE_LIST_INCLUDES_FOLDERS, this.settings));
      appendLine(sb, "  </IgnoreIncludeFolders>");
      appendLine(sb, "  <PlotHeaderDependency>" + (settings.getBoolean(PLOT_HEADER_DEPENDENCIES) ? "true" : "false")  + "</PlotHeaderDependency>");
      appendLine(sb, "  <CheckRedundantIncludes>false</CheckRedundantIncludes>");
      appendLine(sb, "  <PlotPackagesDependecies>" + (settings.getBoolean(PLOT_NUGET_DEPENDENCIES) ? "true" : "false")  + "</PlotPackagesDependecies>");
      appendLine(sb, "  <PlotProjectDependencies>" + (settings.getBoolean(PLOT_PROJECT_DEPENDENCIES) ? "true" : "false")  + "</PlotProjectDependencies>");
      appendLine(sb, "  <PlotSolutionBuildDependencies>" + (settings.getBoolean(PLOT_SOLUTION_BUILD_DEPENDENCIES) ? "true" : "false")  + "</PlotSolutionBuildDependencies>");
      appendLine(sb, "  <PlotHeaderDependencyInsideProject>" + (settings.getBoolean(PLOT_HEADER_DEPENDENCIES_INSIDE_PROJECT) ? "true" : "false")  + "</PlotHeaderDependencyInsideProject>");
      appendLine(sb, "  <PlotHeaderDependencFilter>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(HEADER_DEPENDENCY_FILTER, this.settings));
      appendLine(sb, "  </PlotHeaderDependencFilter>");
      appendLine(sb, "  <PlotSolutionNodeFilter>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(SOLUTION_NODE_FILTER, this.settings));
      appendLine(sb, "  </PlotSolutionNodeFilter>");       
      appendLine(sb, "</Configuration>");
      
      File analysisInput = toolInput();
      File analysisOutput = new File(fs.workDir(), "msbuild-diagram-output");
      
      try {
        Files.write(sb, analysisInput, Charsets.UTF_8);
      } catch (IOException e) {
        throw Throwables.propagate(e);
      }
      
      File executableFile = extractor.diagramCreatorFile();
      
      Command command;
      if (OsUtils.isWindows()) {
        command = Command.create(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/o:" + analysisOutput.getAbsolutePath())
                .addArgument("/d:" + projectRoot);
      } else {
        command = Command.create("mono")
                .addArgument(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/o:" + analysisOutput.getAbsolutePath())
                .addArgument("/d:" + projectRoot);
      }
      
      LOG.debug(command.toCommandLine());
      CommandExecutor.create().execute(command, new LogInfoStreamConsumer(), new LogErrorStreamConsumer(), Integer.MAX_VALUE);
    } catch (Exception ex) {
        String msg = new StringBuilder()
          .append("Cannot execute diagram creator, details: '")
          .append(ex)
          .append("'")
          .toString();
        throw new IllegalStateException(msg, ex);
    }
  }

  private void importResults(Project project, SensorContext context) {
    try {
      File analysisOutput = new File(fs.workDir(), "msbuild-diagram-output.dgml");
      String content = readFileToString(analysisOutput);
      
      Measure measure = new Measure(MSBuildMetrics.DGMLDIAGRAM);
      measure.setData(content);
      context.saveMeasure(measure);
      
      Measure measure1 = new Measure(MSBuildMetrics.DGMLDIAGRAMSIZE);
      measure1.setIntValue(content.length());
      context.saveMeasure(measure1);
      
    } catch (IOException ex) {
        String msg = new StringBuilder()
          .append("Cannot import diagram, details: '")
          .append(ex)
          .append("'")
          .toString();
        throw new IllegalStateException(msg, ex);
    }
  }

  private void appendLine(StringBuilder sb, String line) {
    sb.append(line);
    sb.append("\r\n");
  }

  private static class LogInfoStreamConsumer implements StreamConsumer {

    @Override
    public void consumeLine(String line) {
      LOG.info(line);
    }
  }

  private static class LogErrorStreamConsumer implements StreamConsumer {
    @Override
    public void consumeLine(String line) {
      LOG.error(line);
    }
  }

  private Iterable<File> filesToAnalyze() {
    return fs.files(fs.predicates().hasLanguage(MSBuildLanguage.KEY));
  }

  private File toolInput() {
    return new File(fs.workDir(), "msbuild-diagram-input.xml");
  }
}
