using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CodeVision.TestCode
{
    /// <summary>
    /// Excellent quality service with proper documentation, error handling, and best practices
    /// </summary>
    public class ExcellentService
    {
        private readonly ILogger<ExcellentService> _logger;
        private const int DefaultTimeout = 30000;
        private const int MaxRetryAttempts = 3;

        public ExcellentService(ILogger<ExcellentService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes data asynchronously with proper error handling and logging
        /// </summary>
        /// <param name="data">The data to process</param>
        /// <returns>Processed result</returns>
        public async Task<string> ProcessDataAsync(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            }

            try
            {
                _logger.LogInformation("Processing data with length: {DataLength}", data.Length);
                
                await Task.Delay(100); // Simulate processing
                
                var result = data.ToUpperInvariant();
                
                _logger.LogInformation("Data processed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data");
                throw;
            }
        }

        /// <summary>
        /// Calculates sum with input validation
        /// </summary>
        public int CalculateSum(int a, int b)
        {
            if (a < 0 || b < 0)
            {
                throw new ArgumentException("Values must be non-negative");
            }

            return a + b;
        }
    }
}
