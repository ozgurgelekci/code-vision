using System;

namespace CodeVision.TestCode
{
    // God class with too many responsibilities
    public class SmellsService
    {
        // Long parameter list
        public void ProcessUser(string firstName, string lastName, string email, string phone, 
            string address, string city, string state, string zip, int age, bool isActive, 
            DateTime createdDate, string notes, string department, string title)
        {
            // Long method with multiple responsibilities
            if (firstName == null || lastName == null || email == null)
                throw new Exception("Invalid input");
                
            var fullName = firstName + " " + lastName;
            Console.WriteLine("Processing: " + fullName);
            
            // Duplicate code block 1
            if (age < 18)
            {
                Console.WriteLine("Minor user");
                Console.WriteLine("Special handling required");
                Console.WriteLine("Parental consent needed");
            }
            
            // More processing...
            var emailValid = email.Contains("@");
            if (!emailValid)
                throw new Exception("Invalid email");
                
            // Duplicate code block 2 (similar to block 1)
            if (age < 21)
            {
                Console.WriteLine("Young user");
                Console.WriteLine("Special handling required");
                Console.WriteLine("Age verification needed");
            }
            
            // Magic numbers everywhere
            if (fullName.Length > 50)
                fullName = fullName.Substring(0, 50);
                
            // Dead code
            var unused1 = "This is never used";
            var unused2 = 42;
        }

        // Feature envy - this method knows too much about other classes
        public void CalculateUserScore(User user)
        {
            var score = 0;
            score += user.Profile.Education.Degree == "PhD" ? 100 : 0;
            score += user.Profile.Experience.Years * 5;
            score += user.Account.Premium ? 50 : 0;
            score += user.Social.Followers > 1000 ? 25 : 0;
        }
    }

    // Dummy classes for the example
    public class User
    {
        public Profile Profile { get; set; }
        public Account Account { get; set; }
        public Social Social { get; set; }
    }

    public class Profile
    {
        public Education Education { get; set; }
        public Experience Experience { get; set; }
    }

    public class Education
    {
        public string Degree { get; set; }
    }

    public class Experience
    {
        public int Years { get; set; }
    }

    public class Account
    {
        public bool Premium { get; set; }
    }

    public class Social
    {
        public int Followers { get; set; }
    }
}
