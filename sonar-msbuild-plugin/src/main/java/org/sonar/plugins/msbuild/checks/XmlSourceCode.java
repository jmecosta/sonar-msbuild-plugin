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
package org.sonar.plugins.msbuild.checks;

import org.apache.commons.io.FileUtils;
import org.sonar.api.resources.Resource;
import org.sonar.api.rules.Violation;
import org.sonar.api.utils.SonarException;
import org.sonar.plugins.msbuild.parsers.SaxParser;
import org.w3c.dom.Document;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.List;

/**
 * Checks and analyzes report measurements, violations and other findings in WebSourceCode.
 * 
 * @author Matthijs Galesloot
 */
public class XmlSourceCode {

  private String code;
  private final File file;

  private final Resource resource;
  private final List<Violation> violations = new ArrayList<Violation>();

  private Document documentNamespaceAware;

  private Document documentNamespaceUnaware;

  public XmlSourceCode(Resource resource, File file) {
    this.resource = resource;
    this.file = file;
  }

  public void addViolation(Violation violation) {
    this.violations.add(violation);
  }

  InputStream createInputStream() {
    if (file != null) {
      try {
        return FileUtils.openInputStream(file);
      } catch (IOException e) {
        throw new SonarException(e);
      }
    } else {
      return new ByteArrayInputStream(code.getBytes());
    }
  }

  protected Document getDocument(boolean namespaceAware) {
    InputStream inputStream = createInputStream();
    if (namespaceAware) {
      if (documentNamespaceAware == null) {
        documentNamespaceAware = new SaxParser().parseDocument(inputStream, true);
      }
      return documentNamespaceAware;
    } else {
      if (documentNamespaceUnaware == null) {
        documentNamespaceUnaware = new SaxParser().parseDocument(inputStream, false);
      }
      return documentNamespaceUnaware;
    }
  }

  public Resource getResource() {
    return resource;
  }

  public List<Violation> getViolations() {
    return violations;
  }

  public void setCode(String code) {
    this.code = code;
  }

  public File getFile() {
    return file;
  }

  @Override
  public String toString() {
    return resource.getLongName();
  }
}
