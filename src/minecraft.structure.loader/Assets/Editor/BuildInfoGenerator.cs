using System.Collections.Generic;
using System.IO;
using Editor.Utils;
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
            string branch = GetBranchName();
            string commit = GetCommitHash();
            string version = GetVersion();
            PlayerSettings.bundleVersion = version;

            Debug.Log($"v{version}-{branch}-{commit}");

            Dictionary<string, string> info = new()
            {
                ["version"] = version,
                ["branch"] = branch,
                ["commit"] = commit,
            };

            string json = JsonConvert.SerializeObject(info);
            File.WriteAllText("Assets/Resources/build_info.json", json);
        }

        private static string GetCommitHash()
        {
            const string hashVar = "GITHUB_SHA";
            if (EnvironmentUtils.TryGetVariable(hashVar, out string variableValue))
                return variableValue[..7];
                
            return GitUtils.RunCommand("rev-parse --short HEAD");
        }
        
        private static string GetBranchName()
        {
            const string branchVar = "GITHUB_REF";
            if (EnvironmentUtils.TryGetVariable(branchVar, out string variableValue))
            {
                string[] components = variableValue.Split('/');
                return components[^1];
            }
            
            return GitUtils.RunCommand("branch --show-current");
        }
        
        private static string GetVersion()
        {
            const string versionVar = "VERSION";
            if (EnvironmentUtils.TryGetVariable(versionVar, out string variableValue))
                return variableValue;
            
            return GetIncrementedBundleVersion();
        }
        
        private static string GetIncrementedBundleVersion()
        {
            string version = PlayerSettings.bundleVersion;

            string[] components = version.Split('.');

            int minor = int.Parse(components[2]);
            minor++;

            components[2] = minor.ToString();

            version = string.Join(".", components);

            return version;
        }
    }
}