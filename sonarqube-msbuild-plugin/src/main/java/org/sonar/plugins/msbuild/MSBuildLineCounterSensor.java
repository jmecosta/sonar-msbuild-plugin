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

import org.sonar.api.batch.sensor.Sensor;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonar.api.batch.fs.InputFile;
import java.io.FileInputStream;
import java.io.IOException;
import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonar.api.measures.CoreMetrics;
import org.sonar.plugins.msbuild.parsers.MSBuildLineCountParser;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

/**
 * Count lines of code in XML files.
 *
 * Original from: https://github.com/SonarSource/sonar-xml
 * Modified to fit msbuild files
 */
public final class MSBuildLineCounterSensor implements Sensor {
  private static final Logger LOG = Loggers.get(MSBuildLineCounterSensor.class);
    
  @Override
  public String toString() {
    return getClass().getSimpleName();
  }

  @Override
  public void describe(SensorDescriptor descriptor) {
    descriptor.onlyOnLanguage(MSBuildLanguage.KEY).name("MSBuildLineCounterSensor");
  }

  @Override
  public void execute(SensorContext context) {
    for (InputFile file : context.fileSystem().inputFiles(context.fileSystem().predicates().hasLanguage(MSBuildLanguage.KEY))) {
      addMeasures(context, file);
    }
  }  
  
  private void addMeasures(SensorContext sensorContext, InputFile file) {
  
    int numLines = 0;
    int numBlankLines = 0;

    try {
      String [] lines = MSBuildUtils.readLines(file.filename());
      
      for (String line : lines) {
        numLines++;
        if (line.isEmpty()) {
          numBlankLines++;
        }        
      }
    } catch (IOException e) {
      LOG.warn("Unable to count lines for file: " + file.filename());
      LOG.warn("Cause: {}", e);
    }

    try {

      LOG.debug("Count comment in " + file.filename());

      MSBuildLineCountParser lineCountParser = new MSBuildLineCountParser();
      int numCommentLines = lineCountParser.countLinesOfComment(new FileInputStream(file.filename()), file);
      sensorContext.<Integer>newMeasure()
         .forMetric(CoreMetrics.COMMENT_LINES)
         .on(file)
         .withValue(numCommentLines)
         .save();

      sensorContext.<Integer>newMeasure()
         .forMetric(CoreMetrics.NCLOC)
         .on(file)
         .withValue(numLines - numBlankLines - numCommentLines)
         .save();
      
    } catch (Exception e) {
      LOG.debug("Fail to count lines in " + file.filename(), e);
    }

    LOG.debug("LineCountSensor: " + file.filename() + ":" + numLines + "," + numBlankLines + "," + 0);
  }
}