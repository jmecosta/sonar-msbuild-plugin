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

import java.io.File;
import java.io.IOException;
import org.apache.commons.io.FileUtils;
import org.apache.commons.io.LineIterator;
import org.apache.commons.lang.StringUtils;
import org.sonar.api.batch.Sensor;
import org.sonar.api.batch.SensorContext;
import org.sonar.api.measures.CoreMetrics;
import org.sonar.api.resources.Project;
import org.sonar.api.scan.filesystem.ModuleFileSystem;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.scan.filesystem.FileQuery;
import org.sonar.plugins.msbuild.parsers.MSBuildLineCountParser;

/**
 * Count lines of code in XML files.
 *
 * @author Matthijs Galesloot
 */
public final class MSBuildLineCounterSensor implements Sensor {
  private static final Logger LOG = LoggerFactory.getLogger(MSBuildLineCounterSensor.class);
  private final ModuleFileSystem fileSystem;

  /**
   * {@inheritDoc}
   */
  public MSBuildLineCounterSensor(ModuleFileSystem fileSystem) {
      this.fileSystem = fileSystem;
  }
    
  private void addMeasures(SensorContext sensorContext, File file, org.sonar.api.resources.File xmlFile) {

    LineIterator iterator = null;
    int numLines = 0;
    int numBlankLines = 0;

    try {
      iterator = FileUtils.lineIterator(file);

      while (iterator.hasNext()) {
        String line = iterator.nextLine();
        numLines++;
        if (StringUtils.isBlank(line)) {
          numBlankLines++;
        }
      }
    } catch (IOException e) {
      LOG.warn("Unable to count lines for file: " + file.getAbsolutePath());
      LOG.warn("Cause: {}", e);
    } finally {
      LineIterator.closeQuietly(iterator);
    }

    try {

      LOG.debug("Count comment in " + file.getPath());

      MSBuildLineCountParser lineCountParser = new MSBuildLineCountParser();
      int numCommentLines = lineCountParser.countLinesOfComment(FileUtils.openInputStream(file));
      sensorContext.saveMeasure(xmlFile, CoreMetrics.LINES, (double) numLines);
      sensorContext.saveMeasure(xmlFile, CoreMetrics.COMMENT_LINES, (double) numCommentLines);
      sensorContext.saveMeasure(xmlFile, CoreMetrics.NCLOC, (double) numLines - numBlankLines - numCommentLines);
    } catch (Exception e) {
      LOG.debug("Fail to count lines in " + file.getPath(), e);
    }

    LOG.debug("LineCountSensor: " + xmlFile.getKey() + ":" + numLines + "," + numBlankLines + "," + 0);
  }

  @Override
  public void analyse(Project project, SensorContext sensorContext) {

    for (File file : fileSystem.files(FileQuery.onSource().onLanguage(MSBuildPlugin.KEY))) {
      org.sonar.api.resources.File htmlFile = org.sonar.api.resources.File.fromIOFile(file, project);
      addMeasures(sensorContext, file, htmlFile);
    }
  }

  @Override
  public boolean shouldExecuteOnProject(Project project) {
    return !fileSystem.files(FileQuery.onSource().onLanguage(MSBuildPlugin.KEY)).isEmpty();
  }

  @Override
  public String toString() {
    return getClass().getSimpleName();
  }

}