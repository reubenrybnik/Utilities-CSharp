using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceHelperUnitTests
{
    [TestClass]
    public sealed class WindowsServiceTests
    {
        private delegate void TestDelegate(MockServiceImplementation serviceImplementation);

        // class variables
        private static TextReader originalConsoleIn;
        private static ReusableThread reusableThread;
        private static StreamWriter consoleInWriter;

        // test variables
        private static TestDelegate tickTestMethod;
        private static ImplementationMethod lastMethodCalled;
        private static ManualResetEvent methodCalledEvent;
        private static AutoResetEvent methodWaitEvent;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            WindowsServiceTests.reusableThread = new ReusableThread("Test Thread");

            WindowsService<MockServiceImplementation>.SkipConsoleAllocation();
            WindowsServiceTests.originalConsoleIn = Console.In;

            AnonymousPipeServerStream consoleInServerPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            WindowsServiceTests.consoleInWriter = new StreamWriter(consoleInServerPipe);
            consoleInWriter.AutoFlush = true;

            AnonymousPipeClientStream consoleInClientPipe = new AnonymousPipeClientStream(PipeDirection.In, consoleInServerPipe.ClientSafePipeHandle);
            Console.SetIn(new StreamReader(consoleInClientPipe));

            WindowsServiceTests.methodCalledEvent = new ManualResetEvent(false);
            WindowsServiceTests.methodWaitEvent = new AutoResetEvent(false);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            WindowsServiceTests.reusableThread.Dispose();

            TextReader consoleInReader = Console.In;
            Console.SetIn(WindowsServiceTests.originalConsoleIn);

            WindowsServiceTests.consoleInWriter.Dispose();
            consoleInReader.Dispose();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            WindowsServiceTests.lastMethodCalled = ImplementationMethod.None;
            WindowsServiceTests.methodCalledEvent.Reset();
            WindowsServiceTests.methodWaitEvent.Reset();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (WindowsServiceTests.reusableThread.IsBusy)
            {
                WindowsServiceTests.reusableThread.Abort();

                if (!WindowsServiceTests.reusableThread.Wait(TestConstants.MaxWaitTime))
                {
                    throw new TimeoutException(string.Format("Could not reset the reusable thread within {0} seconds.", TestConstants.MaxWaitTime.TotalSeconds));
                }
            }

            WindowsServiceTests.ReleaseMethod();
        }

        [TestMethod]
        public void Debug_Once_AllMethodsCalledOnceInOrder()
        {
            const string command = "/debug /once";

            WindowsServiceTests.StartBasicTest(command);

            // setup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Setup);
            WindowsServiceTests.ReleaseMethod();

            // tick
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);
            WindowsServiceTests.ReleaseMethod();

            // cleanup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Cleanup);
            WindowsServiceTests.ReleaseMethod();

            // dispose
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Dispose);
            WindowsServiceTests.ReleaseMethod();
        }

        [TestMethod]
        public void Debug_Default_MultipleTicks()
        {
            const string command = "/debug";
            const int tickCount = 3;

            WindowsServiceTests.StartBasicTest(command);

            // setup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Setup);

            // ticks
            ThreadResult<WaitHandle> serviceStopEvent = new ThreadResult<WaitHandle>();

            WindowsServiceTests.tickTestMethod = delegate(MockServiceImplementation serviceImplementation)
            {
                serviceStopEvent.Result = serviceImplementation.ServiceStopEvent;
                WindowsServiceTests.MethodCalled(ImplementationMethod.Tick);
            };

            for (int i = 0; i < tickCount; ++i)
            {
                WindowsServiceTests.ReleaseMethod();
                WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);
            }

            WindowsServiceTests.EndServiceLoop();

            bool serviceStopped = serviceStopEvent.Result.WaitOne(TestConstants.MaxWaitTime);
            string serviceNotStoppedFailMessage = string.Format("The service was not stopped within {0} seconds.", TestConstants.MaxWaitTime.TotalSeconds);
            Assert.IsTrue(serviceStopped, serviceNotStoppedFailMessage);

            WindowsServiceTests.tickTestMethod = null;
            WindowsServiceTests.ReleaseMethod();

            // cleanup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Cleanup);
            WindowsServiceTests.ReleaseMethod();

            // dispose
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Dispose);
            WindowsServiceTests.ReleaseMethod();
        }

        [TestMethod]
        [Ignore]
        public void Debug_UserName_Impersonates()
        {

        }

        private static void StartBasicTest(string command)
        {
            Arguments arguments = new Arguments(command);

            ThreadStart runMethod = delegate()
            {
                WindowsService<MockServiceImplementation>.Run(arguments);
            };

            WindowsServiceTests.reusableThread.Start(runMethod);
        }

        private static void MethodCalled(ImplementationMethod actualMethod)
        {
            WindowsServiceTests.lastMethodCalled = actualMethod;
            WaitHandle.SignalAndWait(WindowsServiceTests.methodCalledEvent, WindowsServiceTests.methodWaitEvent);
        }

        private static void CheckMethodCalled(ImplementationMethod expectedMethod)
        {
            bool methodCalled = WindowsServiceTests.methodCalledEvent.WaitOne(TestConstants.MaxWaitTime);

            if (WindowsServiceTests.reusableThread.Exception != null)
            {
                string exceptionOccurredFailMessage = string.Format("An exception occurred in the main service method when {0} was called: {1}", expectedMethod, WindowsServiceTests.reusableThread.Exception.ToString());
                Assert.Fail(exceptionOccurredFailMessage);
            }

            string methodNotCalledFailMessage = string.Format("No method was called when a call to method {0} was expected.", expectedMethod);
            Assert.IsTrue(methodCalled, methodNotCalledFailMessage);

            string incorrectMethodCalledFailMessage = string.Format("A call to method {0} was expected but a call to method {1} was received.", expectedMethod, WindowsServiceTests.lastMethodCalled);
            Assert.AreEqual(expectedMethod, WindowsServiceTests.lastMethodCalled, incorrectMethodCalledFailMessage);
        }

        private static void EndServiceLoop()
        {
            WindowsServiceTests.consoleInWriter.WriteLine();
        }

        private static void ReleaseMethod()
        {
            WindowsServiceTests.methodCalledEvent.Reset();
            WindowsServiceTests.methodWaitEvent.Set();
        }

        private sealed class MockServiceImplementation : WindowsServiceImplementation, IDisposable
        {
            protected internal override void Setup()
            {
                if (WindowsServiceTests.tickTestMethod == null)
                {
                    WindowsServiceTests.MethodCalled(ImplementationMethod.Setup);
                }
            }

            protected internal override void Tick()
            {
                if (WindowsServiceTests.tickTestMethod == null)
                {
                    WindowsServiceTests.MethodCalled(ImplementationMethod.Tick);
                }
                else
                {
                    WindowsServiceTests.tickTestMethod(this);
                }
            }

            protected internal override void Cleanup()
            {
                if (WindowsServiceTests.tickTestMethod == null)
                {
                    WindowsServiceTests.MethodCalled(ImplementationMethod.Cleanup);
                }
            }

            public void Dispose()
            {
                if (WindowsServiceTests.tickTestMethod == null)
                {
                    WindowsServiceTests.MethodCalled(ImplementationMethod.Dispose);
                }
            }
        }

        private enum ImplementationMethod
        {
            None = 0,
            Setup,
            Tick,
            Cleanup,
            Dispose
        }
    }
}
