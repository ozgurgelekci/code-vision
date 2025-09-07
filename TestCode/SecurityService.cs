using System;
using System.Data.SqlClient;

namespace CodeVision.TestCode
{
    public class SecurityService
    {
        // Hardcoded credentials - security vulnerability
        private const string ConnectionString = "Server=localhost;Database=MyDB;User Id=admin;Password=123456;";
        
        public string GetUserData(string userId)
        {
            // SQL Injection vulnerability
            var query = $"SELECT * FROM Users WHERE Id = '{userId}'";
            
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(query, connection);
                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }

        public bool ValidateUser(string username, string password)
        {
            // Weak password validation
            if (password.Length > 3)
                return true;
                
            return false;
        }

        public void LogSensitiveData(string creditCard, string ssn)
        {
            // Logging sensitive information
            Console.WriteLine($"Processing CC: {creditCard}, SSN: {ssn}");
        }

        // Method with potential buffer overflow
        public unsafe void ProcessBuffer(byte* buffer, int size)
        {
            for (int i = 0; i <= size; i++) // Off-by-one error
            {
                buffer[i] = 0;
            }
        }
    }
}
