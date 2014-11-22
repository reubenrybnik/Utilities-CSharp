using System;
using System.Linq;
using System.Security;
using System.Text;

namespace ServiceHelper
{
    internal static class Utils
    {
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
