using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NamnyeoChilse.Editor
{
    public static class AndroidValidationBuild
    {
        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/TalkBack-MVP.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            Debug.Log($"Android validation build: {report.summary.result}, errors={report.summary.totalErrors}, output={output}");
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Android build failed.");
        }
    }
}
