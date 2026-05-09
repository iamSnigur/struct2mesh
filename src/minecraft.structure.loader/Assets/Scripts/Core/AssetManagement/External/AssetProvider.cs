using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        public static void Release()
        {
            foreach (List<AsyncOperationHandle> loadedHandle in _loadedHandles.Values)
                foreach (AsyncOperationHandle handle in loadedHandle) 
                    Addressables.Release(handle);
            
            _cachedHandles.Clear();
            _loadedHandles.Clear();
        }
        
        public static async UniTask<T> LoadByReferenceAsync<T>(string referenceKey) where T : class
        {
            if (_cachedHandles.TryGetValue(referenceKey, out AsyncOperationHandle cachedHandle))
                return cachedHandle.Result as T;
            
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(referenceKey);

            return await LoadAndCacheHandle(referenceKey, handle);
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