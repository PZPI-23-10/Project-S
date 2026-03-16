using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Project_S.Runtime.Common.Extensions
{
    public static class TaskExtensions
    {
        public static async UniTask<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutCancellationSource = new CancellationTokenSource();

            if (task == await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationSource.Token)))
            {
                timeoutCancellationSource.Cancel();
                return await task;
            }

            throw new TimeoutException();
        }
    }
}