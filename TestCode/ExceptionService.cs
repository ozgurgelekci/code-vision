using System;
using System.IO;

namespace CodeVision.TestCode
{
    public class ExceptionService
    {
        // Swallowing exceptions
        public void BadMethod1()
        {
            try
            {
                throw new Exception("Something went wrong");
            }
            catch
            {
                // Silently ignoring exception - bad practice
            }
        }

        // Throwing generic exceptions
        public void BadMethod2(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new Exception("Input is invalid"); // Should use ArgumentException
        }

        // Not using specific exception types
        public void BadMethod3()
        {
            throw new Exception("File not found"); // Should use FileNotFoundException
        }

        // Rethrowing exception incorrectly
        public void BadMethod4()
        {
            try
            {
                File.ReadAllText("nonexistent.txt");
            }
            catch (Exception ex)
            {
                throw ex; // Loses stack trace, should be just 'throw'
            }
        }

        // Using exceptions for control flow
        public bool IsNumeric(string input)
        {
            try
            {
                int.Parse(input);
                return true;
            }
            catch
            {
                return false; // Should use TryParse instead
            }
        }

        // Not disposing resources properly
        public string ReadFile(string path)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open);
                // ... read file
                return "content";
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading file", ex);
            }
            // FileStream not disposed in finally block
        }

        // Catching and rethrowing without adding value
        public void BadMethod5()
        {
            try
            {
                DoSomething();
            }
            catch (Exception ex)
            {
                throw; // No added value, just remove this try-catch
            }
        }

        private void DoSomething()
        {
            throw new NotImplementedException();
        }
    }
}
