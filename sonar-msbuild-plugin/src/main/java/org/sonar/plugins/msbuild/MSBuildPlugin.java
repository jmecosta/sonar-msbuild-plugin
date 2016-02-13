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

import com.google.common.collect.ImmutableList;
import java.util.List;
import org.sonar.api.PropertyType;
import org.sonar.api.SonarPlugin;
import org.sonar.api.config.PropertyDefinition;
import org.sonar.api.resources.Qualifiers;
import org.sonar.plugins.msbuild.dgmlcreator.MSBuildDiagramCreatorSensor;
import org.sonar.plugins.msbuild.projectchecker.MSBuildProjectCheckerRulesDefinition;
import org.sonar.plugins.msbuild.projectchecker.MSBuildProjectCheckerExtensionSensor;
import org.sonar.plugins.msbuild.widgets.DmglXmlWidget;

public final class MSBuildPlugin extends SonarPlugin {
  public static final String KEY = "msbuild";
  public static final String FILE_SUFFIXES_KEY = "sonar.msbuild.file.suffixes";
  public static final String IGNORE_LIST_INCLUDES_FOLDERS  = "sonar.msbuild.include.folder.ignores";  
 
    
  /**
   * {@inheritDoc}
     * @return 
   */
  public List getExtensions() {
    return ImmutableList.of(

      PropertyDefinition.builder(MSBuildPlugin.FILE_SUFFIXES_KEY)
        .name("File suffixes")
        .description("Comma-separated list of suffixes for files to analyze.")
        .defaultValue(".vcxproj,.csproj,.fsproj,.msbuild,.props,.targets")
        .category("Msbuild")
        .onQualifiers(Qualifiers.PROJECT)
        .build(),
            
      PropertyDefinition.builder(MSBuildPlugin.IGNORE_LIST_INCLUDES_FOLDERS)
        .name("Include folders to ignore")
        .description("Include paths in list will not be used during diagram creation")
        .type(PropertyType.STRING)
        .multiValues(true)
        .category("Msbuild")
        .onQualifiers(Qualifiers.PROJECT)
        .build(), 
      
      PropertyDefinition.builder(MSBuildProjectCheckerExtensionSensor.PROJECT_CHECKER_PATH)
        .name("Project checker path")
        .description("Use external path for checker")
        .category("Msbuild")
        .subCategory("ProjectChecker")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.STRING)
        .build(),
      
      PropertyDefinition.builder(MSBuildProjectCheckerExtensionSensor.EXTERNAL_CUSTOM_RULES)
      .name("External dlls to load")
      .description("External Dlls with Rules to Use, absolute or relative")
      .type(PropertyType.STRING)
      .multiValues(true)
      .onQualifiers(Qualifiers.PROJECT)
      .category("Msbuild")
      .subCategory("ProjectChecker")
      .build(),
            
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.DIAGRAM_CREATOR_ENABLED)
        .defaultValue("True")
        .name("Diagram creator enabled")
        .description("Disables diagram creator")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(),
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.DIAGRAM_CREATOR_PATH)
        .name("Diagram creator path")
        .description("Use external path for diagram creator path")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.STRING)
        .build(),
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PACKAGES_BASE_PATH)
        .defaultValue("Packages")
        .name("Nuget packages base path")
        .description("Base path where nugets are found, can be relative or absolute")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.STRING)
        .build(),
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.IGNORE_NUGET_PACKAGES)
      .name("Nuget packages to ignore")
      .description("Packages in list will not be ploted in diagram")
      .type(PropertyType.STRING)
      .multiValues(true)
      .onQualifiers(Qualifiers.PROJECT)
      .subCategory("Dgml")
      .build(),
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.HEADER_DEPENDENCY_FILTER)
      .name("Plot header dependencies between solutions")
      .description("Only solutions defined in filter will be use to plot header dependencies, ex: SLN:SolutionName")
      .type(PropertyType.STRING)
      .multiValues(true)
      .onQualifiers(Qualifiers.PROJECT)
      .subCategory("Dgml")
      .build(),
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.SOLUTION_NODE_FILTER)
      .name("Solution node filter")
      .description("Only solutions in filter will be ploted, ex: SLN:SolutionName")
      .type(PropertyType.STRING)
      .multiValues(true)
      .onQualifiers(Qualifiers.PROJECT)
      .subCategory("Dgml")
      .build(),      
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PLOT_SOLUTION_BUILD_DEPENDENCIES)
        .defaultValue("True")
        .name("Plot solution build order")
        .description("Plot solution build order")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(),
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PLOT_NUGET_DEPENDENCIES)
        .defaultValue("True")
        .name("Plot nuget dependencies")
        .description("Plot nuget dependencies")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(),      
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PLOT_PROJECT_DEPENDENCIES)
        .defaultValue("True")
        .name("Plot nuget dependencies")
        .description("Plot nuget dependencies")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(),       
      
      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PLOT_HEADER_DEPENDENCIES_INSIDE_PROJECT)
        .defaultValue("True")
        .name("Plot headers dependencies inside solution")
        .description("Header dependencies will be plot")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(), 

      PropertyDefinition.builder(MSBuildDiagramCreatorSensor.PLOT_HEADER_DEPENDENCIES)
        .defaultValue("True")
        .name("Plot headers dependencies")
        .description("Header dependencies will be plot")
        .category("Msbuild")
        .subCategory("Dgml")
        .onQualifiers(Qualifiers.PROJECT)
        .type(PropertyType.BOOLEAN)
        .build(),
      
      MSBuildLanguage.class,

      MSBuildCodeColorizerFormat.class,
      MSBuildCpdMapping.class,

      // Sensors
      MSBuildLineCounterSensor.class,
      MSBuildProjectCheckerRulesDefinition.class,
      MSBuildProjectCheckerExtensionSensor.class,
      MSBuildDiagramCreatorSensor.class,
      MSBuildRunnerExtractor.class,
      
      // metrics
      MSBuildMetrics.class,
      
      // widgets
      DmglXmlWidget.class
    );
  }

  @Override
  public String toString() {
    return getClass().getSimpleName();
  }
}
