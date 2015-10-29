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
import org.sonar.api.rules.Rule;
import org.sonar.api.rules.Violation;
import org.sonar.api.utils.WildcardPattern;

/**
 * Abtract superclass for checks.
 *
 * @author Matthijs Galesloot
 */
public abstract class AbstractXmlCheck {

  private Rule rule;
  private XmlSourceCode xmlSourceCode;

  protected final void createViolation(Integer linePosition) {
    createViolation(linePosition, rule.getDescription());
  }

  protected final void createViolation(Integer linePosition, String message) {
    Violation violation = Violation.create(rule, getWebSourceCode().getResource());
    violation.setMessage(message);
    violation.setLineId(linePosition);
    getWebSourceCode().addViolation(violation);
  }

  protected XmlSourceCode getWebSourceCode() {
    return xmlSourceCode;
  }

  /**
   * Check with ant style filepattern if the file is included.
   */
  protected boolean isFileIncluded(String filePattern) {
    if (filePattern != null) {
      String fileName = getWebSourceCode().getResource().getKey();
      WildcardPattern matcher = WildcardPattern.create(filePattern);
      return matcher.match(fileName);
    } else {
      return true;
    }
  }
  
  public Rule getRule() {
    return this.rule;
  }
  
  public final void setRule(Rule rule) {
    this.rule = rule;
  }

  protected void setWebSourceCode(XmlSourceCode xmlSourceCode) {
    this.xmlSourceCode = xmlSourceCode;
  }

  public String[] trimSplitCommaSeparatedList(String value) {
    String[] tokens = StringUtils.split(value, ",");
    for (int i = 0; i < tokens.length; i++) {
      tokens[i] = tokens[i].trim();
    }
    return tokens;
  }

  public abstract void validate(XmlSourceCode xmlSourceCode);
}
