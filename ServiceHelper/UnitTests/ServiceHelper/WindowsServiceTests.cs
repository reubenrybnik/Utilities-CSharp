using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceHelperUnitTests
{
    [TestClass]
    public sealed class WindowsServiceTests
    {
        private delegate void TestDelegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod);

        // class variables
        private static TextReader originalConsoleIn;
        private static ReusableThread reusableThread;
        private static AnonymousPipeClientStream consoleInClientPipe;
        private static StreamWriter consoleInWriter;

        // test variables
        private static TestDelegate testMethod;
        private static EventHandler<TickTimeoutEventArgs> tickTimeoutEvent;
        private static ImplementationMethod lastMethodCalled;
        private static bool sleepBetweenTicks;
        private static TimeSpan timeToNextTick;
        private static TimeSpan timeBetweenTicks;
        private static ManualResetEvent methodCalledEvent;
        private static AutoResetEvent methodWaitEvent;

        #region Init/Cleanup

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            WindowsServiceTests.reusableThread = new ReusableThread("Test Thread");

            WindowsService<DisposableMockServiceImplementation>.SetTestMode();
            WindowsServiceTests.originalConsoleIn = Console.In;

            AnonymousPipeServerStream consoleInServerPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            WindowsServiceTests.consoleInWriter = new StreamWriter(consoleInServerPipe);
            consoleInWriter.AutoFlush = true;

            WindowsServiceTests.consoleInClientPipe = new AnonymousPipeClientStream(PipeDirection.In, consoleInServerPipe.ClientSafePipeHandle);
            Console.SetIn(new StreamReader(WindowsServiceTests.consoleInClientPipe));

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
            // restore all static test variables to defaults
            WindowsServiceTests.ResetTestMethod();
            WindowsServiceTests.tickTimeoutEvent = null;
            WindowsServiceTests.lastMethodCalled = ImplementationMethod.None;
            WindowsServiceTests.sleepBetweenTicks = false;
            WindowsServiceTests.timeToNextTick = TimeSpan.Zero;
            WindowsServiceTests.timeBetweenTicks = ReusableThread.InfiniteWaitTimeSpan;
            WindowsServiceTests.methodCalledEvent.Reset();
            WindowsServiceTests.methodWaitEvent.Reset();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (WindowsServiceTests.reusableThread.Wait(TestConstants.MinWaitTime))
            {
                WindowsServiceTests.reusableThread.Abort();

                if (!WindowsServiceTests.reusableThread.Wait(TestConstants.MaxWaitTime))
                {
                    throw new TimeoutException(string.Format("Could not reset the reusable thread within {0} seconds.", TestConstants.MaxWaitTime.TotalSeconds));
                }
            }

            WindowsServiceTests.ReleaseMethod();

            // TODO: either find a way to make sure that no data remains in Console.In or
            // move class initialize and cleanup for Console.In to test initialize and cleanup
        }

        #endregion

        #region Functionality Tests

        [TestMethod]
        public void Run_DebugOnce_AllMethodsCalledOnceInOrder()
        {
            const string command = "/debug /once";

            WindowsServiceTests.StartBasicTest(command);

            // ctor
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Ctor);
            WindowsServiceTests.ReleaseMethod();

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

            WindowsServiceTests.EndBasicTest();
        }

        [TestMethod]
        public void Run_Debug_MultipleTicks()
        {
            const string command = "/debug";
            const int tickCount = 3;

            WindowsServiceTests.StartBasicTest(command);

            // ctor
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Ctor);
            WindowsServiceTests.ReleaseMethod();

            // setup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Setup);

            // ticks
            ThreadResult<WaitHandle> serviceStopEvent = new ThreadResult<WaitHandle>();

            WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                serviceStopEvent.Result = serviceImplementation.ServiceStopEvent;
                WindowsServiceTests.MethodCalled(serviceImplementation, implementationMethod);
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

            WindowsServiceTests.ResetTestMethod();
            WindowsServiceTests.ReleaseMethod();

            // cleanup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Cleanup);
            WindowsServiceTests.ReleaseMethod();

            // dispose
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Dispose);
            WindowsServiceTests.ReleaseMethod();

            WindowsServiceTests.EndBasicTest();
        }

        [TestMethod]
        [Ignore]
        public void Run_DebugUserName_Impersonates()
        {
            // this test requires a correct user name and password, which may differ on different machines
            // or in different domains and is likely sensitive information that should not be checked in
            const string userName = "TestAccount";
            const string password = "FILL IN";
            const string command = "/debug /once /username:\"" + userName + "\" /password:\"" + password + "\"";

            ThreadResult<WindowsIdentity> threadIdentity = new ThreadResult<WindowsIdentity>();

            WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                threadIdentity.Result = WindowsIdentity.GetCurrent();
                WindowsServiceTests.MethodCalled(serviceImplementation, implementationMethod);
            };

            WindowsServiceTests.StartBasicTest(command);

            // ctor
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Ctor);
            Assert.AreEqual(userName, threadIdentity.Result.Name, true);
            threadIdentity.Result.Dispose();
            threadIdentity.Result = null;
            WindowsServiceTests.ReleaseMethod();

            // setup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Setup);
            Assert.AreEqual(userName, threadIdentity.Result.Name, true);
            threadIdentity.Result.Dispose();
            threadIdentity.Result = null;
            WindowsServiceTests.ReleaseMethod();

            // tick
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);
            Assert.AreEqual(userName, threadIdentity.Result.Name, true);
            threadIdentity.Result.Dispose();
            threadIdentity.Result = null;
            WindowsServiceTests.ReleaseMethod();

            // cleanup
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Cleanup);
            Assert.AreEqual(userName, threadIdentity.Result.Name, true);
            threadIdentity.Result.Dispose();
            threadIdentity.Result = null;
            WindowsServiceTests.ReleaseMethod();

            // dispose
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Dispose);
            Assert.AreEqual(userName, threadIdentity.Result.Name, true);
            threadIdentity.Result.Dispose();
            threadIdentity.Result = null;
            WindowsServiceTests.ReleaseMethod();

            WindowsServiceTests.EndBasicTest();
        }

        [TestMethod]
        public void TimeToNextTick_Valid_Waits()
        {
            const string command = "/debug";
            const int maxWaitTime = TestConstants.MaxWaitTimeMilliseconds / 2;

            Random random = new Random();
            int randomWaitTimeMilliseconds = random.Next(maxWaitTime - TestConstants.MinWaitTimeMilliseconds) + TestConstants.MinWaitTimeMilliseconds;
            TimeSpan randomWaitTime = TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds);
            WindowsServiceTests.timeToNextTick = randomWaitTime;
            WindowsServiceTests.sleepBetweenTicks = true;

            ImplementationMethod targetMethod = ImplementationMethod.Tick;

            WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (implementationMethod == targetMethod)
                {
                    WindowsServiceTests.MethodCalled(serviceImplementation, implementationMethod);
                }
            };

            WindowsServiceTests.StartBasicTest(command);

            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);

            Stopwatch waitTimeStopwatch = new Stopwatch();
            waitTimeStopwatch.Start();

            WindowsServiceTests.ReleaseMethod();
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);

            waitTimeStopwatch.Stop();
            WindowsServiceTests.EndServiceLoop();
            WindowsServiceTests.ReleaseMethod();

            WindowsServiceTests.EndBasicTest();

            // main test condition
            bool waitedAtLeastWaitTime = (waitTimeStopwatch.ElapsedMilliseconds > randomWaitTimeMilliseconds);
            string didNotWaitAtLeastWaitTimeFailMessage = string.Format("A wait of at least {0} milliseconds was expected, but a wait of only {1} milliseconds was observed.", randomWaitTimeMilliseconds / 2, waitTimeStopwatch.ElapsedMilliseconds);
            Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);
        }

        [TestMethod]
        public void TimeBetweenTicks_Valid_Waits()
        {
            const string command = "/debug";
            const int maxWaitTime = TestConstants.MaxWaitTimeMilliseconds / 2;

            Random random = new Random();
            int randomWaitTimeMilliseconds = random.Next(maxWaitTime - TestConstants.MinWaitTimeMilliseconds) + TestConstants.MinWaitTimeMilliseconds;
            TimeSpan randomWaitTime = TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds);
            WindowsServiceTests.timeBetweenTicks = randomWaitTime;
            WindowsServiceTests.sleepBetweenTicks = true;

            ImplementationMethod targetMethod = ImplementationMethod.Tick;

            WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (implementationMethod == targetMethod)
                {
                    WindowsServiceTests.MethodCalled(serviceImplementation, implementationMethod);
                }
            };

            ThreadResult<bool> tickTimeoutEventFired = new ThreadResult<bool>();

            WindowsServiceTests.tickTimeoutEvent = delegate(object sender, TickTimeoutEventArgs e)
            {
                tickTimeoutEventFired.Result = true;
            };

            WindowsServiceTests.StartBasicTest(command);

            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);

            Stopwatch waitTimeStopwatch = new Stopwatch();
            waitTimeStopwatch.Start();

            WindowsServiceTests.ReleaseMethod();
            WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);

            waitTimeStopwatch.Stop();
            WindowsServiceTests.EndServiceLoop();
            WindowsServiceTests.ReleaseMethod();

            WindowsServiceTests.EndBasicTest();

            // main test condition
            bool waited = (waitTimeStopwatch.ElapsedMilliseconds > randomWaitTimeMilliseconds / 2);
            string didNotWaitFailMessage = string.Format("A wait of at least {0} milliseconds was expected, but a wait of only {1} milliseconds was observed.", randomWaitTimeMilliseconds / 2, waitTimeStopwatch.ElapsedMilliseconds);
            Assert.IsTrue(waited, didNotWaitFailMessage);

            // main test condition
            bool waitedLessThanWaitTime = (waitTimeStopwatch.ElapsedMilliseconds <= randomWaitTimeMilliseconds);
            string didNotWaitLessThanWaitTimeFailMessage = string.Format("A wait of no more than {0} milliseconds was expected, but a wait of {1} milliseconds was observed.", randomWaitTimeMilliseconds, waitTimeStopwatch.ElapsedMilliseconds);
            Assert.IsTrue(waitedLessThanWaitTime, didNotWaitLessThanWaitTimeFailMessage);

            string tickTimeoutEventFiredFailMessage = "The tick timeout event was fired when it was not expected to be.";
            Assert.IsFalse(tickTimeoutEventFired.Result, tickTimeoutEventFiredFailMessage);
        }

        [TestMethod]
        public void Tick_Timeout_TickTimeoutFired()
        {
            const string command = "/debug";

            WindowsServiceTests.timeBetweenTicks = TestConstants.MinWaitTime;
            WindowsServiceTests.sleepBetweenTicks = true;

            using (ManualResetEvent tickWaitEvent = new ManualResetEvent(false))
            {
                WindowsServiceTests.tickTimeoutEvent = delegate(object sender, TickTimeoutEventArgs e)
                {
                    tickWaitEvent.Set();
                };

                ImplementationMethod targetMethod = ImplementationMethod.Tick;

                WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
                {
                    if (implementationMethod == targetMethod)
                    {
                        WindowsServiceTests.MethodCalled(serviceImplementation, implementationMethod);
                    }
                };

                WindowsServiceTests.StartBasicTest(command);

                WindowsServiceTests.CheckMethodCalled(ImplementationMethod.Tick);

                bool tickTimeoutEventFired = tickWaitEvent.WaitOne(TestConstants.MaxWaitTime);

                WindowsServiceTests.EndServiceLoop();
                WindowsServiceTests.ReleaseMethod();

                WindowsServiceTests.EndBasicTest();

                // main test condition
                string tickTimeoutEventNotFiredFailMessage = "The tick timeout event was not fired.";
                Assert.IsTrue(tickTimeoutEventFired, tickTimeoutEventNotFiredFailMessage);
            }
        }

        #endregion

        #region State Tests

        [TestMethod]
        public void Ctor_ThrowsException_ExceptionPropagated()
        {
            const string command = "/debug";
            const ImplementationMethod targetMethod = ImplementationMethod.Ctor;
            TestException testException = new TestException();

            WindowsServiceTests.testMethod = testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (targetMethod == implementationMethod)
                {
                    throw testException;
                }
            };

            WindowsServiceTests.StartBasicTest(command);
            WindowsServiceTests.EndBasicTest();

            Exception exception = WindowsServiceTests.reusableThread.Exception;

            string noExceptionFailMessage = "No exception was thrown by WindowsService.Run.";
            Assert.IsNotNull(exception, noExceptionFailMessage);

            string incorrectExceptionTypeFailMessage = string.Format("An exception of type ServiceTaskFailedException was expected, but an exception of type {0} was thrown.", exception.GetType().Name);
            Assert.IsInstanceOfType(exception, typeof(ServiceTaskFailedException), incorrectExceptionTypeFailMessage);

            // because ctor is called generically via the new() constraint, reflection is used under the hood
            // which results in an extra TargetInvocationException inner exception
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            string incorrectInnerExceptionFailMessage = string.Format("The thrown ServiceTaskFailedException {0} did not contain the expected exception of type {1}.", exception.ToString(), typeof(TestException).Name);
            Assert.AreSame(testException, exception, incorrectInnerExceptionFailMessage);
        }

        [TestMethod]
        public void Setup_ThrowsException_ExceptionPropagated()
        {
            const string command = "/debug";
            const ImplementationMethod targetMethod = ImplementationMethod.Setup;
            TestException testException = new TestException();

            WindowsServiceTests.testMethod = testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (targetMethod == implementationMethod)
                {
                    throw testException;
                }
            };

            WindowsServiceTests.StartBasicTest(command);
            WindowsServiceTests.EndBasicTest();

            Exception exception = WindowsServiceTests.reusableThread.Exception;

            string noExceptionFailMessage = "No exception was thrown by WindowsService.Run.";
            Assert.IsNotNull(exception, noExceptionFailMessage);

                        string incorrectExceptionTypeFailMessage = string.Format("An exception of type ServiceTaskFailedException was expected, but an exception of type {0} was thrown.", exception.GetType().Name);
            Assert.IsInstanceOfType(exception, typeof(ServiceTaskFailedException), incorrectExceptionTypeFailMessage);

            string incorrectInnerExceptionFailMessage = string.Format("The thrown ServiceTaskFailedException {0} did not contain the expected exception of type {1}.", exception.ToString(), typeof(TestException).Name);
            Assert.AreSame(testException, exception.InnerException, incorrectInnerExceptionFailMessage);
        }

        [TestMethod]
        public void Cleanup_ThrowsException_ExceptionPropagated()
        {
            const string command = "/debug";
            const ImplementationMethod targetMethod = ImplementationMethod.Cleanup;
            TestException testException = new TestException();

            WindowsServiceTests.testMethod = testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (targetMethod == implementationMethod)
                {
                    throw testException;
                }
            };

            WindowsServiceTests.EndServiceLoop();
            WindowsServiceTests.StartBasicTest(command);
            WindowsServiceTests.EndBasicTest();

            Exception exception = WindowsServiceTests.reusableThread.Exception;

            string noExceptionFailMessage = "No exception was thrown by WindowsService.Run.";
            Assert.IsNotNull(exception, noExceptionFailMessage);

            string incorrectExceptionTypeFailMessage = string.Format("An exception of type ServiceTaskFailedException was expected, but an exception of type {0} was thrown.", exception.GetType().Name);
            Assert.IsInstanceOfType(exception, typeof(ServiceTaskFailedException), incorrectExceptionTypeFailMessage);

            string incorrectInnerExceptionFailMessage = string.Format("The thrown ServiceTaskFailedException {0} did not contain the expected exception of type {1}.", exception.ToString(), typeof(TestException).Name);
            Assert.AreSame(testException, exception.InnerException, incorrectInnerExceptionFailMessage);
        }

        [TestMethod]
        public void Dispose_ThrowsException_ExceptionPropagated()
        {
            const string command = "/debug";
            const ImplementationMethod targetMethod = ImplementationMethod.Dispose;
            TestException testException = new TestException();

            WindowsServiceTests.testMethod = testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (targetMethod == implementationMethod)
                {
                    throw testException;
                }
            };

            WindowsServiceTests.EndServiceLoop();
            WindowsServiceTests.StartBasicTest(command);
            WindowsServiceTests.EndBasicTest();

            Exception exception = WindowsServiceTests.reusableThread.Exception;

            string noExceptionFailMessage = "No exception was thrown by WindowsService.Run.";
            Assert.IsNotNull(exception, noExceptionFailMessage);

            string incorrectExceptionTypeFailMessage = string.Format("An exception of type ServiceTaskFailedException was expected, but an exception of type {0} was thrown.", exception.GetType().Name);
            Assert.IsInstanceOfType(exception, typeof(ServiceTaskFailedException), incorrectExceptionTypeFailMessage);

            string incorrectInnerExceptionFailMessage = string.Format("The thrown ServiceTaskFailedException {0} did not contain the expected exception of type {1}.", exception.ToString(), typeof(TestException).Name);
            Assert.AreSame(testException, exception.InnerException, incorrectInnerExceptionFailMessage);
        }

        [TestMethod]
        public void DebugOnce_NotDisposable_DisposeNotCalled()
        {
            const string command = "/debug /once";
            const ImplementationMethod targetMethod = ImplementationMethod.Dispose;

            ThreadResult<bool> disposeCalled = new ThreadResult<bool>();

            WindowsServiceTests.testMethod = delegate(WindowsServiceImplementation serviceImplementation, ImplementationMethod implementationMethod)
            {
                if (targetMethod == implementationMethod)
                {
                    disposeCalled.Result = true;
                }
            };

            Arguments arguments = new Arguments(command);

            ThreadStart runMethod = delegate()
            {
                WindowsService<MockServiceImplementation>.Run(arguments);
            };

            WindowsServiceTests.reusableThread.Start(runMethod);

            WindowsServiceTests.EndBasicTest();
        }

        #endregion

        #region Helpers

        private static void StartBasicTest(string command)
        {
            Arguments arguments = new Arguments(command);

            ThreadStart runMethod = delegate()
            {
                WindowsService<DisposableMockServiceImplementation>.Run(arguments);
            };

            WindowsServiceTests.reusableThread.Start(runMethod);
        }

        private static void EndBasicTest()
        {
            if (!WindowsServiceTests.reusableThread.Wait(TestConstants.MaxWaitTime))
            {
                string didNotCompleteFailMessage = string.Format("The service did not terminate within {0} seconds.", TestConstants.MaxWaitTime.TotalSeconds);
                Assert.Fail(didNotCompleteFailMessage);
            }
        }

        private static void MethodCalled(WindowsServiceImplementation serviceImplementation, ImplementationMethod actualMethod)
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

        private static void ResetTestMethod()
        {
            WindowsServiceTests.testMethod = WindowsServiceTests.MethodCalled;
        }

        #endregion

        #region Classes and Enums

        private class DisposableMockServiceImplementation : MockServiceImplementation, IDisposable
        {
        }

        private class MockServiceImplementation : WindowsServiceImplementation
        {
            protected internal override bool SleepBetweenTicks
            {
                get { return WindowsServiceTests.sleepBetweenTicks; }
            }

            protected internal override TimeSpan TimeToNextTick
            {
                get { return WindowsServiceTests.timeToNextTick; }
            }

            protected internal override TimeSpan TimeBetweenTicks
            {
                get { return WindowsServiceTests.timeBetweenTicks; }
            }

            public MockServiceImplementation()
            {
                WindowsServiceTests.testMethod(this, ImplementationMethod.Ctor);

                if (WindowsServiceTests.tickTimeoutEvent != null)
                {
                    this.TickTimeout += tickTimeoutEvent;
                }
            }

            protected internal override void Setup()
            {
                WindowsServiceTests.testMethod(this, ImplementationMethod.Setup);
            }

            protected internal override void Tick()
            {
                WindowsServiceTests.testMethod(this, ImplementationMethod.Tick);
            }

            protected internal override void Cleanup()
            {
                WindowsServiceTests.testMethod(this, ImplementationMethod.Cleanup);
            }

            public void Dispose()
            {
                WindowsServiceTests.testMethod(this, ImplementationMethod.Dispose);
            }
        }

        private enum ImplementationMethod
        {
            None = 0,
            Ctor,
            Setup,
            Tick,
            Cleanup,
            Dispose
        }

        #endregion
    }
}
