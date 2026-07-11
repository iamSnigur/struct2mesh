using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Logic
{
    public class FileReceiver : MonoBehaviour
    {
        public event Action<string> OnFileReceived;

        private StructureBuilder _structureBuilder;
        
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();

        public void Open() => 
            OpenUploadPanel();

        public void InjectDependencies(StructureBuilder structureBuilder) =>
            _structureBuilder = structureBuilder;

        public void OnFileLoaded(string jsonPayload)
        {
            Payload payload = JsonUtility.FromJson<Payload>(jsonPayload);
            OnFileReceived?.Invoke(payload.FileName);
            byte[] rawContent = Convert.FromBase64String(payload.FileContent);
            _structureBuilder
                .BuildFromBytesAsync(rawContent, "nbt")
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