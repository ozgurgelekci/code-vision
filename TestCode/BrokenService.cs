using System;

namespace CodeVision.TestCode
{
    public class BrokenService
    {
        public void Method1()
        {
            Console.WriteLine("Missing semicolon")
            // Missing semicolon above
        }

        public void Method2()
        {
            var x = undefinedVariable + 1; // Undefined variable
            Console.WriteLine(x);
        }

        public void Method3()
        {
            Console.Writeline("Typo in method name"); // WriteLine typo
        }

        public void Method4()
        {
            if (true)
            {
                Console.WriteLine("Unbalanced braces");
            // Missing closing brace
        }

        public void Method5()
        {
            string str = "Unterminated string;
            Console.WriteLine(str);
        }

        // Invalid method signature
        public void Method6(int x, int x) // Duplicate parameter names
        {
            return x + x; // Wrong return type
        }

        public void Method7()
        {
            // Invalid escape sequence
            string path = "C:\invalid\path";
            Console.WriteLine(path);
        }
    }
}
