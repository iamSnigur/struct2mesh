using System;
using System.Runtime.InteropServices;
using fNbt;
using UnityEngine;

public class FileReceiver : MonoBehaviour
{
    public event Action<string> OnFileReceived;
    
    [DllImport("__Internal")]
    private static extern void OpenUploadPanel();

    public void Open() => 
        OpenUploadPanel();

    public void OnFileLoaded(string jsonPayload)
    {
        Payload payload = JsonUtility.FromJson<Payload>(jsonPayload);
        OnFileReceived?.Invoke(payload.FileName);
        
        Debug.Log($"Received file: {payload.FileName}, size: {payload.FileContent.Length}");

        byte[] data = System.Convert.FromBase64String(payload.FileContent);

        NbtFile nbtFile = new();
        nbtFile.LoadFromBuffer(data, 0, data.Length, NbtCompression.AutoDetect);
        
        _nbtReader.LoadStructure(nbtFile);
        
        //Debug.Log(nbtFile.RootTag);
    }

    private NbtReader _nbtReader;
    
    private void Awake()
    {
        _nbtReader = FindFirstObjectByType<NbtReader>();
    }

    private record Payload
    {
        public string FileName;
        public string FileContent;
    }
}