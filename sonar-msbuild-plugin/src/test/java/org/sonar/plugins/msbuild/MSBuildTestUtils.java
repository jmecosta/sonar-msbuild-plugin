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

package org.sonar.plugins.msbuild;

import org.sonar.plugins.msbuild.MSBuildLanguage;
import static org.junit.Assert.fail;
import static org.mockito.Matchers.anyObject;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.io.File;
import java.net.URISyntaxException;
import java.net.URL;
import java.nio.charset.Charset;
import java.util.ArrayList;
import java.util.List;

import org.apache.tools.ant.DirectoryScanner;
import org.sonar.api.config.Settings;
import org.sonar.api.resources.InputFile;
import org.sonar.api.resources.Project;
import org.sonar.api.resources.ProjectFileSystem;
import org.sonar.api.rules.Rule;
import org.sonar.api.rules.RuleFinder;
import org.sonar.api.rules.RuleQuery;
import org.sonar.plugins.msbuild.utils.MSBuildUtils;

public class MSBuildTestUtils{
  public static RuleFinder mockRuleFinder(){
    Rule ruleMock = Rule.create("", "", "");
    RuleFinder ruleFinder = mock(RuleFinder.class);
    when(ruleFinder.findByKey((String) anyObject(),
        (String) anyObject())).thenReturn(ruleMock);
    when(ruleFinder.find((RuleQuery) anyObject())).thenReturn(ruleMock);
    return ruleFinder;
  }

  public static File loadResource(String resourceName) {
    URL resource = MSBuildTestUtils.class.getResource(resourceName);
    File resourceAsFile = null;
    try{
      resourceAsFile = new File(resource.toURI());
    } catch (URISyntaxException e) {
      System.out.println("Cannot load resource: " + resourceName);
    }
    
    return resourceAsFile;
  }

  /**
   * @return  default mock project
   */
  public static Project mockProject() {
    File baseDir;
    baseDir = loadResource("/org/sonar/plugins/msbuild/");  //we skip "SampleProject" dir because report dirs as here
    
    List<File> sourceDirs = new ArrayList<File>();
    sourceDirs.add(loadResource("/org/sonar/plugins/msbuild/SampleProject/") );
             
    return mockProject(baseDir, sourceDirs);
  }
  
  /**
   * Mock project
   * @param baseDir project base dir
   * @param sourceFiles project source files
   * @return  mocked project
   */
  public static Project mockProject(File baseDir, List<File> sourceDirs) {
    List<File> mainSourceFiles = scanForSourceFiles(sourceDirs);
    
    List<InputFile> mainFiles = fromSourceFiles(mainSourceFiles);
    
    ProjectFileSystem fileSystem = mock(ProjectFileSystem.class);
    when(fileSystem.getBasedir()).thenReturn(baseDir);
    when(fileSystem.getSourceCharset()).thenReturn(Charset.defaultCharset());
    when(fileSystem.getSourceFiles(mockMSBuildLanguage())).thenReturn(mainSourceFiles);
    when(fileSystem.mainFiles(MSBuildPlugin.KEY)).thenReturn(mainFiles);
    when(fileSystem.getSourceDirs()).thenReturn(sourceDirs);


    Project project = mock(Project.class);
    when(project.getFileSystem()).thenReturn(fileSystem);
    MSBuildLanguage lang = mockMSBuildLanguage();
    when(project.getLanguage()).thenReturn(lang);
    when(project.getLanguageKey()).thenReturn(lang.getKey());
    
    return project;
  }

  private static List<InputFile> fromSourceFiles(List<File> sourceFiles){
    List<InputFile> result = new ArrayList<InputFile>();
    for(File file: sourceFiles) {
      InputFile inputFile = mock(InputFile.class);
      when(inputFile.getFile()).thenReturn(new File(file, ""));
      result.add(inputFile);
    }
    return result;
  }

  public static MSBuildLanguage mockMSBuildLanguage(){
    return new MSBuildLanguage(new Settings());
  }
  
  private static List<File> scanForSourceFiles(List<File> sourceDirs) {
    List<File> result = new ArrayList<File>();
    String[] suffixes = mockMSBuildLanguage().getFileSuffixes();
    String[] includes = new String[ suffixes.length ];
    for(int i = 0; i < includes.length; ++i) {
      includes[i] = "**/*." + suffixes[i];
    }
    
    DirectoryScanner scanner = new DirectoryScanner();
    for(File baseDir : sourceDirs) {
      scanner.setBasedir(baseDir);
      scanner.setIncludes(includes);  
      scanner.scan();
      for (String relPath : scanner.getIncludedFiles()) {
        File f = new File(baseDir, relPath);
        result.add(f);
      }  
    }
    
    return result;
  }
}
