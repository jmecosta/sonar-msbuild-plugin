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
package org.sonar.plugins.msbuild.projectchecker;

import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.rule.ActiveRule;
import org.sonar.api.rule.RuleKey;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.command.StreamConsumer;

import org.sonar.api.batch.sensor.Sensor;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.utils.log.Logger;

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.Map.Entry;
import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonar.api.batch.sensor.issue.NewIssue;
import org.sonar.api.batch.sensor.issue.NewIssueLocation;
import org.sonar.api.config.Configuration;
import org.sonar.api.utils.log.Loggers;
import org.sonar.plugins.msbuild.MSBuildRunnerExtractor;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.sonar.plugins.msbuild.MSBuildPlugin.IGNORE_LIST_INCLUDES_FOLDERS;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

public class MSBuildProjectCheckerExtensionSensor implements Sensor {

  private static final Logger LOG = Loggers.get(MSBuildProjectCheckerExtensionSensor.class);

  private final Configuration settings;
  private final MSBuildRunnerExtractor extractor;
  private final FileSystem fs;

  public static final String EXTERNAL_CUSTOM_RULES = "sonar.msbuild.projectchecker.customrules";
  public static final String PROJECT_CHECKER_PATH = "sonar.msbuild.prjectChecker.Path";
  public static String CHECKER_ENABLED = "sonar.msbuild.projectchecker.enabled";
  
  public MSBuildProjectCheckerExtensionSensor(
          Configuration settings,
          MSBuildRunnerExtractor extractor,
          FileSystem fs) {
    this.settings = settings;
    this.extractor = extractor;
    this.fs = fs;
  }

  @Override
  public void describe(SensorDescriptor descriptor) {
    descriptor.global().name("MSBuildProjectCheckerExtensionSensor");
  }

  @Override
  public void execute(SensorContext context) {
    if (!settings.getBoolean(CHECKER_ENABLED).isPresent()) {
      LOG.info("Project Checker not present - Disabled");
      return;
    }
    
    if (!settings.getBoolean(CHECKER_ENABLED).get()) {
      LOG.info("Project Checker - Disabled");
      return;
    } 
    
    try {
      analyze(context);
      importResults(context);
    } catch (Exception ex) {
      LOG.warn("Failed to execute sensor '{0}' msbuild checks are not going to be available", ex.getMessage());
    }
  }
  
  private String getEmptyStringOrValue(SensorContext ctx, String key) {
    if (ctx.config().get(key).isPresent()) {
      return ctx.config().get(key).get();
    }
    
    return "";    
  }
    
  private void analyze(SensorContext context) {       
    try {       
      String workdirRoot = context.fileSystem().workDir().getCanonicalPath();
      String projectRoot = context.fileSystem().baseDir().getCanonicalPath();
      
      StringBuilder sb = new StringBuilder();
      appendLine(sb, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
      appendLine(sb, "<AnalysisInput>");
      appendLine(sb, "  <Settings>");
      appendLine(sb, "    <Setting>");
      appendLine(sb, "      <Key>" + IGNORE_LIST_INCLUDES_FOLDERS + "</Key>");
      appendLine(sb, "      <Value>" + (MSBuildUtils.getStringArrayProperty(IGNORE_LIST_INCLUDES_FOLDERS, this.settings)) + "</Value>");
      appendLine(sb, "    </Setting>");
      appendLine(sb, "    <Setting>");
      appendLine(sb, "      <Key>" + EXTERNAL_CUSTOM_RULES + "</Key>");
      appendLine(sb, "      <Value>" + (MSBuildUtils.getStringArrayProperty(EXTERNAL_CUSTOM_RULES, this.settings)) + "</Value>");
      appendLine(sb, "    </Setting>");      
      appendLine(sb, "    <Setting>");
      appendLine(sb, "      <Key>ProjectRoot</Key>");
      appendLine(sb, "      <Value>" + projectRoot + "</Value>");
      appendLine(sb, "    </Setting>");
      appendLine(sb, "  </Settings>");
      appendLine(sb, "  <Rules>");
      for (ActiveRule activeRule : context.activeRules().findByRepository(MSBuildProjectCheckerRulesDefinition.REPOSITORY_KEY)) {
        appendLine(sb, "    <Rule>");
        Map<String, String> parameters = effectiveParameters(activeRule);
        appendLine(sb, "      <Key>" + parameters.get("RuleKey") + "</Key>");
        if (!parameters.isEmpty()) {
          appendLine(sb, "      <Parameters>");
          for (Entry<String, String> parameter : parameters.entrySet()) {
            appendLine(sb, "        <Parameter>");
            appendLine(sb, "          <Key>" + parameter.getKey() + "</Key>");
            appendLine(sb, "          <Value>" + parameter.getValue() + "</Value>");
            appendLine(sb, "        </Parameter>");
          }
          appendLine(sb, "      </Parameters>");
        }
        appendLine(sb, "    </Rule>");
      }
      appendLine(sb, "  </Rules>");
      appendLine(sb, "  <Files>");
      for (InputFile file : filesToAnalyze()) {
        appendLine(sb, "    <File>" + file.uri() + "</File>");
      }
      appendLine(sb, "  </Files>");
      appendLine(sb, "</AnalysisInput>");
      
      File analysisInput = toolInput();
      File analysisOutput = toolOutput();
      
      try {
        MSBuildUtils.writeStringToFile(analysisInput.getAbsolutePath(), sb.toString());
      } catch (IOException e) {
        LOG.error("Could not write settings to file '{0}'", e.getMessage());
        throw e;
      }
      
      File executableFile = extractor.projectCheckerFile(workdirRoot);
      LOG.info("Using ProjectChecker from:" + executableFile.getCanonicalPath());
      
      String username = getEmptyStringOrValue(context, "sonar.login");
      String password = getEmptyStringOrValue(context, "sonar.password");
      

      String host = getEmptyStringOrValue(context, "sonar.host.url");
      if(!context.config().get("sonar.projectKey").isPresent()) {
        LOG.info("ProjectChecker Skipped because projectKey not available...");
      }
      String projectKey = context.config().get("sonar.projectKey").get();
    
      Command command;
      if (OsUtils.isWindows()) {
        command = Command.create(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/h:" + host)
                .addArgument("/k:" + projectKey)
                .addArgument("/o:" + analysisOutput.getAbsolutePath());
      } else {
        command = Command.create("mono")
                .addArgument(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/h:" + host)                
                .addArgument("/k:" + projectKey)
                .addArgument("/o:" + analysisOutput.getAbsolutePath());
      }
      
      command.addArgument("/u:" + username);
      LOG.info(command.toCommandLine());
      
      if (!password.equals("")){
        command.addArgument("/p:" + password);
      } 
      
      CommandExecutor.create().execute(command, new LogInfoStreamConsumer(), new LogErrorStreamConsumer(), Integer.MAX_VALUE);
    } catch (IOException ex) {
        LOG.info("Project Checker failed to execute, will skip");
        LOG.warn("Cannot execute project Checker, details: '" + ex + "'");
    }
  }

  private static Map<String, String> effectiveParameters(ActiveRule activeRule) {
    Map<String, String> builder = new HashMap<>();

    if (!"".equals(activeRule.templateRuleKey())) {
      builder.put("RuleKey", activeRule.ruleKey().rule());
    }

    for (Map.Entry<String, String> param : activeRule.params().entrySet()) {
      builder.put(param.getKey(), param.getValue());
    }

    return builder;
  }

  private void importResults(SensorContext context) throws IOException, XMLStreamException {
    File analysisOutput = toolOutput();

    new AnalysisResultImporter(context).parse(analysisOutput, context);
  }

  private static class AnalysisResultImporter {

    private final FileSystem fs;
    private XMLStreamReader stream;

    private AnalysisResultImporter(SensorContext context) {
      this.fs = context.fileSystem();
    }

    private void parse(File file, SensorContext context) throws IOException, XMLStreamException {
      InputStreamReader reader;
      XMLInputFactory xmlFactory = XMLInputFactory.newInstance();

      try {
        reader = new InputStreamReader(new FileInputStream(file), StandardCharsets.UTF_8);
        stream = xmlFactory.createXMLStreamReader(reader);

        while (stream.hasNext()) {
          if (stream.next() == XMLStreamConstants.START_ELEMENT) {
            String tagName = stream.getLocalName();

            if ("File".equals(tagName)) {
              handleFileTag(context);
            }
          }
        }
        
        reader.close();
      } catch (IOException | XMLStreamException e) {        
        closeXmlStream();
        LOG.error("Not able to parse file : {0}", e.getMessage());
        throw e; 
      }      
    }

    private void closeXmlStream() {
      if (stream != null) {
        try {
          stream.close();
        } catch (XMLStreamException e) {
          LOG.error("Not able to close stream file : {0}", e.getMessage());
        }
      }
    }

    private void handleFileTag(SensorContext context) throws XMLStreamException {
      InputFile inputFile = null;

      while (stream.hasNext()) {
        int next = stream.next();

        if (next == XMLStreamConstants.END_ELEMENT && "File".equals(stream.getLocalName())) {
          break;
        } else if (next == XMLStreamConstants.START_ELEMENT) {
          String tagName = stream.getLocalName();

          if ("Path".equals(tagName)) {
            String path = stream.getElementText();
            inputFile = fs.inputFile(fs.predicates().hasAbsolutePath(path));
          } if ("Issues".equals(tagName)) {
            handleIssuesTag(context, inputFile);
          }
        }
      }
    }

    private void handleIssuesTag(SensorContext context, InputFile inputFile) throws XMLStreamException {

      while (stream.hasNext()) {
        int next = stream.next();

        if (next == XMLStreamConstants.END_ELEMENT && "Issues".equals(stream.getLocalName())) {
          break;
        } else if (next == XMLStreamConstants.START_ELEMENT) {
          String tagName = stream.getLocalName();

          if ("Issue".equals(tagName)) {
            handleIssueTag(context, inputFile);
          }
        }
      }
    }

    private void handleIssueTag(SensorContext sensorContext, InputFile inputFile) throws XMLStreamException {
      String id = null;
      String message = null;
      int line = 1;

      while (stream.hasNext()) {
        int next = stream.next();

        if (next == XMLStreamConstants.END_ELEMENT && "Issue".equals(stream.getLocalName())) {

          NewIssue newIssue = sensorContext.newIssue().forRule(RuleKey.of(MSBuildProjectCheckerRulesDefinition.REPOSITORY_KEY, id));
          NewIssueLocation location = newIssue.newLocation()
            .on(inputFile)
            .at(inputFile.selectLine(line))
            .message(message);

          newIssue.at(location);
          newIssue.save();
            
          break;
        } else if (next == XMLStreamConstants.START_ELEMENT) {
          String tagName = stream.getLocalName();

          if ("Id".equals(tagName)) {
            id = stream.getElementText();
          } else if ("Line".equals(tagName)) {
            line = Integer.parseInt(stream.getElementText());
          } else if ("Message".equals(tagName)) {
            message = stream.getElementText();
          }
        }
      }            
    }   
  }

  private void appendLine(StringBuilder sb, String line) {
    sb.append(line);
    sb.append("\r\n");
  }

  private static class LogInfoStreamConsumer implements StreamConsumer {

    @Override
    public void consumeLine(String line) {
      LOG.info(line);
    }

  }

  private static class LogErrorStreamConsumer implements StreamConsumer {

    @Override
    public void consumeLine(String line) {
      LOG.error(line);
    }

  }

  private Iterable<InputFile> filesToAnalyze() {
    return fs.inputFiles(fs.predicates().hasLanguage(MSBuildLanguage.KEY));
  }

  private File toolInput() {
    return new File(fs.workDir(), "msbuild-analysis-input.xml");
  }

  private File toolOutput() {
    return toolOutput(fs);
  }

  private static File toolOutput(FileSystem fileSystem) {
    return new File(fileSystem.workDir(), "msbuild-analysis-output.xml");
  }


}
