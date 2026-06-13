using System;
using System.Runtime.InteropServices;
using fNbt;
using UnityEngine;

namespace Core
{
    public class FileReceiver : MonoBehaviour
    {
        public event Action<string> OnFileReceived;
    
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();

        public void Open() => 
            OpenUploadPanel();

        private NbtStructureParser nbtStructureParser;

        private void Awake()
        {
            nbtStructureParser = FindFirstObjectByType<NbtStructureParser>();
        }

        public void OnFileLoaded(string jsonPayload)
        {
            Payload payload = JsonUtility.FromJson<Payload>(jsonPayload);
            OnFileReceived?.Invoke(payload.FileName);
        
            Debug.Log($"Received file: {payload.FileName}, size: {payload.FileContent.Length}");

            byte[] data = Convert.FromBase64String(payload.FileContent);

            NbtFile nbtFile = new();
            nbtFile.LoadFromBuffer(data, 0, data.Length, NbtCompression.AutoDetect);
        
            nbtStructureParser.Parse(nbtFile);
        }

        private record Payload
        {
            public string FileName;
            public string FileContent;
        }
    }
}