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
import org.sonar.check.RuleProperty;
import org.sonar.plugins.msbuild.parsers.SaxParser;
import org.w3c.dom.Document;
import org.w3c.dom.Node;

/**
 * Perform check for tab.
 * 
 * @author Matthijs Galesloot
 */
@Rule(key = "IllegalTabCheck",
      name = "Illegal Tab Check",
      description = "Tabs should not be used",        
      priority = Priority.MINOR)
@BelongsToProfile(title = CheckRepository.PROFILE_NAME, priority = Priority.MINOR)
public class IllegalTabCheck extends AbstractXmlCheck {

  @RuleProperty(key = "markAll", defaultValue = "false")
  private boolean markAll;

  private boolean validationReady;

  /**
   * Find Illegal tabs in whitespace.
   */
  private void findIllegalTabs(Node node) {

    // check whitespace in the node
    for (Node sibling = node.getPreviousSibling(); sibling != null; sibling = sibling.getPreviousSibling()) {
      if (sibling.getNodeType() == Node.TEXT_NODE) {
        String text = sibling.getTextContent();
        if (StringUtils.isWhitespace(text) && StringUtils.contains(text, "\t")) {
          createNewViolation(SaxParser.getLineNumber(sibling));
          // one violation for this node is enough
          break;
        }
      }
    }

    // check the child elements of the node
    for (Node child = node.getFirstChild(); !validationReady && child != null; child = child.getNextSibling()) {
      switch (child.getNodeType()) {
        case Node.ELEMENT_NODE:
          findIllegalTabs(child);
          break;
        default:
          break;
      }
    }
  }

  private void createNewViolation(int lineNumber) {
    if (!markAll) {
      createViolation(lineNumber, "Tab characters found (this is the first occurrence)");
      validationReady = true;
    } else {
      createViolation(lineNumber);
    }
  }

  @Override
  public void validate(XmlSourceCode xmlSourceCode) {
    setWebSourceCode(xmlSourceCode);

    validationReady = false;
    Document document = getWebSourceCode().getDocument(false);
    if (document != null && document.getDocumentElement() != null) {
      findIllegalTabs(document.getDocumentElement());
    }
  }

  public boolean isMarkAll() {
    return markAll;
  }

  public void setMarkAll(boolean markAll) {
    this.markAll = markAll;
  }
}
