using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace CodeVision.TestCode
{
    public class MemoryLeakService
    {
        private static List<byte[]> staticList = new List<byte[]>();
        private Timer timer;
        private FileStream fileStream;

        // Static collection that grows indefinitely
        public void AddToStaticCollection()
        {
            staticList.Add(new byte[1024 * 1024]); // 1MB each time
        }

        // Event handler not unsubscribed
        public void StartTimer()
        {
            timer = new Timer(1000);
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
            // Timer.Elapsed event not unsubscribed
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Event handler that can cause memory leaks
            var data = new byte[1024];
            // Processing...
        }

        // IDisposable not implemented for unmanaged resources
        public void OpenFile(string path)
        {
            fileStream = new FileStream(path, FileMode.Open);
            // FileStream not disposed
        }

        // Circular references
        public class Parent
        {
            public List<Child> Children { get; set; } = new List<Child>();
        }

        public class Child
        {
            public Parent Parent { get; set; }
        }

        public void CreateCircularReference()
        {
            var parent = new Parent();
            var child = new Child { Parent = parent };
            parent.Children.Add(child);
            // Circular reference created without proper cleanup
        }

        // Large objects in finalizer
        ~MemoryLeakService()
        {
            // Finalizer doing heavy work
            var largeArray = new byte[1024 * 1024 * 10]; // 10MB
            // Heavy cleanup work in finalizer
        }

        // Caching without expiration
        private static Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();

        public byte[] GetCachedData(string key)
        {
            if (!cache.ContainsKey(key))
            {
                cache[key] = new byte[1024 * 1024]; // 1MB cached data
            }
            return cache[key];
            // Cache never expires or clears
        }

        // Delegate/Event memory leak
        public static event Action<string> StaticEvent;

        public void SubscribeToStaticEvent()
        {
            StaticEvent += HandleStaticEvent;
            // Never unsubscribes from static event
        }

        private void HandleStaticEvent(string message)
        {
            Console.WriteLine(message);
        }
    }
}
