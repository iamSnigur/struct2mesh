using Cysharp.Threading.Tasks;

namespace Core.Async
{
    public sealed class LongTaskYielder
    {
        private readonly int _operationsPerFrame;
        private int _operationsElapsed;

        public LongTaskYielder(int operationsPerFrame = 2500)
        {
            _operationsPerFrame = operationsPerFrame;
            _operationsElapsed = 0;
        }
            
        public async UniTask TryYieldAsync()
        {
            if (_operationsElapsed > _operationsPerFrame)
            {
                await UniTask.Yield();
                _operationsElapsed = 0;
            }
            
            _operationsElapsed++;
        }
    }
}