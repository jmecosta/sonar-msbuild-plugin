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

package org.sonar.plugins.msbuild.parsers;

import java.io.IOException;
import java.io.InputStream;
import javax.xml.parsers.ParserConfigurationException;

import javax.xml.parsers.SAXParser;

import org.apache.xerces.impl.Constants;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.plugins.msbuild.MSBuildAbastractParser;
import org.xml.sax.Attributes;
import org.xml.sax.Locator;
import org.xml.sax.SAXException;
import org.xml.sax.SAXParseException;
import org.xml.sax.XMLReader;
import org.xml.sax.ext.LexicalHandler;
import org.xml.sax.helpers.DefaultHandler;

/**
 * Comment Counting in XML files
 * Original from: https://github.com/SonarSource/sonar-xml
 * Modified to fit msbuild files
 */
public final class MSBuildLineCountParser extends MSBuildAbastractParser {
  
  private static class CommentHandler extends DefaultHandler implements LexicalHandler {

    private int currentCommentLine = -1;
    private Locator locator;
    private int numCommentLines;

    @Override
    public void characters(char[] ch, int start, int length) throws SAXException {
      checkComment();
    }

    private void checkComment() {
      if (currentCommentLine >= 0 && locator.getLineNumber() > currentCommentLine) {
        numCommentLines++;
        currentCommentLine = -1;
      }
    }

    @Override
    public void comment(char[] ch, int start, int length) throws SAXException {
      for (int i = 0; i < length; i++) {
        if (ch[start + i] == '\n') {
          numCommentLines++;
        }
      }
      currentCommentLine = locator.getLineNumber();
    }

    @Override
    public void endCDATA() throws SAXException {
      // empty
    }

    @Override
    public void endDTD() throws SAXException {
      // empty
    }

    @Override
    public void endEntity(String name) throws SAXException {
      // empty
    }

    @Override
    public void fatalError(SAXParseException e) throws SAXException {
      if (e.getLocalizedMessage().contains("The reference to entity \"null\"")) {
        throw e;
      }
    }

    protected int getNumCommentLines() {
      return numCommentLines;
    }

    @Override
    public void setDocumentLocator(Locator locator) {
      this.locator = locator;
    }

    @Override
    public void startCDATA() throws SAXException {
      // empty
    }

    @Override
    public void startDTD(String name, String publicId, String systemId) throws SAXException {
      // empty
    }

    @Override
    public void startElement(String uri, String localName, String qName, Attributes attributes) throws SAXException {
      checkComment();
    }

    @Override
    public void startEntity(String name) throws SAXException {
      // empty
    }
  }

  public int countLinesOfComment(InputStream input, InputFile file) {
    try {
      SAXParser parser = newSaxParser();
      XMLReader xmlReader = parser.getXMLReader();
      xmlReader.setFeature(Constants.XERCES_FEATURE_PREFIX + "continue-after-fatal-error", true);
      CommentHandler commentHandler = new CommentHandler();
      xmlReader.setProperty("http://xml.org/sax/properties/lexical-handler", commentHandler);
      parser.parse(input, commentHandler);
      return commentHandler.getNumCommentLines();
    } catch (IOException | SAXException | ParserConfigurationException ex ) {
      LOG.error("countLinesOfComment fails for '{}' => '{}'", file.absolutePath(), ex.getMessage());
      return 0;
    }
  }
}