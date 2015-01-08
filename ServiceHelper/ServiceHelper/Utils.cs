using System;
using System.Linq;
using System.Security;
using System.Text;

namespace ServiceHelper
{
    /// <summary>
    /// A class containing internal utility methods that are or could be shared by multiple classes.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Reads a password from the console, hiding the input keystrokes.
        /// </summary>
        /// <param name="userName">The user name to read the password for.</param>
        /// <returns>the password entered by the user.</returns>
        public static string ReadPasswordFromConsole(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException("userName", "userName cannot be null or empty.");
            }

            Console.Write("Enter the password for user account " + userName + ": ");

            ConsoleKeyInfo keyInfo;
            StringBuilder password = new StringBuilder();

            do
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.KeyChar != (char)0)
                {
                    password.Append(keyInfo.KeyChar);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    password.Remove(password.Length - 1, 1);
                }
            }
            while (keyInfo.Key != ConsoleKey.Enter);

            Console.WriteLine();

            return password.ToString();
        }

        /// <summary>
        /// Reads a password from the console into a SecureString, hiding the input keystrokes.
        /// </summary>
        /// <param name="userName">The user name to read the password for.</param>
        /// <returns>the password entered by the user.</returns>
        public static SecureString SecureReadPasswordFromConsole(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException("userName", "userName cannot be null or empty.");
            }

            Console.Write("Enter the password for user account " + userName + ": ");

            ConsoleKeyInfo keyInfo;
            SecureString securePassword = new SecureString();

            do
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.KeyChar != (char)0)
                {
                    securePassword.AppendChar(keyInfo.KeyChar);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    securePassword.RemoveAt(securePassword.Length - 1);
                }
            }
            while (keyInfo.Key != ConsoleKey.Enter);

            Console.WriteLine();

            securePassword.MakeReadOnly();
            return securePassword;
        }

        /// <summary>
        /// Given a string with both a domain and user, splits the string into a domain string and a user string.
        /// Input strings can be in either of two forms:  domain\user or user@domain.
        /// </summary>
        /// <param name="domainUser">The string to be split into a domain and user name.</param>
        /// <param name="user">The user name part of the string.</param>
        /// <param name="domain">The domain part of the string or null if the string does not include a domain.</param>
        public static void SplitDomainUserString(string domainUser, out string user, out string domain)
        {
            if (domainUser.Contains('\\'))
            {
                string[] splitDomainUser = domainUser.Split('\\');
                user = splitDomainUser[1];
                domain = splitDomainUser[0];
            }
            else if (domainUser.Contains('@'))
            {
                string[] splitUserDomain = domainUser.Split('@');
                user = splitUserDomain[0];
                domain = splitUserDomain[1];
            }
            else
            {
                user = domainUser;
                domain = null;
            }
        }
    }
}
