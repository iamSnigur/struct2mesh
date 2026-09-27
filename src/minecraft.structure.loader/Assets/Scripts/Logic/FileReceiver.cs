using System;
using Newtonsoft.Json;
using UnityEngine;
using VContainer;

namespace Logic
{
    public class FileReceiver : MonoBehaviour
    {
        public event Action<string> OnFileReceived;

        private StructureBuilder _structureBuilder;
        
        [Inject]
        public void InjectDependencies(StructureBuilder structureBuilder) =>
            _structureBuilder = structureBuilder;

        [UnityEngine.Scripting.Preserve]
        public void OnFileLoaded(string jsonPayload)
        { 
            // convert type to enum
            Payload payload = JsonConvert.DeserializeObject<Payload>(jsonPayload);
            OnFileReceived?.Invoke(payload.FileName);
            byte[] rawContent = Convert.FromBase64String(payload.FileContent);
            _structureBuilder
                .BuildFromBytesAsync(rawContent, payload.FileType)
                .Forget();
        }

        private record Payload
        {
            public string FileName;
            public string FileContent;
            public string FileType;
        }
    }
}