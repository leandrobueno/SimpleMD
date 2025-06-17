using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace SimpleMD.Helpers
{
    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Func<Task> function)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    await function();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue operation"));
            }
            
            return tcs.Task;
        }
        
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue operation"));
            }
            
            return tcs.Task;
        }
    }
}
