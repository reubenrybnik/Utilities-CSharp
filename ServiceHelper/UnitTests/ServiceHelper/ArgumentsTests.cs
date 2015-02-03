using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHelperUnitTests
{
    [TestClass]
    public sealed class ArgumentsTests
    {
        #region Functionality Tests

        #region Ctor

        [TestMethod]
        public void Ctor_Empty_NoArguments()
        {
            Arguments arguments = new Arguments();

            // main test condition
            string hasArgumentsFailMessage = string.Format("A count of 0 arguments was expected, but the actual count was {0}.", arguments.Count);
            Assert.AreEqual(0, arguments.Count);
        }

        [TestMethod]
        public void Ctor_Args_Preserved()
        {
            string[] args = new string[] { "Unnamed", "/Named:Value" };

            Arguments arguments = new Arguments(args);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(args, arguments);
        }

        [TestMethod]
        public void Ctor_Command_ParsedAndPreserved()
        {
            string[] args = new string[] { "Unnamed", "/Named:Value" };
            string command = ArgumentsTests.CommandFromArgs(args);

            Arguments arguments = new Arguments(command);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(args, arguments);
        }

        #endregion

        #region Add and AddRange

        [TestMethod]
        public void Add_NameValue_Added()
        {
            string[] initialArgs = new string[] { "Unnamed1", "/Named1:Value1" };
            const string nameToAdd = "Named2";
            const string valueToAdd = "Value2";
            string[] addedArgs = new string[] { ArgumentsTests.ArgFromNameValue(nameToAdd, valueToAdd, true) };
            string[] allArgs = initialArgs.Concat(addedArgs).ToArray();

            Arguments arguments = new Arguments(initialArgs);
            arguments.Add(nameToAdd, valueToAdd);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(allArgs, arguments);
        }

        [TestMethod]
        public void AddRange_Args_Preserved()
        {
            string[] initialArgs = new string[] { "Unnamed1", "/Named1:Value1" };
            string[] addedArgs = new string[] { "/Named2:Value2", "Unnamed2" };
            string[] allArgs = initialArgs.Concat(addedArgs).ToArray();

            Arguments arguments = new Arguments(initialArgs);
            arguments.AddRange(addedArgs);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(allArgs, arguments);
        }

        [TestMethod]
        public void Add_Command_ParsedAndPreserved()
        {
            string[] initialArgs = new string[] { "Unnamed1", "/Named1:Value1" };
            string[] addedArgs = new string[] { "/Named2:Value2", "Unnamed2" };
            string[] allArgs = initialArgs.Concat(addedArgs).ToArray();
            string addCommand = ArgumentsTests.CommandFromArgs(addedArgs);

            Arguments arguments = new Arguments(initialArgs);
            arguments.Add(addCommand);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(allArgs, arguments);
        }

        #endregion

        #region Exists

        [TestMethod]
        public void Exists_NamedArgumentAdded_ReturnsTrue()
        {
            const string name = "Named";
            const string value = "Value";

            Arguments arguments = new Arguments();
            arguments.Add(name, value);

            // main test condition
            string notExistsFailMessage = string.Format("True was expected when checking for the existence of argument {0}, but false was returned.", name);
            Assert.IsTrue(arguments.Exists(name), notExistsFailMessage);
        }

        [TestMethod]
        public void Exists_NamedArgumentNotAdded_ReturnsFalse()
        {
            const string name = "Named";

            Arguments arguments = new Arguments();

            // main test condition
            string existsFailMessage = string.Format("False was expected when checking for the existence of argument {0}, but true was returned.", name);
            Assert.IsFalse(arguments.Exists(name), existsFailMessage);
        }

        #endregion

        #region IndexByName

        [TestMethod]
        public void IndexByName_NamedArgumentAdded_ReturnsValue()
        {
            const string name = "Named";
            const string value = "Value";

            Arguments arguments = new Arguments();
            arguments.Add(name, value);

            string indexedValue = arguments[name];

            // main test condition
            string incorrectValueFailMessage = string.Format("Argument value {0} was expected for argument {1}, but the actual value was {2}.", value, name, indexedValue);
            Assert.AreEqual(value, indexedValue, incorrectValueFailMessage);
        }

        [TestMethod]
        public void IndexByName_NamedArgumentNotAdded_ReturnsNull()
        {
            const string name = "Named";

            Arguments arguments = new Arguments();

            string indexedValue = arguments[name];

            // main test condition
            string incorrectValueFailMessage = string.Format("Null was expected for argument {0}, but the actual value was {1}.", name, indexedValue);
            Assert.IsNull(indexedValue, incorrectValueFailMessage);
        }

        #endregion

        #endregion

        #region Argument Tests

        [TestMethod]
        public void Ctor_NamedArgumentsSimpleArgs_HasExpectedValues()
        {
            const string command = "/Named1:Value1 /Named2:Value2";

            Arguments arguments = new Arguments(command);

            // main test condition
            ArgumentsTests.CheckNamedArgumentHasValue(arguments, "Named1", "Value1");
            ArgumentsTests.CheckNamedArgumentHasValue(arguments, "Named2", "Value2");
        }

        #endregion

        #region State Tests

        [TestMethod]
        public void AddRange_EmptyArguments_Added()
        {
            string[] addedArgs = new string[] { "/Named:Value", "/Unnamed" };

            Arguments arguments = new Arguments();
            arguments.AddRange(addedArgs);

            // main test condition
            ArgumentsTests.CheckArgumentCountAndPositions(addedArgs, arguments);
        }

        [TestMethod]
        public void AddRange_AlreadyAddedArgument_FirstValueReplaced()
        {
            string[] initialArgs = new string[] { "/Named:Value", "Unnamed" };
            string[] addedArgs = new string[] { "/Named:NewValue" };
            string[] allArgs = initialArgs.Concat(addedArgs).ToArray();

            Arguments arguments = new Arguments(initialArgs);
            arguments.AddRange(addedArgs);

            ArgumentsTests.CheckArgumentCountAndPositions(allArgs, arguments);

            // main test condition
            ArgumentsTests.CheckNamedArgumentHasValue(arguments, "Named", "NewValue");
        }

        #endregion

        #region Helpers

        private static void CheckArgumentCountAndPositions(string[] expected, Arguments actual)
        {
            string argCountNotSameFailMessage = string.Format("A count of {0} arguments was expected, but the actual count was {1}.", expected.Length, actual.Count);
            Assert.AreEqual(expected.Length, actual.Count, argCountNotSameFailMessage);

            for (int i = 0; i < expected.Length; ++i)
            {
                string argNotSameFailMessage = string.Format("Argument {0} was expected at position {1}, but the actual argument at this position was {2}.", expected[i], i, actual[i]);
                Assert.AreEqual(expected[i], actual[i], argNotSameFailMessage);
            }
        }

        private static void CheckNamedArgumentHasValue(Arguments arguments, string name, string expectedValue)
        {
            bool nameInNamedArguments = arguments.Exists(name);
            string nameNotInNamedArgumentssFailMessage = string.Format("Argument {0} was not found in the named arguments collection.", name);
            Assert.IsTrue(nameInNamedArguments, nameNotInNamedArgumentssFailMessage);

            string actualValue = arguments[name];
            string namedArgumentValueDoesNotMatch = string.Format("Argument {0} had value {1}, but expected value was {2}.", name, actualValue, expectedValue);
            Assert.AreEqual(expectedValue, actualValue, namedArgumentValueDoesNotMatch);
        }

        private static string CommandFromArgs(string[] args)
        {
            return "\"" + string.Join("\" \"", args) + "\"";
        }

        private static string ArgFromNameValue(string name, string value, bool addQuotes)
        {
            string arg = "/" + name + ":" + value;

            if (addQuotes)
            {
                arg = "\"" + arg.Replace("\"", "\\\"") + "\"";
            }

            return arg;
        }

        #endregion
    }
}
