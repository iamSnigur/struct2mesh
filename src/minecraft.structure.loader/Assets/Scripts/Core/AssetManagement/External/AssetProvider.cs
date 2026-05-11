using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Core.AssetManagement.External
{
    public static class AssetProvider
    {
        private static readonly Dictionary<string, AsyncOperationHandle> _cachedHandles = new();
        private static readonly Dictionary<string, List<AsyncOperationHandle>> _loadedHandles = new();

        public static void Init()
        {
            Addressables.InitializeAsync();
        }

        public static async UniTask<bool> KeyExistsAsync<T>(string key)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(key, typeof(T));
            await handle.Task;

            bool exists = handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Count > 0;
            Addressables.Release(handle);
            return exists;
        }
        
        public static void Release()
        {
            foreach (List<AsyncOperationHandle> loadedHandle in _loadedHandles.Values)
                foreach (AsyncOperationHandle handle in loadedHandle) 
                    Addressables.Release(handle);
            
            _cachedHandles.Clear();
            _loadedHandles.Clear();
            Debug.Log("Asset handles released");
        }
        
        public static async UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string key) where T : class
        {
            /*if (_cachedHandles.TryGetValue(key, out AsyncOperationHandle cachedHandle))
                return cachedHandle.Result as T;*/
            
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            AssetHandle<T> assetHandle = new()
            {
                Handle = handle,
                Asset = await handle,
            };
            
            return assetHandle;
        }
        
        public static async UniTask<T> LoadByAddressAsync<T>(string key) where T : class
        {
            if (_cachedHandles.TryGetValue(key, out AsyncOperationHandle cachedHandle))
                return cachedHandle.Result as T;
            
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            return await LoadAndCacheHandle(key, handle);
        }

        private static async UniTask<T> LoadAndCacheHandle<T>(string reference, AsyncOperationHandle<T> handle) where T : class
        {
            handle.Completed += h =>
            {
                _cachedHandles[reference] = h;
            };

            if (!_loadedHandles.TryGetValue(reference, out List<AsyncOperationHandle> loaded))
            {
                loaded = new List<AsyncOperationHandle>();
                _loadedHandles[reference] = loaded;
            }
            
            loaded.Add(handle);
            
            return await handle.ToUniTask();
        }
    }
}