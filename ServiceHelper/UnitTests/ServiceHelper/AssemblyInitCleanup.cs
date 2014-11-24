using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;

namespace ServiceHelperUnitTests
{
    [TestClass]
    public static class AssemblyInitCleanup
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            ReusableThread.AlwaysHandleExceptions();
        }
    }
}
