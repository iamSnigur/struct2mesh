using System;
using System.Collections.Generic;
using Core.AssetManagement.External;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Core
{
    public static class BuildInfo
    {
        public static event Action<string> OnBuildInfoLoaded;
        
        public static string Version { get; private set; }
        public static string BranchName { get; private set; }
        public static string CommitHash { get; private set; }

        public static async UniTaskVoid Init()
        {
            TextAsset textAsset = await AssetProvider.LoadByAddressAsync<TextAsset>("build_info");
            Dictionary<string, string> info = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);

            Version = info["version"];
            BranchName = info["branch"];
            CommitHash = info["commit"];
            
            OnBuildInfoLoaded?.Invoke($"v{Version}-{BranchName}-{CommitHash}");
        }
    }
}