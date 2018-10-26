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
package org.sonar.plugins.msbuild.dgmlcreator;

import java.io.File;
import java.io.IOException;
import java.util.Optional;
import org.sonar.plugins.msbuild.projectchecker.*;
import org.sonar.api.batch.sensor.Sensor;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.command.StreamConsumer;

import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonar.api.config.Configuration;
import org.sonar.plugins.msbuild.MSBuildRunnerExtractor;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import org.sonar.plugins.msbuild.MSBuildMetrics;
import static org.sonar.plugins.msbuild.MSBuildPlugin.IGNORE_LIST_INCLUDES_FOLDERS;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

public class MSBuildDiagramCreatorSensor implements Sensor {
  public static final Logger LOG = Loggers.get(MSBuildDiagramCreatorSensor.class);
  
  private final Configuration settings;
  private final MSBuildRunnerExtractor extractor;
  private final FileSystem fs;
  
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
  
  public MSBuildDiagramCreatorSensor(Configuration settings, MSBuildRunnerExtractor extractor, FileSystem fs) {
    this.settings = settings;
    this.extractor = extractor;
    this.fs = fs;
  }

  @Override
  public void describe(SensorDescriptor descriptor) {
    descriptor.onlyOnLanguage(MSBuildLanguage.KEY).name("MSBuildLineCounterSensor");
  }

  @Override
  public void execute(SensorContext context) {
    if (!settings.getBoolean(DIAGRAM_CREATOR_ENABLED).get()) {
      LOG.debug("Diagram Creator Skipped - Disabled");
      return;
    }
    
    Optional<String> moduleKey = context.config().get("sonar.moduleKey");
    if (moduleKey.isPresent()) {
        LOG.debug("Runs Diagram Creator only at top level project skip : Module Key = '{}'", moduleKey);
        return;        
    }
        
    analyze();
    importResults(context);
  }

  private void analyze() {       
    try {       
      String projectRoot = fs.baseDir().getCanonicalPath();      
      String projectRootPackages = new File(projectRoot, "Packages").toString();
      
      if (new File(settings.get(PACKAGES_BASE_PATH).get()).isAbsolute()) {
        projectRootPackages = settings.get(PACKAGES_BASE_PATH).get();
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
      appendLine(sb, "  <PlotHeaderDependency>" + (settings.getBoolean(PLOT_HEADER_DEPENDENCIES).get() ? "true" : "false")  + "</PlotHeaderDependency>");
      appendLine(sb, "  <CheckRedundantIncludes>false</CheckRedundantIncludes>");
      appendLine(sb, "  <PlotPackagesDependecies>" + (settings.getBoolean(PLOT_NUGET_DEPENDENCIES).get() ? "true" : "false")  + "</PlotPackagesDependecies>");
      appendLine(sb, "  <PlotProjectDependencies>" + (settings.getBoolean(PLOT_PROJECT_DEPENDENCIES).get() ? "true" : "false")  + "</PlotProjectDependencies>");
      appendLine(sb, "  <PlotSolutionBuildDependencies>" + (settings.getBoolean(PLOT_SOLUTION_BUILD_DEPENDENCIES).get() ? "true" : "false")  + "</PlotSolutionBuildDependencies>");
      appendLine(sb, "  <PlotHeaderDependencyInsideProject>" + (settings.getBoolean(PLOT_HEADER_DEPENDENCIES_INSIDE_PROJECT).get() ? "true" : "false")  + "</PlotHeaderDependencyInsideProject>");
      appendLine(sb, "  <PlotHeaderDependencFilter>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(HEADER_DEPENDENCY_FILTER, this.settings));
      appendLine(sb, "  </PlotHeaderDependencFilter>");
      appendLine(sb, "  <PlotSolutionNodeFilter>");
      appendLine(sb, "      " + MSBuildUtils.getStringArrayProperty(SOLUTION_NODE_FILTER, this.settings));
      appendLine(sb, "  </PlotSolutionNodeFilter>");       
      appendLine(sb, "</Configuration>");
      
      File analysisInput = toolInput();
      File analysisOutput = new File(fs.workDir(), "msbuild-diagram-output");      
      
      MSBuildUtils.writeStringToFile(analysisInput.getAbsolutePath(), sb.toString());
      
      File executableFile = extractor.diagramCreatorFile(fs.workDir().getCanonicalPath());
      
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
        LOG.error(msg);        
    }
  }

  private void importResults(SensorContext sensorContext) {
    try {
      File analysisOutput = new File(sensorContext.fileSystem().workDir(), "msbuild-diagram-output.dgml");
      String content = MSBuildUtils.readLinesToString(analysisOutput.getAbsolutePath());
      
      sensorContext.<String>newMeasure()
         .forMetric(MSBuildMetrics.DGMLDIAGRAM)
         .on(sensorContext.module())
         .withValue(content)
         .save();
      
      sensorContext.<Integer>newMeasure()
         .forMetric(MSBuildMetrics.DGMLDIAGRAMSIZE)
         .on(sensorContext.module())
         .withValue(content.length())
         .save();
      
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
