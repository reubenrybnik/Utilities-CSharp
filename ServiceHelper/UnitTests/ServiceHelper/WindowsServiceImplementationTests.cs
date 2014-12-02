using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceHelperUnitTests
{
    /// <summary>
    /// This class tests the protected helper methods provided by the WindowsServiceImplementation abstract base
    /// class.
    /// </summary>
    [TestClass]
    public sealed class WindowsServiceImplementationTests : WindowsServiceImplementation
    {
        private static ReusableThread reusableThread;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            WindowsServiceImplementationTests.reusableThread = new ReusableThread("Test Thread");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            WindowsServiceImplementationTests.reusableThread.Dispose();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            base.SetServiceStopEvent(null);

            if (WindowsServiceImplementationTests.reusableThread.IsBusy)
            {
                WindowsServiceImplementationTests.reusableThread.Abort();
                WindowsServiceImplementationTests.reusableThread = new ReusableThread("Test Thread");
            }
        }

        #region Stop Request Monitoring Tests

        #region Functionality Tests

        [TestMethod]
        public void StopRequested_StopEventNotSet_ReturnsFalse()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadResult<bool> stopRequested = new ThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.StopRequested();
                };

                this.Wait_RunTestMethod(testMethod);

                // main test condition
                string stopNotRequestedFailMessage = "A stop was not requested, but StopRequested returned true.";
                Assert.IsFalse(stopRequested.Result, stopNotRequestedFailMessage);
            }
        }

        [TestMethod]
        public void StopRequested_StopEventSet_ReturnsTrue()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(true))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadResult<bool> stopRequested = new ThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.StopRequested();
                };

                this.Wait_RunTestMethod(testMethod);

                // main test condition
                string stopNotRequestedFailMessage = "A stop was requested, but StopRequested returned false.";
                Assert.IsTrue(stopRequested.Result, stopNotRequestedFailMessage);
            }
        }

        [TestMethod]
        public void WakeOnStopRequested_StopEventNotSet_Waits()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadStart testMethod = delegate()
                {
                    Thread.CurrentThread.Interrupt();

                    // main test condition (should not throw exception other than ThreadInterruptedException)
                    base.WakeOnStopRequested(TestConstants.MaxWaitTime);
                };

                this.Wait_RunTestMethod(testMethod, typeof(ThreadInterruptedException));
            }
        }

        [TestMethod]
        public void WakeOnStopRequested_StopEventSet_ReturnsTrue()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(true))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadResult<bool> stopRequested = new ThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.StopRequested();
                };

                this.Wait_RunTestMethod(testMethod);

                // main test condition
                string stopNotRequestedFailMessage = "A stop was requested, but StopRequested returned false.";
                Assert.IsTrue(stopRequested.Result, stopNotRequestedFailMessage);
            }
        }

        [TestMethod]
        public void WaitOnStopRequested_AdditonalWaitHandleSetStopEventNotSet_ReturnsZero()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            using (ManualResetEvent otherEvent = new ManualResetEvent(true))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadResult<int> stopRequested = new ThreadResult<int>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.WakeOnStopRequested(TestConstants.MaxWaitTime, otherEvent);
                };

                this.Wait_RunTestMethod(testMethod);

                // main test condition
                string stopNotRequestedFailMessage = string.Format("The other event was set, but WakeOnStopRequested returned {0} instead of 0.", stopRequested.Result);
                Assert.AreEqual(0, stopRequested.Result, stopNotRequestedFailMessage);
            }
        }

        [TestMethod]
        public void WaitOnStopRequested_AdditonalWaitHandleNotSetStopEventSet_ReturnsOne()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(true))
            using (ManualResetEvent otherEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadResult<int> stopRequested = new ThreadResult<int>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.WakeOnStopRequested(TestConstants.MaxWaitTime, otherEvent);
                };

                this.Wait_RunTestMethod(testMethod);

                // main test condition
                string stopNotRequestedFailMessage = string.Format("The stop event was set, but WakeOnStopRequested returned {0} instead of 1.", stopRequested.Result);
                Assert.AreEqual(1, stopRequested.Result, stopNotRequestedFailMessage);
            }
        }

        [TestMethod]
        public void AbortOnStopRequested_StopEventNotSet_DoesNotAbortThread()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadStart testMethod = delegate()
                {
                    // main test condition (should not throw exception)
                    base.AbortOnStopRequested();
                };

                this.Wait_RunTestMethod(testMethod);
            }
        }

        [TestMethod]
        public void AbortOnStopRequested_StopEventSet_AbortsThread()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(true))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadStart testMethod = delegate()
                {
                    // main test condition (should throw ThreadAbortException)
                    base.AbortOnStopRequested();
                };

                this.Wait_RunTestMethod(testMethod, typeof(ThreadAbortException));
            }
        }

        #endregion

        #region Argument Tests

        [TestMethod]
        public void WakeOnStopRequested_StopEventNotSet_WaitsForSpecifiedTime()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                int maxWaitTime = ((int)TestConstants.MaxWaitTime.TotalMilliseconds) / 2;

                Random random = new Random();
                int randomWaitTimeMilliseconds = random.Next(maxWaitTime - TestConstants.MinWaitTimeMilliseconds) + TestConstants.MinWaitTimeMilliseconds;

                ThreadResult<bool> stopRequested = new ThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.WakeOnStopRequested(TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds));
                };

                Stopwatch waitTimeStopwatch = new Stopwatch();
                waitTimeStopwatch.Start();

                this.Wait_RunTestMethod(testMethod);

                waitTimeStopwatch.Stop();

                string stopNotRequestedFailMessage = "A stop was not requested, but WakeOnStopRequested returned true.";
                Assert.IsFalse(stopRequested.Result, stopNotRequestedFailMessage);

                // main test condition
                bool waitedAtLeastWaitTime = (waitTimeStopwatch.ElapsedMilliseconds >= randomWaitTimeMilliseconds);
                string didNotWaitAtLeastWaitTimeFailMessage = string.Format("The call to WakeOnStopRequested completed in {0} milliseconds when a wait of {1} milliseconds was requested.", waitTimeStopwatch.ElapsedMilliseconds, randomWaitTimeMilliseconds);
                Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);
            }
        }

        [TestMethod]
        public void WaitOnStopRequested_AdditonalWaitHandleNotSet_WaitsForSpecifiedTime()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            using (ManualResetEvent otherEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                int maxWaitTime = ((int)TestConstants.MaxWaitTime.TotalMilliseconds) / 2;

                Random random = new Random();
                int randomWaitTimeMilliseconds = random.Next(maxWaitTime - TestConstants.MinWaitTimeMilliseconds) + TestConstants.MinWaitTimeMilliseconds;

                ThreadResult<int> stopRequested = new ThreadResult<int>();

                ThreadStart testMethod = delegate()
                {
                    stopRequested.Result = base.WakeOnStopRequested(TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds), otherEvent);
                };

                Stopwatch waitTimeStopwatch = new Stopwatch();
                waitTimeStopwatch.Start();

                this.Wait_RunTestMethod(testMethod);

                waitTimeStopwatch.Stop();

                string stopNotRequestedFailMessage = string.Format("A stop was not requested, but WakeOnStopRequested returned {0} instead of WaitHandle.WaitTimeout.", stopRequested.Result);
                Assert.AreEqual(WaitHandle.WaitTimeout, stopRequested.Result, stopNotRequestedFailMessage);

                // main test condition
                bool waitedAtLeastWaitTime = (waitTimeStopwatch.ElapsedMilliseconds >= randomWaitTimeMilliseconds);
                string didNotWaitAtLeastWaitTimeFailMessage = string.Format("The call to WakeOnStopRequested completed in {0} milliseconds when a wait of {1} milliseconds was requested.", waitTimeStopwatch.ElapsedMilliseconds, randomWaitTimeMilliseconds);
                Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);
            }
        }

        [TestMethod]
        public void WakeOnStopRequested_InvalidTimeSpanAsSleepTime_ThrowsException()
        {
            ThreadStart testMethod = delegate()
            {
                // main test condition (should throw exception)
                base.WakeOnStopRequested(TimeSpan.FromSeconds(-2));
            };

            this.Wait_RunTestMethod(testMethod, typeof(ArgumentOutOfRangeException));
        }

        [TestMethod]
        public void WakeOnStopRequested_InfiniteTimeSpanAsSleepTime_DoesNotThrowException()
        {
            using (ManualResetEvent stopEvent = new ManualResetEvent(false))
            {
                base.SetServiceStopEvent(stopEvent);

                string stopEventNotSetFailMessage = "The stop event was not set.";
                Assert.AreSame(stopEvent, base.ServiceStopEvent, stopEventNotSetFailMessage);

                ThreadStart testMethod = delegate()
                {
                    Thread.CurrentThread.Interrupt();

                    // main test condition (should not throw exception other than ThreadInterruptedException)
                    base.WakeOnStopRequested(ReusableThread.InfiniteWaitTimeSpan);
                };

                this.Wait_RunTestMethod(testMethod, typeof(ThreadInterruptedException));
            }
        }

        [TestMethod]
        public void WakeOnStopRequested_NullAsWaitHandles_ThrowsException()
        {
            ThreadStart testMethod = delegate()
            {
                // main test condition (should throw exception)
                base.WakeOnStopRequested(TestConstants.MinWaitTime, null);
            };

            this.Wait_RunTestMethod(testMethod, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void WakeOnStopRequested_EmptyArrayAsWaitHandles_ThrowsException()
        {
            ThreadStart testMethod = delegate()
            {
                // main test condition (should throw exception)
                base.WakeOnStopRequested(TestConstants.MinWaitTime, new WaitHandle[0]);
            };

            this.Wait_RunTestMethod(testMethod, typeof(ArgumentException));
        }

        #endregion

        #region State Tests

        [TestMethod]
        public void StopRequested_StopEventNull_ReturnsFalse()
        {
            ThreadResult<bool> stopRequested = new ThreadResult<bool>();

            ThreadStart testMethod = delegate()
            {
                Thread.CurrentThread.Interrupt();
                stopRequested.Result = base.StopRequested();
            };

            this.Wait_RunTestMethod(testMethod);

            // main test condition
            string stopNotRequestedFailMessage = "A stop event was not set, but StopRequested returned true.";
            Assert.IsFalse(stopRequested.Result, stopNotRequestedFailMessage);
        }

        [TestMethod]
        public void WakeOnStopRequested_StopEventNull_WaitsForSpecifiedTime()
        {
            int maxWaitTime = ((int)TestConstants.MaxWaitTime.TotalMilliseconds) / 2;

            Random random = new Random();
            int randomWaitTimeMilliseconds = random.Next(maxWaitTime - TestConstants.MinWaitTimeMilliseconds) + TestConstants.MinWaitTimeMilliseconds;

            ThreadResult<bool> stopRequested = new ThreadResult<bool>();

            ThreadStart testMethod = delegate()
            {
                stopRequested.Result = base.WakeOnStopRequested(TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds));
            };

            Stopwatch waitTimeStopwatch = new Stopwatch();
            waitTimeStopwatch.Start();

            this.Wait_RunTestMethod(testMethod);

            waitTimeStopwatch.Stop();

            string stopNotRequestedFailMessage = "A stop was not requested, but WakeOnStopRequested returned true.";
            Assert.IsFalse(stopRequested.Result, stopNotRequestedFailMessage);

            // main test condition
            bool waitedAtLeastWaitTime = (waitTimeStopwatch.ElapsedMilliseconds >= randomWaitTimeMilliseconds);
            string didNotWaitAtLeastWaitTimeFailMessage = string.Format("The call to WakeOnStopRequested completed in {0} milliseconds when a wait of {1} milliseconds was requested.", waitTimeStopwatch.ElapsedMilliseconds, randomWaitTimeMilliseconds);
            Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);
        }

        #endregion

        #region Helpers

        private void Wait_RunTestMethod(ThreadStart testMethod)
        {
            this.Wait_RunTestMethod(testMethod, null);
        }

        private void Wait_RunTestMethod(ThreadStart testMethod, Type expectedExceptionType)
        {
            using (ManualResetEvent testMethodWaitEvent = new ManualResetEvent(false))
            {
                ThreadStart startMethod = delegate()
                {
                    try
                    {
                        testMethod();
                    }
                    finally
                    {
                        testMethodWaitEvent.Set();
                    }
                };

                WindowsServiceImplementationTests.reusableThread.Start(startMethod);

                bool testMethodCompleted = WindowsServiceImplementationTests.reusableThread.Wait(TestConstants.MaxWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", TestConstants.MaxWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);

                if (expectedExceptionType != null)
                {
                    if(WindowsServiceImplementationTests.reusableThread.Exception == null)
                    {
                        string noExpectedExceptionFailMessage = string.Format("An exception of type {0} was expected, but no exception occurred.", expectedExceptionType.Name);
                        Assert.Fail(noExpectedExceptionFailMessage);
                    }
                    else
                    {
                        string notExpectedExceptionTypeFailMessage = string.Format("An exception of type {0} was expected, but the following exception was received: {1}", expectedExceptionType.Name, WindowsServiceImplementationTests.reusableThread.Exception.ToString());
                        Assert.IsInstanceOfType(WindowsServiceImplementationTests.reusableThread.Exception, expectedExceptionType, notExpectedExceptionTypeFailMessage);
                    }
                }
                else if (WindowsServiceImplementationTests.reusableThread.Exception != null)
                {
                    string unexpectedExceptionFailMessage = "An exception occurred when one was not expected: " + WindowsServiceImplementationTests.reusableThread.Exception.ToString();
                    Assert.Fail(unexpectedExceptionFailMessage);
                }
            }
        }

        #endregion

        #endregion

        #region WindowsServiceImplementation Stubs

        protected internal override void Tick()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
