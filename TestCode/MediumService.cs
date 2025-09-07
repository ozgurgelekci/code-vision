using System;

namespace CodeVision.TestCode
{
    public class MediumService
    {
        public void ProcessData(string data)
        {
            // Missing null check
            Console.WriteLine("Processing: " + data.Length);
        }

        public int Divide(int a, int b)
        {
            // No division by zero check
            return a / b;
        }

        public string GetUserName(int userId)
        {
            if (userId == 1)
                return "Admin"
            // Missing semicolon above
            
            return "User";
        }

        // Unused variable
        public void DoWork()
        {
            var unused = "This variable is not used";
            Console.WriteLine("Working...");
        }
    }
}
