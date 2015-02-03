using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace ServiceHelper
{
    /// <summary>
    /// Parses named arguments and provides a mechanism for adding arguments. Argument names are not case-sensitive.
    /// </summary>
    public sealed class Arguments
    {
        private static readonly char[] namedArgumentFlags = new char[] { '/', '-' };
        private static readonly char[] nameValueDelimiters = new char[] { ':', '=' };

        private readonly List<string> allArguments;
        private Dictionary<string, string> namedArguments;

        public int Count
        {
            get
            {
                return this.allArguments.Count;
            }
        }

        /// <summary>
        /// The set of all arguments wrapped by this object in their original form.
        /// </summary>
        public string[] AllArguments
        {
            get
            {
                return this.allArguments.ToArray();
            }
        }

        /// <summary>
        /// Retrieves the argument at the specified position.
        /// </summary>
        /// <param name="index">The index of the argument to retrieve.</param>
        /// <returns>the argument at the specified position in its original form</returns>
        public string this[int index]
        {
            get { return this.allArguments[index]; }
        }

        /// <summary>
        /// Retrieves the value for the specified named argument or <c>null</c> if the named argument was
        /// not included in the command line.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <returns>the argument's value or <c>null</c> if the argument was not specified.</returns>
        public string this[string name]
        {
            get
            {
                string value;

                if (!this.namedArguments.TryGetValue(name, out value))
                {
                    value = null;
                }

                return value;
            }
        }

        /// <summary>
        /// Creates an empty argument collection.
        /// </summary>
        public Arguments()
            : this(new string[0])
        {
        }

        /// <summary>
        /// Creates a new argument collection from the arguments in the specified command. Arguments are parsed
        /// using Windows built-in function CommandLineToArgvW.
        /// </summary>
        /// <param name="command">The command to parse to create the argument collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command" /> is null.</exception>
        public Arguments(string command)
            : this(Arguments.GetArgumentsFromCommand(command))
        {
        }

        /// <summary>
        /// Creates a new argument collection from the arguments in the specified array of parsed arguments.
        /// </summary>
        /// <param name="args">The arguments to fill this collection with.</param>
        /// <exception cref="ArgumentNullException"><paramref name="args" /> is null.</exception>
        public Arguments(string[] args)
        {
            this.namedArguments = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            this.PopulateNamedArguments(args);
            this.allArguments = new List<string>(args);
        }

        /// <summary>
        /// Adds named arguments from an argument set to the named arguments dictionary. When multiple instances
        /// of an argument are specified, the last instance wins.
        /// </summary>
        /// <param name="args">The argument set to retrieve named arguments from.</param>
        private void PopulateNamedArguments(string[] args)
        {
            Dictionary<string, string> namedArguments = new Dictionary<string, string>(this.namedArguments, this.namedArguments.Comparer);

            foreach (string arg in args)
            {
                if (arg != null && arg.Length > 1 && namedArgumentFlags.Contains(arg[0]))
                {
                    string name;
                    string value;
                    int nameValueDelimiterIndex = arg.IndexOfAny(nameValueDelimiters, 1);

                    if (nameValueDelimiterIndex > 0)
                    {
                        name = arg.Substring(1, nameValueDelimiterIndex - 1);
                        value = arg.Substring(nameValueDelimiterIndex + 1, arg.Length - nameValueDelimiterIndex - 1);
                    }
                    else
                    {
                        name = arg.Substring(1);
                        value = string.Empty;
                    }

                    // the last instance of a named argument wins
                    namedArguments[name] = value;
                }
            }

            this.namedArguments = namedArguments;
        }

        /// <summary>
        /// Adds the specified named argument to this collection. In the <see cref="Arguments.AllArguments"/>
        /// collection, the argument will use the default flag and delimiter characters and will be surrounded
        /// by quotes, and any quotes in <paramref name="name" /> or <paramref name="value" /> will be escaped.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="value">The value of the named argument. If value is <c>null</c>, it will be replaced
        /// by <see cref="string.Empty" />.</param>
        public void Add(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name", "name cannot be null or empty");
            }
            if (value == null)
            {
                value = string.Empty;
            }

            string argument = "\"" + Arguments.namedArgumentFlags[0] + name.Replace("\"", "\\\"") + Arguments.nameValueDelimiters[0] + value.Replace("\"", "\\\"") + "\"";
            this.allArguments.Add(argument);

            this.namedArguments[name] = value;
        }

        /// <summary>
        /// Adds the arguments in a command to the set of arguments wrapped by this object.
        /// </summary>
        /// <param name="argument">The argument to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command" /> is null.</exception>
        public void Add(string command)
        {
            this.AddRange(Arguments.GetArgumentsFromCommand(command));
        }

        /// <summary>
        /// Adds a set of arguments to the set of arguments wrapped by this object.
        /// </summary>
        /// <param name="args">The set of arguments to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="args" /> is null.</exception>
        public void AddRange(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            this.PopulateNamedArguments(args);
            this.allArguments.AddRange(args);
        }

        /// <summary>
        /// Checks whether the specified named argument is in this collection.
        /// </summary>
        /// <param name="name">The name of the argument to check existence of.</param>
        /// <returns><c>true</c> if the argument is in the collection, <c>false</c> otherwise.</returns>
        public bool Exists(string name)
        {
            return this.namedArguments.ContainsKey(name);
        }

        /// <summary>
        /// Uses Windows built-in function CommandLineToArgvW to split a command line into arguments.
        /// </summary>
        /// <param name="command">The command to split.</param>
        /// <returns>the arguments for the command.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="command" /> is null.</exception>
        public static string[] GetArgumentsFromCommand(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            int argc;
            using (NativeMethods.SafeLocalAllocWStrArray safeArray = NativeMethods.CommandLineToArgvW(command, out argc))
            {
                if (safeArray.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "The call to CommandLineToArgvW failed.");
                }

                string[] argv = new string[argc];
                safeArray.CopyTo(argv);
                return argv;
            }
        }
    }
}
