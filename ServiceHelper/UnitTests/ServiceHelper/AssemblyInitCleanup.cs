using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;

namespace ServiceHelperUnitTests
{
    /// <summary>
    /// Initialization and cleanup methods common to all ServiceHelper unit tests.
    /// </summary>
    [TestClass]
    public static class AssemblyInitCleanup
    {
        /// <summary>
        /// Performs initialization operations required for all ServiceHelper tests.
        /// </summary>
        /// <param name="context">not used</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            // when debugging unit tests, make sure reusable thread exception handling still happens or some
            // tests that use this functionality may fail
            ReusableThread.AlwaysHandleExceptions();
        }
    }
}
