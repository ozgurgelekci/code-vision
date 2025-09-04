using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace TestProject
{
    /// <summary>
    /// 🔴 VERY BAD CODE - Security vulnerabilities and bad practices
    /// This class contains multiple security issues and code smells for testing CodeVision
    /// </summary>
    public class BadSecurityCode
    {
        // 🚨 SECURITY ISSUE: Hardcoded secrets - NEVER DO THIS!
        private static string DATABASE_PASSWORD = "admin123";
        private static string API_KEY = "sk-1234567890abcdef-secret-key";
        private static string JWT_SECRET = "my-super-secret-key-for-jwt";
        private static string ADMIN_PASSWORD = "defaultAdminPass123";
        
        /// <summary>
        /// 🚨 SECURITY VULNERABILITY: SQL Injection
        /// </summary>
        public string GetUserData(string userId)
        {
            // 🔴 SQL Injection vulnerability - user input directly in query!
            string query = $"SELECT * FROM Users WHERE Id = {userId}";
            
            // 🔴 Hardcoded password in connection string
            string connectionString = $"Server=localhost;Database=MyDB;User=admin;Password={DATABASE_PASSWORD};";
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(query, connection);
                
                // 🔴 No error handling, no input validation!
                var result = command.ExecuteScalar();
                return result.ToString(); // 🔴 Potential null reference exception!
            }
        }
        
        /// <summary>
        /// 🚨 SECURITY ISSUE: Hardcoded credentials and weak authentication
        /// </summary>
        public bool AuthenticateUser(string username, string password)
        {
            // 🔴 Hardcoded admin credentials!
            if (username == "admin" && password == ADMIN_PASSWORD)
            {
                return true;
            }
            
            // 🔴 Password comparison without hashing!
            if (password == "guest123" || password == "user123")
            {
                return true;
            }
            
            // 🔴 Default weak password
            return password == "defaultpass";
        }
        
        /// <summary>
        /// 🚨 SECURITY VULNERABILITY: Code injection and path traversal
        /// </summary>
        public void ProcessUserInput(string userInput)
        {
            // 🔴 Code injection risk - executing user input!
            var dangerousScript = $"<script>eval('{userInput}')</script>";
            
            // 🔴 Path traversal vulnerability - no input sanitization
            string filePath = $"/tmp/{userInput}.txt";
            File.WriteAllText(filePath, dangerousScript);
            
            // 🔴 Information disclosure
            Console.WriteLine($"Secret API Key: {API_KEY}");
        }
        
        /// <summary>
        /// 🚨 SECURITY ISSUE: Information disclosure
        /// </summary>
        public string GetSystemInfo()
        {
            // 🔴 Exposing sensitive information
            return $"Database Password: {DATABASE_PASSWORD}, API Key: {API_KEY}, JWT Secret: {JWT_SECRET}";
        }
        
        /// <summary>
        /// 🔴 CODE SMELL: Method doing too many things, deeply nested, poor error handling
        /// </summary>
        public void DoEverythingBadly(string user, string pass, string data, int type, bool flag, string extra)
        {
            if (user != null) // 🔴 Deeply nested conditions
            {
                if (pass != null)
                {
                    if (data != null)
                    {
                        if (type > 0)
                        {
                            if (flag == true) // 🔴 Redundant comparison
                            {
                                if (extra.Length > 0) // 🔴 Potential null reference
                                {
                                    // 🔴 More SQL injection + connection leak
                                    var connection = new SqlConnection($"Server=localhost;User=sa;Password=123456;");
                                    var query = $"UPDATE Users SET Data = '{data}' WHERE Name = '{user}' AND Extra = '{extra}'";
                                    
                                    // 🔴 No using statement - connection leak!
                                    connection.Open();
                                    var cmd = new SqlCommand(query, connection);
                                    cmd.ExecuteNonQuery(); // 🔴 No error handling!
                                    
                                    // 🔴 Connection never closed - memory leak!
                                    
                                    // 🔴 More bad practices
                                    Thread.Sleep(5000); // Blocking operation
                                    Console.WriteLine($"Processed {user} with secret: {JWT_SECRET}");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 🔴 CODE SMELL: Magic numbers, no constants, poor naming
        /// </summary>
        public decimal CalculatePriceWithMagicNumbers(int qty)
        {
            // 🔴 Magic numbers everywhere - what do these mean?
            if (qty > 100) return qty * 0.85m; 
            if (qty > 50) return qty * 0.90m;  
            if (qty > 25) return qty * 0.95m;
            if (qty > 10) return qty * 0.98m;
            return qty * 1.0m;
        }
        
        /// <summary>
        /// 🔴 CODE SMELL: Poor exception handling
        /// </summary>
        public void BadExceptionHandling()
        {
            try
            {
                // 🔴 Risky operations without proper handling
                var result = 10 / 0;
                File.ReadAllText("nonexistent-file.txt");
                throw new Exception("Something went wrong");
            }
            catch (Exception ex)
            {
                // 🔴 Swallowing exceptions
                // 🔴 No logging
                // 🔴 Returning default value without indication of failure
            }
        }
        
        /// <summary>
        /// 🔴 PERFORMANCE ISSUE: Inefficient operations
        /// </summary>
        public string ProcessLargeDataInefficiently(string[] data)
        {
            string result = "";
            
            // 🔴 String concatenation in loop - performance killer!
            for (int i = 0; i < data.Length; i++)
            {
                result += data[i] + " ";
                
                // 🔴 Unnecessary database call in loop
                var query = $"SELECT COUNT(*) FROM Items WHERE Name = '{data[i]}'";
                // Database call for each item...
            }
            
            return result;
        }
    }
}
