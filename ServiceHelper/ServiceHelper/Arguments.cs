using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ServiceHelper
{
    // TODO: rework this class
    public sealed class Arguments
    {
        private static readonly char[] namedArgumentFlags = new char[] { '/', '-' };
        private static readonly char[] nameValueDelimiters = new char[] { ':', '=' };

        public NameValueCollection NamedArguments
        {
            get;
            private set;
        }

        public string[] UnnamedArguments
        {
            get;
            private set;
        }

        public string[] AllArguments
        {
            get;
            private set;
        }

        public Arguments()
            : this(Environment.CommandLine)
        {
        }

        public Arguments(string command, bool removeProgramArgument = true)
            : this(Arguments.GetArgumentsFromCommand(command, removeProgramArgument))
        {
        }

        public Arguments(string[] args, IEqualityComparer<string> equalityComparer = null)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            if (equalityComparer == null)
            {
                equalityComparer = StringComparer.CurrentCultureIgnoreCase;
            }

            NameValueCollection namedArguments = new NameValueCollection((System.Collections.IEqualityComparer)equalityComparer);
            List<string> unnamedArguments = new List<string>();

            foreach (string arg in args)
            {
                if (arg.Length > 1 && namedArgumentFlags.Contains(arg[0]))
                {
                    string name;
                    string value;
                    int nameValueDelimiterIndex = arg.IndexOfAny(nameValueDelimiters, 1);

                    if (nameValueDelimiterIndex > 0)
                    {
                        name = arg.Substring(1, nameValueDelimiterIndex - 2);
                        value = arg.Substring(nameValueDelimiterIndex + 1, arg.Length - nameValueDelimiterIndex - 1);
                    }
                    else
                    {
                        name = arg.Substring(1);
                        value = null;
                    }

                    namedArguments.Add(name, value);
                }
                else
                {
                    unnamedArguments.Add(arg);
                }
            }

            this.NamedArguments = namedArguments;
            this.UnnamedArguments = unnamedArguments.ToArray();
            this.AllArguments = args;
        }

        public static string[] GetArgumentsFromCommand(string command, bool removeProgramArgument = true)
        {
            if (command == null)
            {
                throw new ArgumentException("command");
            }

            // TODO: implement this
            throw new NotImplementedException();
        }
    }
}
