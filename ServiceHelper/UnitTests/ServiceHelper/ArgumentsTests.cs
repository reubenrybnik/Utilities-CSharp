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

        [TestMethod]
        public void NamedArguments_SimpleArgs_HasExpectedValues()
        {
            const string command = "/Name1:Value1 /Name2:Value2";

            Arguments arguments = new Arguments(command);

            this.CheckNamedArgumentHasValue(arguments, "Name1", "Value1");
            this.CheckNamedArgumentHasValue(arguments, "Name2", "Value2");
        }

        #endregion

        #region Argument Tests

        #endregion

        #region State Tests

        #endregion

        #region Helpers

        private void CheckNamedArgumentHasValue(Arguments arguments, string name, string expectedValue)
        {
            bool nameInNamedArguments = arguments.Exists(name);
            string nameNotInNamedArgumentssFailMessage = string.Format("Argument {0} was not found in the named arguments collection.", name);
            Assert.IsTrue(nameInNamedArguments, nameNotInNamedArgumentssFailMessage);

            string actualValue = arguments[name];
            string namedArgumentValueDoesNotMatch = string.Format("Argument {0} had value {1}, but expected value was {2}.", name, actualValue, expectedValue);
            Assert.AreEqual(expectedValue, actualValue, namedArgumentValueDoesNotMatch);
        }

        #endregion
    }
}
