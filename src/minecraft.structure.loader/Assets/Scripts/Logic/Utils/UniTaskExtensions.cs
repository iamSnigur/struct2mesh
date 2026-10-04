using Cysharp.Threading.Tasks;

namespace Logic.Utils
{
    public static class UniTaskExtensions
    {
        public static async UniTask RunWithMinDuration(this UniTask task, int ms) => 
            await UniTask.WhenAll(UniTask.Delay(ms), task);
        
        public static async UniTask<T> RunWithMinDuration<T>(this UniTask<T> task, int ms)
        {
            await UniTask.WhenAll(UniTask.Delay(ms), task);
            return await task;
        }
    }
}