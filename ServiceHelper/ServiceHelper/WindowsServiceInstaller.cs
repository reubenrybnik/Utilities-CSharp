using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace ServiceHelper
{
    [RunInstaller(true)]
    internal class WindowsServiceInstaller : ServiceProcessInstaller
    {
        private readonly ServiceInstaller serviceInstaller;

        public WindowsServiceInstaller()
        {
            this.serviceInstaller = new ServiceInstaller();
            this.Installers.Add(this.serviceInstaller);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.serviceInstaller.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            bool interactive = this.Context.Parameters.ContainsKey("Interactive");

            if (interactive)
            {
                Console.WriteLine("Leave value blank to accept the default value for any option.");
            }

            // service name
            {
                const string optionName = "ServiceName";
                Regex validationRegex = new Regex(@"[^/\\]{1,256}");
                string validationMessage = "Must be between 1 and 256 characters in length and cannot contain / or \\";

                if (!this.Context.Parameters.ContainsKey(optionName))
                {
                    throw new InvalidInstallOptionException(optionName + ": No value was provided.");
                }

                string value = this.Context.Parameters[optionName];

                if(!validationRegex.IsMatch(value))
                {
                    throw new InvalidInstallOptionException(optionName + ": " + validationMessage);
                }

                this.serviceInstaller.ServiceName = value;
            }

            // display name
            {
                const string optionName = "DisplayName";
                const string friendlyOptionName = "Display Name";
                Regex validationRegex = new Regex(@".{1,256}");
                string validationMessage = "Must be between 1 and 256 characters in length.";

                string defaultValue = Properties.Settings.Default.DefaultServiceDisplayName;
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    defaultValue = this.serviceInstaller.ServiceName;
                }

                string value = this.GetOptionValue(optionName, friendlyOptionName, defaultValue, interactive, validationRegex, validationMessage);
                this.serviceInstaller.DisplayName = value;
            }

            // description
            {
                const string optionName = "Description";
                const string friendlyOptionName = optionName;
                string defaultValue = Properties.Settings.Default.DefaultServiceDescription;

                string value = this.GetOptionValue(optionName, friendlyOptionName, defaultValue, interactive); ;
                this.serviceInstaller.Description = value;
            }

            // start type
            {
                const string optionName = "StartType";
                const string friendlyOptionName = "Start Type";
                string defaultValue = Properties.Settings.Default.DefaultServiceStartType.ToString();

                Regex validationRegex;
                string validationMessage;
                this.GetValidationForEnum(typeof(ServiceStartMode), out validationRegex, out validationMessage);

                string value = this.GetOptionValue(optionName, friendlyOptionName, defaultValue, interactive);
                this.serviceInstaller.StartType = (ServiceStartMode)Enum.Parse(typeof(ServiceStartMode), value, true);
            }

            // account type
            {
                const string friendlyOptionName = "Account Type";
                string validValues = string.Join("|", Enum.GetNames(typeof(ServiceAccount)));

                Regex validationRegex;
                string validationMessage;
                this.GetValidationForEnum(typeof(ServiceAccount), out validationRegex, out validationMessage);

                if (this.Context.Parameters.ContainsKey("UserName"))
                {
                    try
                    {
                        this.Account = (ServiceAccount)Enum.Parse(typeof(ServiceAccount), this.Context.Parameters["UserName"], true);
                    }
                    catch (ArgumentException)
                    {
                        this.Account = ServiceAccount.User;
                    }
                }
                else
                {
                    this.Account = Properties.Settings.Default.DefaultServiceAccount;
                }

                if (interactive)
                {
                    string defaultValue = this.Account.ToString();
                    string selectedValue = this.GetValidInteractiveInput(friendlyOptionName, defaultValue, validationRegex, validationMessage);
                    this.Account = (ServiceAccount)Enum.Parse(typeof(ServiceAccount), selectedValue, true);
                }
            }

            if (this.Account == ServiceAccount.User)
            {
                bool logonSucceeded;
                bool userNameInteractive = (interactive || !this.Context.Parameters.ContainsKey("UserName"));

                do
                {
                    // user name
                    {
                        // TODO: look up the requirements for a user account name
                        const string optionName = "UserName";
                        const string friendlyOptionName = "User Name";
                        string defaultValue = this.Username;
                        Regex validationRegex = new Regex(@"([\w-]+\\)?[\w-]+");
                        string validationMessage = "The user account specified is not valid.";

                        string value = this.GetOptionValue(optionName, friendlyOptionName, defaultValue, userNameInteractive, validationRegex, validationMessage);
                        this.Username = value;
                    }

                    // password
                    {
                        if (this.Context.Parameters.ContainsKey("Password"))
                        {
                            this.Password = this.Context.Parameters["Password"];
                        }

                        if (userNameInteractive)
                        {
                            // TODO: consider possibly using a SecureString somehow
                            // unfortunately, ServiceProcessInstaller does not accept a SecureString,
                            // so this may not be feasible without re-implementing ServiceProcessInstaller
                            this.Password = Utils.ReadPasswordFromConsole(this.Username);
                        }

                        string domain;
                        string user;
                        string[] domainUser = this.Username.Split('\\');

                        if (domainUser.Length == 1)
                        {
                            domain = null;
                            user = domainUser[0];
                        }
                        else
                        {
                            domain = domainUser[0];
                            user = domainUser[1];
                        }

                        NativeMethods.SafeTokenHandle userToken;
                        logonSucceeded = NativeMethods.LogonUser(user, domain, this.Password, NativeMethods.LogonType.LOGON32_LOGON_INTERACTIVE, NativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT, out userToken);
                        int lastError = Marshal.GetHRForLastWin32Error();
                        userToken.Dispose();

                        if (!logonSucceeded)
                        {
                            if (userNameInteractive)
                            {
                                Console.WriteLine("The provided username and password are incorrect. Please re-enter them.");
                            }
                            else
                            {
                                throw new InvalidInstallOptionException("User Name/Password: Login failed for the specified credentials.", lastError);
                            }
                        }
                    }
                }
                while (!logonSucceeded);
            }

            base.OnBeforeInstall(savedState);
        }

        private void GetValidationForEnum(Type enumType, out Regex validationRegex, out string validationMessage)
        {
            string validValues = string.Join("|", Enum.GetNames(enumType));
            validationRegex = new Regex("(" + validValues + ")", RegexOptions.IgnoreCase);
            validationMessage = "Valid values are: " + validValues;
        }

        private string GetOptionValue(string optionName, string friendlyOptionName, string defaultValue, bool interactive)
        {
            return this.GetOptionValue(optionName, friendlyOptionName, defaultValue, interactive, null, null);
        }

        private string GetOptionValue(string optionName, string friendlyOptionName, string defaultValue, bool interactive, Regex validationRegex, string validationMessage)
        {
            string value;

            if (this.Context.Parameters.ContainsKey(optionName))
            {
                value = this.Context.Parameters[optionName];
            }
            else
            {
                value = defaultValue;
            }

            if (interactive)
            {
                value = this.GetValidInteractiveInput(friendlyOptionName, value, validationRegex, validationMessage);
            }
            else if (validationRegex != null && !validationRegex.IsMatch(value))
            {
                throw new InvalidInstallOptionException(optionName + ": " + validationMessage);
            }

            return value;
        }

        private string GetValidInteractiveInput(string option, string defaultValue, Regex validationRegex, string validationMessage)
        {
            string selectedValue;
            bool firstInput = true;
            bool hasDefaultValue = !string.IsNullOrEmpty(defaultValue);

            Console.WriteLine();
            Console.WriteLine("Option: " + option);
            
            if (hasDefaultValue)
            {
                Console.WriteLine("Default value: " + defaultValue);
            }

            if (validationMessage != null)
            {
                Console.WriteLine(validationMessage);
            }

            do
            {
                if (firstInput)
                {
                    firstInput = false;
                }
                else if (validationMessage != null)
                {
                    Console.WriteLine(validationMessage);
                }

                Console.Write("Enter value: ");

                selectedValue = Console.ReadLine();
                if (string.IsNullOrEmpty(selectedValue))
                {
                    selectedValue = defaultValue;
                }
            }
            while (validationRegex == null || !validationRegex.IsMatch(selectedValue));

            return selectedValue;
        }
    }
}
