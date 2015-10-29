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
import org.sonar.api.CoreProperties;
import org.sonar.api.batch.DependedUpon;
import org.sonar.api.batch.DependsUpon;
import org.sonar.api.batch.Phase;
import org.sonar.api.batch.Sensor;
import org.sonar.api.batch.SensorContext;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.rule.CheckFactory;
import org.sonar.api.batch.rule.Checks;
import org.sonar.api.checks.NoSonarFilter;
import org.sonar.api.config.Settings;
import org.sonar.api.profiles.RulesProfile;
import org.sonar.api.resources.Project;

import java.io.File;
import java.nio.charset.Charset;
import java.util.Collection;
import java.util.Collections;
import java.util.List;
import java.util.Locale;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.sensor.issue.Issue;
import org.sonar.api.component.ResourcePerspectives;
import org.sonar.api.issue.Issuable;
import org.sonar.api.resources.Resource;
import org.sonar.api.rule.RuleKey;
import org.sonar.api.rules.ActiveRule;
import org.sonar.api.rules.Violation;
import org.sonar.plugins.msbuild.checks.AbstractXmlCheck;
import org.sonar.plugins.msbuild.checks.CheckRepository;
import org.sonar.plugins.msbuild.checks.XmlSourceCode;

/**
 * XmlSensor provides analysis of xml files.
 *
 * @author Matthijs Galesloot
 */
public final class MSBuildSensor implements Sensor {

  private static final Logger LOG = LoggerFactory.getLogger(MSBuildSensor.class);
  private final Checks<AbstractXmlCheck> checks;
  private final FileSystem fs;
  private final Settings settings;
  private final CheckFactory checkFactory;
  private final RulesProfile profile;
  private final NoSonarFilter noSonarFilter;
  private final ResourcePerspectives resourcePerspectives;

  public MSBuildSensor(RulesProfile profile, FileSystem fs, Settings settings, NoSonarFilter noSonarFilter, CheckFactory checkFactory, ResourcePerspectives resourcePerspectives) {
    this.checks = checkFactory.<AbstractXmlCheck>create(CheckRepository.REPOSITORY_KEY).addAnnotatedChecks(CheckRepository.getChecks());
    this.profile = profile;
    this.noSonarFilter = noSonarFilter;
    this.fs = fs;
    this.settings = settings;
    this.checkFactory = checkFactory;
    this.resourcePerspectives = resourcePerspectives;
  }

  /**
   * Analyze the XML files.
   */
  public void analyse(Project project, SensorContext sensorContext) {
    Collection<AbstractXmlCheck> checkList = this.checks.all();

    for (File inputfile :  getSourceFiles()) {

      try {
        LOG.debug("Analyse: " + inputfile.getAbsolutePath());
        org.sonar.api.resources.File resource = org.sonar.api.resources.File.fromIOFile(inputfile, project);
        XmlSourceCode sourceCode = new XmlSourceCode(resource, inputfile);


        for (AbstractXmlCheck check : checkList) {
          check.validate(sourceCode);
        }
        
        saveIssues(resource, sourceCode);

      } catch (Exception e) {
        LOG.error("Could not analyze the file " + inputfile.getAbsolutePath(), e);
      }
    }
  }
    
  private void saveIssues(org.sonar.api.resources.File sonarFile, XmlSourceCode sourceFile) {
    for (Violation xmlIssue : sourceFile.getViolations()) {
      Issuable issuable = resourcePerspectives.as(Issuable.class, sourceFile.getResource());
      issuable.addIssue(
        issuable.newIssueBuilder()
          .ruleKey(xmlIssue.getRule().ruleKey())
          .line(xmlIssue.getLineId())
          .message(xmlIssue.getMessage())
          .build());
    }
  }
  
  private Iterable<File> getSourceFiles() {
    return toFile(fs.inputFiles(fs.predicates().and(fs.predicates().hasLanguage(MSBuildLanguage.KEY), fs.predicates().hasType(InputFile.Type.MAIN))));
  }

  /**
   * This sensor only executes on projects with active XML rules.
   */
  public boolean shouldExecuteOnProject(Project project) {
    return MSBuildPlugin.KEY.equals(project.getLanguageKey());
  }

  @Override
  public String toString() {
    return getClass().getSimpleName();
  }
  
  private Iterable<java.io.File> toFile(Iterable<InputFile> inputFiles) {
    List<java.io.File> files = Lists.newArrayList();
    for (InputFile inputFile : inputFiles) {
      files.add(inputFile.file());
    }
    return files;
  }
}
