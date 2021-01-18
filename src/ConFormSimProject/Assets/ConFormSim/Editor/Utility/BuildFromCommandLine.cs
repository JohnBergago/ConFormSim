using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildFromCommandLine : Editor
{
    private static Dictionary<string, string> FindAllScenes()
    {
        Dictionary<string, string> scenes  = new Dictionary<string, string>();
        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            string[] scenePath = scene.path.Split('/');
            string sceneName = scenePath[scenePath.Length - 1].Split('.')[0];
            scenes.Add(sceneName, scene.path);
        }
        return scenes;
    }
    static void PerformBuild ()
     {
        Dictionary<string, string>  scenes = FindAllScenes();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.None;
        // buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;

        foreach (KeyValuePair<string, string> scene in scenes)
        {
            buildPlayerOptions.scenes = new string[] {scene.Value};
            buildPlayerOptions.locationPathName = "../../build/" 
                + scene.Key 
                + ".x86_64";
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " 
                    + scene.Key 
                    + " with " 
                    + summary.totalSize 
                    + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Build failed " + scene.Key);
            } 
        }
     }
}
