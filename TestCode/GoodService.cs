using System;
using System.Threading.Tasks;

namespace CodeVision.TestCode
{
    public class GoodService
    {
        // Magic number - should be a constant
        private readonly int timeoutMs = 30000;
        
        public GoodService()
        {
        }

        // Async void is problematic - should return Task
        public async void ProcessDataAsync(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
                
            await Task.Delay(100);
            Console.WriteLine($"Processing: {data}");
        }

        public int Calculate(int x, int y)
        {
            // Missing input validation
            return x + y;
        }

        public string GetMessage()
        {
            return "Hello World";
        }
    }
}
