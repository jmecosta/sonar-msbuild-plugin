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

import org.apache.xml.utils.PrefixResolver;
import org.apache.xml.utils.PrefixResolverDefault;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.utils.SonarException;
import org.sonar.check.Cardinality;
import org.sonar.check.Priority;
import org.sonar.check.Rule;
import org.sonar.check.RuleProperty;
import org.sonar.plugins.msbuild.parsers.SaxParser;
import org.w3c.dom.Document;
import org.w3c.dom.NodeList;

import javax.xml.namespace.NamespaceContext;
import javax.xml.xpath.XPath;
import javax.xml.xpath.XPathConstants;
import javax.xml.xpath.XPathExpression;
import javax.xml.xpath.XPathExpressionException;
import javax.xml.xpath.XPathFactory;

import java.util.Iterator;

@Rule(key = "XPathCheck",
      name = "XPAth Check",
      description = "Validate Xpath query",            
        priority = Priority.MAJOR, cardinality = Cardinality.MULTIPLE)
public class XPathCheck extends AbstractXmlCheck {

  private static final Logger LOG = LoggerFactory.getLogger(XPathCheck.class);

  @RuleProperty(key = "expression", type = "TEXT")
  private String expression;

  @RuleProperty(key = "filePattern")
  private String filePattern;

  @RuleProperty(key = "message")
  private String message;

  private static final class DocumentNamespaceContext implements NamespaceContext {

    private final PrefixResolver resolver;

    private DocumentNamespaceContext(PrefixResolver resolver) {
      this.resolver = resolver;
    }

    public String getNamespaceURI(String prefix) {
      return resolver.getNamespaceForPrefix(prefix);
    }

    // Dummy implementation - not used!
    public String getPrefix(String uri) {
      return null;
    }

    // Dummy implementation - not used!
    public Iterator<Object> getPrefixes(String val) {
      return null;
    }
  }

  private void evaluateXPath() {

    Document document = getWebSourceCode().getDocument(expression.contains(":"));

    if (document == null) {
      LOG.warn("XPath check cannot be evaluated on {} because document is not valid", getWebSourceCode().getFile());
    } else {
      try {
        NodeList nodes = (NodeList) getXPathExpressionForDocument(document).evaluate(document, XPathConstants.NODESET);
        for (int i = 0; i < nodes.getLength(); i++) {

          int lineNumber = SaxParser.getLineNumber(nodes.item(i));
          if (message == null) {
            createViolation(lineNumber);
          } else {
            createViolation(lineNumber, message);
          }
        }
      } catch (XPathExpressionException e) {
        throw new SonarException(e);
      }
    }
  }

  public String getExpression() {
    return expression;
  }

  public String getFilePattern() {
    return filePattern;
  }

  public String getMessage() {
    return message;
  }

  private XPathExpression getXPathExpressionForDocument(Document document) {
    if (expression != null) {
      try {
        XPath xpath = XPathFactory.newInstance().newXPath();
        PrefixResolver resolver = new PrefixResolverDefault(document.getDocumentElement());
        xpath.setNamespaceContext(new DocumentNamespaceContext(resolver));
        return xpath.compile(expression);
      } catch (XPathExpressionException e) {
        throw new SonarException(e);
      }
    }
    return null;
  }

  public void setExpression(String expression) {
    this.expression = expression;
  }

  public void setFilePattern(String filePattern) {
    this.filePattern = filePattern;
  }

  public void setMessage(String message) {
    this.message = message;
  }

  @Override
  public void validate(XmlSourceCode xmlSourceCode) {
    setWebSourceCode(xmlSourceCode);

    if (expression != null && isFileIncluded(filePattern)) {
      evaluateXPath();
    }
  }
}
