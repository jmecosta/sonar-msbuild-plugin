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
package org.sonar.plugins.msbuild.parsers;

import java.io.IOException;
import java.io.InputStream;

import javax.xml.parsers.SAXParser;

import org.apache.commons.io.IOUtils;
import org.apache.xerces.impl.Constants;
import org.sonar.api.utils.SonarException;
import org.xml.sax.Attributes;
import org.xml.sax.SAXException;
import org.xml.sax.SAXParseException;
import org.xml.sax.XMLReader;
import org.xml.sax.ext.LexicalHandler;
import org.xml.sax.helpers.DefaultHandler;

/**
 * Parse the file quickly only to detect the sDTD or Schema.
 * 
 * @author Matthijs Galesloot
 */
public final class DetectSchemaParser extends AbstractParser {

  /**
   * Doctype declaration in a Document.
   */
  public static class Doctype {

    private String dtd;
    private String namespace;

    public String getDtd() {
      return dtd;
    }

    public String getNamespace() {
      return namespace;
    }
  }

  /**
   * Exception to stop the parser from further processing.  
   */
  private static class StopParserException extends SAXException {

    private static final long serialVersionUID = 1L;
    
  }
  
  private static class Handler extends DefaultHandler implements LexicalHandler {

    private Doctype doctype = new Doctype();

    public void comment(char[] arg0, int arg1, int arg2) throws SAXException {
      // empty
    }

    public void endCDATA() throws SAXException {
      // empty
    }

    public void endDTD() throws SAXException {
      // empty
    }

    public void endEntity(String name) throws SAXException {
      // empty
    }

    @Override
    public void fatalError(SAXParseException e) throws SAXException {
      // ignore
    }

    public void startCDATA() throws SAXException {
      // empty
    }

    public void startDTD(String name, String publicId, String systemId) throws SAXException {
      doctype.dtd = publicId;
    }

    @Override
    public void startElement(String uri, String localName, String qName, Attributes attributes) throws SAXException {
      doctype.namespace = attributes.getValue("xmlns");

      // we are done, cause the parser to stop
      throw new StopParserException();
    }

    public void startEntity(String name) throws SAXException {
      // empty
    }
  }

  /**
   * Find the Doctype (DTD or schema).
   */
  public Doctype findDoctype(InputStream input) {
    Handler handler = new Handler();

    try {
      SAXParser parser = newSaxParser();
      XMLReader xmlReader = parser.getXMLReader();
      xmlReader.setFeature(Constants.XERCES_FEATURE_PREFIX + "continue-after-fatal-error", true);
      xmlReader.setProperty("http://xml.org/sax/properties/lexical-handler", handler);
      parser.parse(input, handler);
      return handler.doctype;
    } catch (IOException e) {
      throw new SonarException(e);
    } catch (StopParserException e) {
      return handler.doctype; 
    } catch (SAXException e) {
      throw new SonarException(e);
    } finally {
      IOUtils.closeQuietly(input);
    }
  }
}
