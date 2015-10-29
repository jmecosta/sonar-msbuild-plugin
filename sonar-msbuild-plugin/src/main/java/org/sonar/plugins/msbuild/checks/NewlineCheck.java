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

import org.apache.commons.lang.StringUtils;
import org.sonar.check.BelongsToProfile;
import org.sonar.check.Priority;
import org.sonar.check.Rule;
import org.sonar.plugins.msbuild.parsers.SaxParser;
import org.w3c.dom.Document;
import org.w3c.dom.Node;

/**
 * Perform check for newline after elements.
 * 
 * @author Matthijs Galesloot
 */
@Rule(key = "NewlineCheck",
      name = "Newline Tab Check",
      description = "New lines should always be present",            
  priority = Priority.MINOR)
@BelongsToProfile(title = CheckRepository.PROFILE_NAME, priority = Priority.MINOR)
public class NewlineCheck extends AbstractXmlCheck {

  /**
   * Validate newlines for node.
   */
  private void validateNewline(Node node) {

    // check if we have a newline after the elements and after each childelement.
    boolean newline = false;
    Node lastChild = null;

    for (Node child = node.getFirstChild(); child != null; child = child.getNextSibling()) {
      switch (child.getNodeType()) {
        case Node.COMMENT_NODE:
        case Node.ELEMENT_NODE:
          // check if there is a new node before we have had any newlines.
          if (!newline) {
            createViolation(SaxParser.getLineNumber(child), "Node should be on the next line");
          } else {
            newline = false;
          }
          lastChild = child;
          break;
        case Node.TEXT_NODE:
          // newline check is OK if there is non whitespace or the whitespace contains a newline
          if (!StringUtils.isWhitespace(child.getTextContent()) || child.getTextContent().contains("\n")) {
            newline = true;
          }
          break;
        default:
          break;
      }
    }

    // validate first last child.
    validateLastChild(newline, lastChild);

    // check the child elements
    for (Node child = node.getFirstChild(); child != null; child = child.getNextSibling()) {
      if (child.getNodeType() == Node.ELEMENT_NODE) {
        validateNewline(child);
      }
    }
  }

  private void validateLastChild(boolean newlineAfterLastChild, Node lastChild) {
    if (!newlineAfterLastChild && lastChild != null) {
      createViolation(SaxParser.getLineNumber(lastChild), "Missing newline after last element");
    }
  }

  @Override
  public void validate(XmlSourceCode xmlSourceCode) {
    setWebSourceCode(xmlSourceCode);

    Document document = getWebSourceCode().getDocument(false);
    if (document != null && document.getDocumentElement() != null) {
      validateNewline(document.getDocumentElement());
    }
  }
}
