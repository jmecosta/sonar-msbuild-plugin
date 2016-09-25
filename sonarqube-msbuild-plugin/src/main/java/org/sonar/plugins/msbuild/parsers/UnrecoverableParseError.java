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
package org.sonar.plugins.msbuild.parsers;

import org.xml.sax.SAXParseException;

/**
 * Exception for a parse error from which the parser cannot recover.
 */
class UnrecoverableParseError extends RuntimeException {

  static final String FAILUREMESSAGE = "The reference to entity \"null\"";

  private static final long serialVersionUID = 1L;

  public UnrecoverableParseError(SAXParseException e) {
    super(e);
  }
}
