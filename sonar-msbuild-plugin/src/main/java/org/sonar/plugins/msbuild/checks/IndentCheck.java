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
 * Perform check for indenting of elements.
 * 
 * @author Matthijs Galesloot
 */
@Rule(key = "IndentCheck",
      name = "Indentation Check",
      description = "Indentation should be correct",            
  priority = Priority.MINOR)
@BelongsToProfile(title = CheckRepository.PROFILE_NAME, priority = Priority.MINOR)
public class IndentCheck extends AbstractXmlCheck {

  @RuleProperty(key = "indentSize", defaultValue = "4")
  private int indentSize = 2;

  @RuleProperty(key = "tabSize", defaultValue = "4")
  private int tabSize = 2;

  /**
   * Collect the indenting whitespace before this node.
   */
  private int collectIndent(Node node) {
    int indent = 0;
    for (Node sibling = node.getPreviousSibling(); sibling != null; sibling = sibling.getPreviousSibling()) {
      switch (sibling.getNodeType()) {
        case Node.COMMENT_NODE:
        case Node.ELEMENT_NODE:
          return indent;
        case Node.TEXT_NODE:
          String text = sibling.getTextContent();
          if (StringUtils.isWhitespace(text)) {
            for (int i = text.length() - 1; i >= 0; i--) {
              char c = text.charAt(i);
              switch (c) {
                case '\n':
                  // newline found, we are done
                  return indent;
                case '\t':
                  // add tabsize
                  indent += tabSize;
                  break;
                case ' ':
                  // add one space
                  indent++;
                  break;
                default:
                  break;
              }
            }
          } else {
            // non whitespace found, we are done
            return indent;
          }
          break;
        default:
          break;
      }
    }
    return indent;
  }

  /**
   * Get the depth of this node in the node hierarchy.
   */
  private int getDepth(Node node) {
    int depth = 0;
    for (Node parent = node.getParentNode(); parent.getParentNode() != null; parent = parent.getParentNode()) {
      depth++;
    }
    return depth;
  }

  public int getIndentSize() {
    return indentSize;
  }

  public int getTabSize() {
    return tabSize;
  }

  public void setIndentSize(int indentSize) {
    this.indentSize = indentSize;
  }

  public void setTabSize(int tabSize) {
    this.tabSize = tabSize;
  }

  @Override
  public void validate(XmlSourceCode xmlSourceCode) {
    setWebSourceCode(xmlSourceCode);

    Document document = getWebSourceCode().getDocument(false);
    if (document != null && document.getDocumentElement() != null) {
      validateIndent(document.getDocumentElement());
    }
  }

  /**
   * Validate the indent for this node.
   */
  private void validateIndent(Node node) {

    int depth = getDepth(node);
    int indent = collectIndent(node);

    if (depth * indentSize != indent) {
      createViolation(SaxParser.getLineNumber(node), "Wrong indentation");
    }

    // check the child elements
    for (Node child = node.getFirstChild(); child != null; child = child.getNextSibling()) {
      switch (child.getNodeType()) {
        case Node.ELEMENT_NODE:
        case Node.COMMENT_NODE:
          validateIndent(child);
          break;
        default:
          break;
      }
    }
  }
}
