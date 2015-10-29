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

import net.sourceforge.pmd.cpd.AnyLanguage;
import net.sourceforge.pmd.cpd.Tokenizer;
import org.sonar.api.batch.AbstractCpdMapping;
import org.sonar.api.resources.Language;

/**
 * {@inheritDoc}
 */
public final class MSBuildCpdMapping extends AbstractCpdMapping {

  private final AnyLanguage language = new AnyLanguage("msbuild");
  private MSBuildLanguage lang;

  /**
   *  {@inheritDoc}
   */
  public MSBuildCpdMapping(MSBuildLanguage lang) {
    this.lang = lang;
  }

  /**
   *  {@inheritDoc}
   */
  public Language getLanguage() {
    return lang;
  }

  /**
   *  {@inheritDoc}
   */
  public Tokenizer getTokenizer() {
    return language.getTokenizer();
  }
}
