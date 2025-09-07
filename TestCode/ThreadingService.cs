using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeVision.TestCode
{
    public class ThreadingService
    {
        private static int counter = 0;
        private readonly object lockObject = new object();

        // Race condition - accessing shared state without synchronization
        public void IncrementCounter()
        {
            counter++; // Not thread-safe
        }

        // Deadlock potential
        public void Method1()
        {
            lock (lockObject)
            {
                Thread.Sleep(100);
                Method2(); // Can cause deadlock if Method2 also locks
            }
        }

        public void Method2()
        {
            lock (lockObject)
            {
                Console.WriteLine("Method2");
            }
        }

        // Async void in non-event handler
        public async void ProcessDataAsync()
        {
            await Task.Delay(1000);
            throw new Exception("Unhandled exception in async void");
        }

        // Blocking async call
        public string GetDataSync()
        {
            return GetDataAsync().Result; // Can cause deadlock
        }

        public async Task<string> GetDataAsync()
        {
            await Task.Delay(1000);
            return "Data";
        }

        // Thread.Sleep in async method
        public async Task BadAsyncMethod()
        {
            Thread.Sleep(1000); // Should use await Task.Delay
            await Task.Delay(100);
        }

        // Not disposing CancellationTokenSource
        public void StartOperation()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => DoWork(cts.Token));
            // CancellationTokenSource not disposed
        }

        private void DoWork(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Work without checking cancellation frequently enough
                Thread.Sleep(5000);
            }
        }
    }
}
