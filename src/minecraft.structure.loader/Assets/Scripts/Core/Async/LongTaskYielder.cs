using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Async
{
    public sealed class LongTaskYielder
    {
        private readonly float _yieldInterval;
        private float _elapsed;

        public LongTaskYielder(float yieldInterval = 1f)
        {
            _yieldInterval = yieldInterval;
            _elapsed = 0f;
        }
            
        public async UniTask TryYieldAsync()
        {
                
            if (_elapsed > _yieldInterval)
            {
                await UniTask.Yield();
                _elapsed = 0;
            }
            
            _elapsed += Time.deltaTime;
        }
    }
}