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
package org.sonar.plugins.msbuild.projectchecker;

import com.google.common.base.Charsets;
import com.google.common.base.Preconditions;
import com.google.common.base.Throwables;
import com.google.common.collect.ImmutableMap;
import com.google.common.collect.Maps;
import com.google.common.io.Closeables;
import com.google.common.io.Files;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.Sensor;
import org.sonar.api.batch.SensorContext;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.component.ResourcePerspectives;
import org.sonar.api.config.Settings;
import org.sonar.api.issue.Issuable;
import org.sonar.api.issue.Issuable.IssueBuilder;
import org.sonar.api.issue.NoSonarFilter;
import org.sonar.api.measures.FileLinesContextFactory;
import org.sonar.api.profiles.RulesProfile;
import org.sonar.api.resources.Project;
import org.sonar.api.rule.RuleKey;
import org.sonar.api.rules.ActiveRule;
import org.sonar.api.rules.ActiveRuleParam;
import org.sonar.api.rules.Rule;
import org.sonar.api.rules.RuleParam;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.command.StreamConsumer;

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Map;
import java.util.Map.Entry;
import org.sonar.api.batch.bootstrap.ProjectReactor;
import org.sonar.plugins.msbuild.MSBuildRunnerExtractor;
import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.sonar.plugins.msbuild.MSBuildPlugin.IGNORE_LIST_INCLUDES_FOLDERS;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

public class MSBuildProjectCheckerExtensionSensor implements Sensor {

  private static final Logger LOG = LoggerFactory.getLogger(MSBuildProjectCheckerExtensionSensor.class);

  private final Settings settings;
  private final MSBuildRunnerExtractor extractor;
  private final FileSystem fs;
  private final FileLinesContextFactory fileLinesContextFactory;
  private final NoSonarFilter noSonarFilter;
  private final RulesProfile ruleProfile;
  private final ResourcePerspectives perspectives;
  private final ProjectReactor reactor;

  public static final String EXTERNAL_CUSTOM_RULES = "sonar.msbuild.projectchecker.customrules";
  public static final String PROJECT_CHECKER_PATH = "sonar.msbuild.prjectChecker.Path";
     
  public MSBuildProjectCheckerExtensionSensor(Settings settings, MSBuildRunnerExtractor extractor, FileSystem fs, FileLinesContextFactory fileLinesContextFactory,
    NoSonarFilter noSonarFilter, RulesProfile ruleProfile,
    ResourcePerspectives perspectives, ProjectReactor reactor) {
    this.settings = settings;
    this.extractor = extractor;
    this.fs = fs;
    this.fileLinesContextFactory = fileLinesContextFactory;
    this.noSonarFilter = noSonarFilter;
    this.ruleProfile = ruleProfile;
    this.perspectives = perspectives;
    this.reactor = reactor;
  }

  @Override
  public boolean shouldExecuteOnProject(Project project) {
    return filesToAnalyze().iterator().hasNext();
  }

  @Override
  public void analyse(Project project, SensorContext context) {    
    analyze();
    importResults(project, context);
  }

  private void analyze() {       
    try {       
      String projectRoot = reactor.getRoot().getBaseDir().getCanonicalPath();
      
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
      for (ActiveRule activeRule : ruleProfile.getActiveRulesByRepository(MSBuildProjectCheckerRulesDefinition.REPOSITORY_KEY)) {
        appendLine(sb, "    <Rule>");
        Rule template = activeRule.getRule().getTemplate();
        String ruleKey = template == null ? activeRule.getRuleKey() : template.getKey();
        appendLine(sb, "      <Key>" + ruleKey + "</Key>");
        Map<String, String> parameters = effectiveParameters(activeRule);
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
      for (File file : filesToAnalyze()) {
        appendLine(sb, "    <File>" + file.getAbsolutePath() + "</File>");
      }
      appendLine(sb, "  </Files>");
      appendLine(sb, "</AnalysisInput>");
      
      File analysisInput = toolInput();
      File analysisOutput = toolOutput();
      
      try {
        Files.write(sb, analysisInput, Charsets.UTF_8);
      } catch (IOException e) {
        throw Throwables.propagate(e);
      }
      
      File executableFile = extractor.projectCheckerFile();
      
      Command command;
      if (OsUtils.isWindows()) {
        command = Command.create(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/o:" + analysisOutput.getAbsolutePath());
      } else {
        command = Command.create("mono")
                .addArgument(executableFile.getAbsolutePath())
                .addArgument("/i:" + analysisInput.getAbsolutePath())
                .addArgument("/o:" + analysisOutput.getAbsolutePath());
      }
      
      LOG.debug(command.toCommandLine());
      CommandExecutor.create().execute(command, new LogInfoStreamConsumer(), new LogErrorStreamConsumer(), Integer.MAX_VALUE);
    } catch (IOException ex) {
        String msg = new StringBuilder()
          .append("Cannot execute project checker, details: '")
          .append(ex)
          .append("'")
          .toString();
        throw new IllegalStateException(msg, ex);
    }
  }

  private static Map<String, String> effectiveParameters(ActiveRule activeRule) {
    Map<String, String> builder = Maps.newHashMap();

    if (activeRule.getRule().getTemplate() != null) {
      builder.put("RuleKey", activeRule.getRuleKey());
    }

    for (ActiveRuleParam param : activeRule.getActiveRuleParams()) {
      builder.put(param.getKey(), param.getValue());
    }

    for (RuleParam param : activeRule.getRule().getParams()) {
      if (!builder.containsKey(param.getKey())) {
        builder.put(param.getKey(), param.getDefaultValue());
      }
    }

    return ImmutableMap.copyOf(builder);
  }

  private void importResults(Project project, SensorContext context) {
    File analysisOutput = toolOutput();

    new AnalysisResultImporter(project, context, fs, fileLinesContextFactory, noSonarFilter, perspectives).parse(analysisOutput);
  }

  private static class AnalysisResultImporter {

    private final SensorContext context;
    private final FileSystem fs;
    private XMLStreamReader stream;
    private final FileLinesContextFactory fileLinesContextFactory;
    private final NoSonarFilter noSonarFilter;
    private final ResourcePerspectives perspectives;

    public AnalysisResultImporter(Project project, SensorContext context, FileSystem fs, FileLinesContextFactory fileLinesContextFactory, NoSonarFilter noSonarFilter,
      ResourcePerspectives perspectives) {
      this.context = context;
      this.fs = fs;
      this.fileLinesContextFactory = fileLinesContextFactory;
      this.noSonarFilter = noSonarFilter;
      this.perspectives = perspectives;
    }

    public void parse(File file) {
      InputStreamReader reader = null;
      XMLInputFactory xmlFactory = XMLInputFactory.newInstance();

      try {
        reader = new InputStreamReader(new FileInputStream(file), Charsets.UTF_8);
        stream = xmlFactory.createXMLStreamReader(reader);

        while (stream.hasNext()) {
          if (stream.next() == XMLStreamConstants.START_ELEMENT) {
            String tagName = stream.getLocalName();

            if ("File".equals(tagName)) {
              handleFileTag();
            }
          }
        }
      } catch (IOException e) {
        throw Throwables.propagate(e);
      } catch (XMLStreamException e) {
        throw Throwables.propagate(e);
      } finally {
        closeXmlStream();
        Closeables.closeQuietly(reader);
      }

      return;
    }

    private void closeXmlStream() {
      if (stream != null) {
        try {
          stream.close();
        } catch (XMLStreamException e) {
          throw Throwables.propagate(e);
        }
      }
    }

    private void handleFileTag() throws XMLStreamException {
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
            // TODO Better message
            Preconditions.checkState(inputFile != null);
            handleIssuesTag(inputFile);
          }
        }
      }
    }

    private void handleIssuesTag(InputFile inputFile) throws XMLStreamException {
      Issuable issuable = perspectives.as(Issuable.class, org.sonar.api.resources.File.create(inputFile.relativePath()));

      while (stream.hasNext()) {
        int next = stream.next();

        if (next == XMLStreamConstants.END_ELEMENT && "Issues".equals(stream.getLocalName())) {
          break;
        } else if (next == XMLStreamConstants.START_ELEMENT) {
          String tagName = stream.getLocalName();

          if ("Issue".equals(tagName) && issuable != null) {
            handleIssueTag(issuable);
          }
        }
      }
    }

    private void handleIssueTag(Issuable issuable) throws XMLStreamException {
      IssueBuilder builder = issuable.newIssueBuilder();

      String id = null;
      String message = null;

      while (stream.hasNext()) {
        int next = stream.next();

        if (next == XMLStreamConstants.END_ELEMENT && "Issue".equals(stream.getLocalName())) {
          Preconditions.checkState(!"AnalyzerDriver".equals(id), "The analyzer failed, double check rule parameters or disable failing rules: " + message);

          builder.ruleKey(RuleKey.of(MSBuildProjectCheckerRulesDefinition.REPOSITORY_KEY, id));
          builder.message(message);
          
          issuable.addIssue(builder.build());
          break;
        } else if (next == XMLStreamConstants.START_ELEMENT) {
          String tagName = stream.getLocalName();

          if ("Id".equals(tagName)) {
            id = stream.getElementText();
          } else if ("Line".equals(tagName)) {
            builder.line(Integer.parseInt(stream.getElementText()));
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

  private Iterable<File> filesToAnalyze() {
    return fs.files(fs.predicates().hasLanguage(MSBuildLanguage.KEY));
  }

  private File toolInput() {
    return new File(fs.workDir(), "msbuild-analysis-input.xml");
  }

  private File toolOutput() {
    return toolOutput(fs);
  }

  public static File toolOutput(FileSystem fileSystem) {
    return new File(fileSystem.workDir(), "msbuild-analysis-output.xml");
  }


}
