using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.AssetManagement.External
{
    public struct AssetHandle<T> 
        where T : class
    {
        public T Asset;
        public AsyncOperationHandle<T> Handle;
        
        public void Release()
        {
            Addressables.Release(Handle);
        }
    }
}