using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeVision.TestCode
{
    public class PerformanceService
    {
        public List<string> ProcessLargeDataSet(List<int> numbers)
        {
            var results = new List<string>();
            
            // Inefficient nested loops - O(n²) complexity
            for (int i = 0; i < numbers.Count; i++)
            {
                for (int j = 0; j < numbers.Count; j++)
                {
                    if (numbers[i] > numbers[j])
                    {
                        results.Add($"{numbers[i]} > {numbers[j]}");
                    }
                }
            }
            
            return results;
        }

        public string ConcatenateStrings(List<string> strings)
        {
            string result = "";
            
            // Inefficient string concatenation
            foreach (var str in strings)
            {
                result += str + " ";
            }
            
            return result;
        }

        public bool ContainsValue(List<int> list, int value)
        {
            // Using LINQ where a simple Contains would suffice
            return list.Where(x => x == value).Any();
        }

        public void ProcessMemoryIntensive()
        {
            // Memory leak - creating large objects without disposal
            var largeList = new List<byte[]>();
            
            for (int i = 0; i < 10000; i++)
            {
                largeList.Add(new byte[1024 * 1024]); // 1MB each
            }
            
            // Not disposing or clearing the list
        }
    }
}
