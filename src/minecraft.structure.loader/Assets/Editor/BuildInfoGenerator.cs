using System.Collections.Generic;
using System.IO;
using Core.Utils;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
    public class BuildInfoGenerator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            string branch = GitUtils.RunCommand("branch --show-current");
            string commit = GitUtils.RunCommand("rev-parse --short HEAD");
            string version = GetIncrementedVersion();

            Debug.Log($"v{version}-{branch}-{commit}");

            Dictionary<string, string> info = new()
            {
                ["version"] = version,
                ["branch"] = branch,
                ["commit"] = commit,
            };

            string json = JsonConvert.SerializeObject(info);
            File.WriteAllText("Assets/Resources_moved/build_info.json", json);
        }

        private static string GetIncrementedVersion()
        {
            string version = PlayerSettings.bundleVersion;

            string[] components = version.Split('.');

            int minor = int.Parse(components[2]);
            minor++;

            components[2] = minor.ToString();

            version = string.Join(".", components);

            PlayerSettings.bundleVersion = version;
            
            return version;
        }
    }
}