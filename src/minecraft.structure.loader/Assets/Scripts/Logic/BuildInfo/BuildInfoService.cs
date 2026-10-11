using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Logic.BuildInfo
{
    [Preserve]
    public class BuildInfoService : IBuildInfoService
    {
        public string BuildInfo => $"v{Version}-{BranchName}-{CommitHash}-MCv{MinecraftVersion}";

        private static string Version { get; set; }
        private static string BranchName { get; set; }
        private static string CommitHash { get; set; }
        private static string MinecraftVersion { get; set; }

        public BuildInfoService()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("build_info");
            var info = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
            Resources.UnloadAsset(textAsset);
            
            Version = info["version"];
            BranchName = info["branch"];
            CommitHash = info["commit"];
            MinecraftVersion = "26.1.2";
        }
    }
}