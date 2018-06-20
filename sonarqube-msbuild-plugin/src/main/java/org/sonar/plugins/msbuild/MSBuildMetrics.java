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

import java.util.ArrayList;
import java.util.List;

import org.sonar.api.measures.Metric;
import org.sonar.api.measures.Metrics;

/**
 * {@inheritDoc}
 */
public class MSBuildMetrics implements Metrics {

  public static final Metric DGMLDIAGRAMSIZE = new Metric.Builder("DIAGRAM-DGML-SIZE", "Dgml diagram size", Metric.ValueType.INT)
        .setDirection(Metric.DIRECTION_NONE)
        .setQualitative(true)
        .setDomain("Msbuild")
        .create();
  
  public static final Metric DGMLDIAGRAM = new Metric.Builder("DIAGRAM-DGML", "Dgml diagram", Metric.ValueType.STRING)
        .setDirection(Metric.DIRECTION_NONE)
        .setQualitative(false)
        .setDomain("Msbuild")
        .setDeleteHistoricalData(true)
        .create();  

  @Override
  public List<Metric> getMetrics() {
    List<Metric> list = new ArrayList<>();
    list.add(DGMLDIAGRAM);
    list.add(DGMLDIAGRAMSIZE);
    return list;
  }
}
