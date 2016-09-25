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
package org.sonar.plugins.msbuild.widgets;

import org.sonar.api.web.*;

@UserRole(UserRole.USER)
@Description("Displays the configured text. Intended to enable dashboard description.")
@WidgetCategory({"Msbuild"})
public class DmglXmlWidget extends AbstractRubyTemplate implements RubyRailsWidget {
  
  public String getId() {
    return "dgml_xml_viewer";
  }

  public String getTitle() {
    return "Display dgml in xml format";
  }

  @Override
  protected String getTemplatePath() {
    return "/dgml_xml_viewer_widget.html.erb";
  }
  
}
