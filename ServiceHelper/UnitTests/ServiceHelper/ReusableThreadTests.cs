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
    [TestClass]
    public sealed class ReusableThreadTests
    {
        /// <summary>
        /// No test should ever wait for more than this amount of time without failing.
        /// </summary>
        private static readonly TimeSpan testWaitTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Tests should wait at least this long to be safe.
        /// </summary>
        private const int minWaitTimeMilliseconds = 500;

        #region Functionality Tests

        #region Start

        /// <summary>
        /// ReusableThread.Start should use a thread to run workloads asynchronously.
        /// </summary>
        [TestMethod]
        public void Start_SingleCall_CallsTestMethodOnNewThread()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethodCalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<int> threadIdResult = new ReusableThreadResult<int>();

                ThreadStart testMethod = delegate()
                {
                    threadIdResult.Result = Thread.CurrentThread.ManagedThreadId;
                    testMethodCalledEvent.Set();
                };

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                // main test condition
                string threadNotDifferentFailMessage = "The test method's thread ID was identical to this thread's ID.";
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, threadIdResult.Result, threadNotDifferentFailMessage);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Start should call the passed in delegate and reuse the same thread.
        /// </summary>
        [TestMethod]
        public void Start_MultipleCalls_CallsCorrectTestMethodAndReusesThread()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethod1CalledEvent = new ManualResetEvent(false))
            using (ManualResetEvent testMethod2CalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<Thread> thread1Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod1 = delegate()
                {
                    thread1Result.Result = Thread.CurrentThread;
                    testMethod1CalledEvent.Set();
                };

                ReusableThreadResult<Thread> thread2Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod2 = delegate()
                {
                    thread2Result.Result = Thread.CurrentThread;
                    testMethod2CalledEvent.Set();
                };

                reusableThread.Start(testMethod1);

                bool testMethod1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string testMethod1NotCalledFailMessage = string.Format("Test method 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Called, testMethod1NotCalledFailMessage);

                string thread1NotDifferentFailMessage = "Test method 1's thread was identical to this thread.";
                Assert.AreNotSame(Thread.CurrentThread, thread1Result.Result, thread1NotDifferentFailMessage);

                bool testMethod1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod1NotCompletedFailMessage = string.Format("Test method 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Completed, testMethod1NotCompletedFailMessage);

                testMethod1CalledEvent.Reset();
                reusableThread.Start(testMethod2);

                bool testMethod2Called = testMethod2CalledEvent.WaitOne(testWaitTime);
                string testMethod2NotCalledFailMessage = string.Format("Test method 2 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Called, testMethod2NotCalledFailMessage);

                // main test condition
                string thread2NotSameAsThread1FailMessage = "Test method 2's thread was different from test method 1's thread.";
                Assert.AreSame(thread1Result.Result, thread2Result.Result, thread2NotSameAsThread1FailMessage);

                bool testMethod2Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod2NotCompletedFailMessage = string.Format("Test method 2 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Completed, testMethod2NotCompletedFailMessage);

                // main test condition
                testMethod1Called = testMethod1CalledEvent.WaitOne(0);
                string testMethod1CalledFailMessage = "Test method 1 was called when only test method 2 should have been called.";
                Assert.IsFalse(testMethod1Called, testMethod1CalledFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Start should call the passed in delegate and use a new thread after Abort is called.
        /// </summary>
        [TestMethod]
        public void Start_AfterAbort_CallsCorrectTestMethodAndDoesNotReuseThread()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethod1CalledEvent = new ManualResetEvent(false))
            using (ManualResetEvent testMethod2CalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<Thread> thread1Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod1 = delegate()
                {
                    thread1Result.Result = Thread.CurrentThread;
                    testMethod1CalledEvent.Set();
                    Thread.Sleep(ReusableThreadTests.testWaitTime);
                };

                ReusableThreadResult<Thread> thread2Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod2 = delegate()
                {
                    thread2Result.Result = Thread.CurrentThread;
                    testMethod2CalledEvent.Set();
                };

                reusableThread.Start(testMethod1);

                bool testMethod1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string testMethod1NotCalledFailMessage = string.Format("Test method 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Called, testMethod1NotCalledFailMessage);

                reusableThread.Abort();

                bool testMethod1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod1NotCompletedFailMessage = string.Format("Test method 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Completed, testMethod1NotCompletedFailMessage);

                Type expectedExceptionType = typeof(ThreadAbortException);
                string exceptionNotExpectedTypeFailMessage = string.Format("The Exception property of the reusable thread does not contain an exception of type {0}.", expectedExceptionType.Name);
                Assert.IsInstanceOfType(reusableThread.Exception, expectedExceptionType, exceptionNotExpectedTypeFailMessage);

                testMethod1CalledEvent.Reset();
                reusableThread.Start(testMethod2);

                bool testMethod2Called = testMethod2CalledEvent.WaitOne(testWaitTime);
                string testMethod2NotCalledFailMessage = string.Format("Test method 2 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Called, testMethod2NotCalledFailMessage);

                // main test condition
                string thread2SameAsThread1FailMessage = "Test method 2's thread was the same as test method 1's thread.";
                Assert.AreNotSame(thread1Result.Result, thread2Result.Result, thread2SameAsThread1FailMessage);

                bool testMethod2Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod2NotCompletedFailMessage = string.Format("Test method 2 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Completed, testMethod2NotCompletedFailMessage);

                // main test condition
                testMethod1Called = testMethod1CalledEvent.WaitOne(0);
                string testMethod1CalledFailMessage = "Test method 1 was called when only test method 2 should have been called.";
                Assert.IsFalse(testMethod1Called, testMethod1CalledFailMessage);
            }
        }

        #endregion

        #region Wait

        /// <summary>
        /// ReusableThread.Wait should put the thread in a WaitSleepJoin state when a workload is running.
        /// </summary>
        [TestMethod]
        public void Wait_CalledWhileRunning_Waits()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    // don't actually wait for the workload to complete
                    Thread.CurrentThread.Interrupt();

                    try
                    {
                        reusableThread.Wait();

                        // main test condition
                        Assert.Fail("The main thread did not enter a WaitSleepJoin state when Wait was called.");
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        /// <summary>
        /// ReusableThread.Wait should return false if the wait times out.
        /// </summary>
        [TestMethod]
        public void Wait_TimeoutWhileRunning_WaitReturnsFalse()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    waitResult.Result = reusableThread.Wait(ReusableThreadTests.minWaitTimeMilliseconds);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);

                // main test condition
                string waitReturnedTrueFailMessage = "Wait indicated that the test method completed when it should not have.";
                Assert.IsFalse(waitResult.Result, waitReturnedTrueFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait should return true if the workload completes before the timeout.
        /// </summary>
        [TestMethod]
        public void Wait_WorkloadCompletes_WaitReturnsTrue()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    Thread.Sleep(ReusableThreadTests.minWaitTimeMilliseconds);
                };

                reusableThread.Start(testMethod);

                // main test condition
                bool waitCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string waitReturnedFalseFailMessage = "Wait indicated that the test method did not complete when it should have.";
                Assert.IsTrue(waitCompleted, waitReturnedFalseFailMessage);
            }
        }

        #endregion

        #region Abort

        /// <summary>
        /// ReusableThread.Abort should abort a running workload and the Exception property
        /// should contain ThreadAbortException.
        /// </summary>
        [TestMethod]
        public void Abort_CalledWhileRunning_ThreadAbortedAndExceptionStored()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (AutoResetEvent testMethodCalledEvent = new AutoResetEvent(false))
            {
                ReusableThreadResult<bool> threadAbortedResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    try
                    {
                        testMethodCalledEvent.Set();
                        Thread.Sleep(ReusableThreadTests.testWaitTime);
                    }
                    catch (ThreadAbortException)
                    {
                        threadAbortedResult.Result = true;
                    }
                    finally
                    {
                        testMethodCalledEvent.Set();
                    }
                };

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                reusableThread.Abort();

                bool testMethodResumed = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotResumedFailMessage = string.Format("The test method did not resume within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodResumed, testMethodNotResumedFailMessage);

                // main test condition
                string threadNotAbortedFailMessage = string.Format("The thread was not aborted.");
                Assert.IsTrue(threadAbortedResult.Result, threadNotAbortedFailMessage);

                // main test condition
                Type expectedExceptionType = typeof(ThreadAbortException);
                string exceptionNotExpectedTypeFailMessage = string.Format("The Exception property of the reusable thread does not contain an exception of type {0}.", expectedExceptionType.Name);
                Assert.IsInstanceOfType(reusableThread.Exception, expectedExceptionType, exceptionNotExpectedTypeFailMessage);
            }
        }

        #endregion

        #region Exception

        /// <summary>
        /// When a workload does not handle an exception, that exception should be stored in the
        /// ReusableThread.Exception property.
        /// </summary>
        [TestMethod]
        public void Exception_ExceptionUnhandled_Stored()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadTestException testException = new ReusableThreadTestException();

                ThreadStart testMethod = delegate()
                {
                    throw testException;
                };

                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);

                // main test condition
                string exceptionsAreNotSameFailMessage = string.Format("The reported exception does not match the thrown exception.");
                Assert.AreSame(testException, reusableThread.Exception, exceptionsAreNotSameFailMessage);
            }
        }

        #endregion

        #endregion

        #region Argument Tests

        #region .ctor

        /// <summary>
        /// ReusableThread..ctor should accept null as a thread name.
        /// </summary>
        [TestMethod]
        public void Ctor_NullAsThreadName_ThreadHasName()
        {
            const string expectedThreadName = null;

            using (ReusableThread reusableThread = new ReusableThread(expectedThreadName))
            using (ManualResetEvent testMethodCalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<string> threadNameResult = new ReusableThreadResult<string>();

                ThreadStart testMethod = delegate()
                {
                    threadNameResult.Result = Thread.CurrentThread.Name;
                    testMethodCalledEvent.Set();
                };

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                // main test condition
                string threadNameIncorrectFailMessage = string.Format("The created reusable thread does not have name {0}.", expectedThreadName);
                Assert.AreEqual(expectedThreadName, threadNameResult.Result, threadNameIncorrectFailMessage);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread..ctor should accept an empty string as a thread name.
        /// </summary>
        [TestMethod]
        public void Ctor_EmptyStringAsThreadName_ThreadHasName()
        {
            const string expectedThreadName = "";

            using (ReusableThread reusableThread = new ReusableThread(expectedThreadName))
            using (ManualResetEvent testMethodCalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<string> threadNameResult = new ReusableThreadResult<string>();

                ThreadStart testMethod = delegate()
                {
                    threadNameResult.Result = Thread.CurrentThread.Name;
                    testMethodCalledEvent.Set();
                };

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                // main test condition
                string threadNameIncorrectFailMessage = string.Format("The created reusable thread does not have name {0}.", expectedThreadName);
                Assert.AreEqual(expectedThreadName, threadNameResult.Result, threadNameIncorrectFailMessage);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
        }

        /// <summary>
        /// When a specific name is provided to ReusableThread..ctor, the thread that is used to run workloads
        /// should have that name.
        /// </summary>
        [TestMethod]
        public void Ctor_SpecificThreadName_ThreadHasName()
        {
            const string expectedThreadName = "UnitTestThread";

            using (ReusableThread reusableThread = new ReusableThread(expectedThreadName))
            using (ManualResetEvent testMethodCalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<string> threadNameResult = new ReusableThreadResult<string>();

                ThreadStart testMethod = delegate()
                {
                    threadNameResult.Result = Thread.CurrentThread.Name;
                    testMethodCalledEvent.Set();
                };

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                // main test condition
                string threadNameIncorrectFailMessage = string.Format("The created reusable thread does not have name {0}.", expectedThreadName);
                Assert.AreEqual(expectedThreadName, threadNameResult.Result, threadNameIncorrectFailMessage);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
        }

        #endregion

        #region Start

        /// <summary>
        /// ReusableThread.Start should not accept a null delegate.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "Start accepted a null test method.")]
        public void Start_NullAsTask_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                // main test condition (should throw exception)
                reusableThread.Start(null);
            }
        }

        #endregion

        #region Wait

        // TODO: this test is being flaky; figure out why
        /// <summary>
        /// ReusableThread.Wait(int) should wait for a specified amount of time.
        /// </summary>
        [TestMethod]
        public void Wait_ValidNumberAsMillisecondsTimeout_WaitsForSpecifiedTime()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                int maxWaitTime =  ((int)ReusableThreadTests.testWaitTime.TotalMilliseconds) / 2;

                Random random = new Random();
                int randomWaitTimeMilliseconds = random.Next(maxWaitTime - ReusableThreadTests.minWaitTimeMilliseconds) + ReusableThreadTests.minWaitTimeMilliseconds;

                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    waitResult.Result = reusableThread.Wait(randomWaitTimeMilliseconds);
                };

                Stopwatch waitTimeStopwatch = new Stopwatch();
                waitTimeStopwatch.Start();

                this.Wait_RunTestMethod(reusableThread, testMethod);

                waitTimeStopwatch.Stop();

                // main test condition
                bool waitedAtLeastWaitTime = (waitTimeStopwatch.ElapsedMilliseconds >= randomWaitTimeMilliseconds);
                string didNotWaitAtLeastWaitTimeFailMessage = string.Format("The call to Wait completed in {0} milliseconds when a wait of {1} milliseconds was requested.", waitTimeStopwatch.ElapsedMilliseconds, randomWaitTimeMilliseconds);
                Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);

                // main test condition
                string waitedLongerThanReusableThreadFailMessage = "The reusable thread completed before Wait returned.";
                Assert.IsFalse(waitResult.Result, waitedLongerThanReusableThreadFailMessage);
            }
        }

        // TODO: this test is being flaky; figure out why
        /// <summary>
        /// ReusableThread.Wait(TimeSpan) should wait for a specified amount of time.
        /// </summary>
        [TestMethod]
        public void Wait_ValidNumberAsTimeout_WaitsForSpecifiedTime()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                int maxWaitTime = ((int)ReusableThreadTests.testWaitTime.TotalMilliseconds) / 2;

                Random random = new Random();
                int randomWaitTimeMilliseconds = random.Next(maxWaitTime - ReusableThreadTests.minWaitTimeMilliseconds) + ReusableThreadTests.minWaitTimeMilliseconds;
                TimeSpan randomWaitTime = TimeSpan.FromMilliseconds(randomWaitTimeMilliseconds);

                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    waitResult.Result = reusableThread.Wait(randomWaitTime);
                };

                Stopwatch waitTimeStopwatch = new Stopwatch();
                waitTimeStopwatch.Start();

                this.Wait_RunTestMethod(reusableThread, testMethod);

                waitTimeStopwatch.Stop();

                // main test condition
                bool waitedAtLeastWaitTime = (waitTimeStopwatch.Elapsed >= randomWaitTime);
                string didNotWaitAtLeastWaitTimeFailMessage = string.Format("The call to Wait completed in {0} milliseconds when a wait of {1} milliseconds was requested.", waitTimeStopwatch.ElapsedMilliseconds, randomWaitTimeMilliseconds);
                Assert.IsTrue(waitedAtLeastWaitTime, didNotWaitAtLeastWaitTimeFailMessage);

                // main test condition
                string waitedLongerThanReusableThreadFailMessage = "The reusable thread completed before Wait returned.";
                Assert.IsFalse(waitResult.Result, waitedLongerThanReusableThreadFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(int) should not accept a negative number unless it is an expected constant.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "Wait accepted a negative value.")]
        public void Wait_InvalidNumberAsMillisecondsTimeout_ExceptionThrown()
        {
            using(ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    // main test condition (should throw exception)
                    reusableThread.Wait(-2);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(TimeSpan) should not accept a negative number unless it is an expected constant.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "Wait accepted a negative value.")]
        public void Wait_InvalidTimeSpanAsTimeout_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    // main test condition (should throw exception)
                    reusableThread.Wait(TimeSpan.FromSeconds(-2));
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(int) should accept a timeout of 0.
        /// </summary>
        [TestMethod]
        public void Wait_ZeroAsMillisecondsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    // main test condition (should not throw exception)
                    waitResult.Result = reusableThread.Wait(0);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);

                string waitReturnedTrueFailMessage = "Wait indicated that the test method completed when it should not have.";
                Assert.IsFalse(waitResult.Result, waitReturnedTrueFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(TimeSpan) should accept a timeout of TimeSpan.Zero.
        /// </summary>
        [TestMethod]
        public void Wait_ZeroAsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    // main test condition (should not throw exception)
                    waitResult.Result = reusableThread.Wait(TimeSpan.Zero);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);

                string waitReturnedTrueFailMessage = "Wait indicated that the test method completed when it should not have.";
                Assert.IsFalse(waitResult.Result, waitReturnedTrueFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(int) should accept ReusableThread.InfiniteWait.
        /// </summary>
        [TestMethod]
        public void Wait_InfiniteWaitAsMillisecondsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    // don't actually wait for the workload to complete
                    Thread.CurrentThread.Interrupt();

                    try
                    {
                        // main test condition (should not throw exception other than ThreadInterruptedException)
                        reusableThread.Wait(ReusableThread.InfiniteWait);

                        Assert.Fail("The main thread did not enter a WaitSleepJoin state when Wait was called.");
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        /// <summary>
        /// ReusableThread.Wait(TimeSpan) should accept ReusableThread.InfiniteWaitTimeSpan.
        /// </summary>
        [TestMethod]
        public void Wait_InfiniteWaitAsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    // don't actually wait for the workload to complete
                    Thread.CurrentThread.Interrupt();

                    try
                    {
                        // main test condition (should not throw exception other than ThreadInterruptedException)
                        reusableThread.Wait(ReusableThread.InfiniteWaitTimeSpan);

                        Assert.Fail("The main thread did not enter a WaitSleepJoin state when Wait was called.");
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        #endregion

        #endregion

        #region State Tests

        #region .ctor

        /// <summary>
        /// Different ReusableThread objects should not use the same thread.
        /// </summary>
        [TestMethod]
        public void Ctor_MultipleCalls_UseDifferentThreads()
        {
            using (ReusableThread reusableThread1 = new ReusableThread())
            using (ReusableThread reusableThread2 = new ReusableThread())
            using (ManualResetEvent testMethod1CalledEvent = new ManualResetEvent(false))
            using (ManualResetEvent testMethod2CalledEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<Thread> thread1Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod1 = delegate()
                {
                    thread1Result.Result = Thread.CurrentThread;
                    testMethod1CalledEvent.Set();
                    Thread.Sleep(ReusableThreadTests.testWaitTime);
                };

                ReusableThreadResult<Thread> thread2Result = new ReusableThreadResult<Thread>();

                ThreadStart testMethod2 = delegate()
                {
                    thread2Result.Result = Thread.CurrentThread;
                    testMethod2CalledEvent.Set();
                };

                reusableThread1.Start(testMethod1);

                bool testMethod1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string testMethod1NotCalledFailMessage = string.Format("Test method 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Called, testMethod1NotCalledFailMessage);

                bool testMethod1Completed = reusableThread1.Wait(ReusableThreadTests.testWaitTime);
                string testMethod1NotCompletedFailMessage = string.Format("Test method 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Completed, testMethod1NotCompletedFailMessage);

                testMethod1CalledEvent.Reset();
                reusableThread2.Start(testMethod2);

                bool testMethod2Called = testMethod2CalledEvent.WaitOne(testWaitTime);
                string testMethod2NotCalledFailMessage = string.Format("Test method 2 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Called, testMethod2NotCalledFailMessage);

                // main test condition
                string thread2SameAsThread1FailMessage = "Test method 2's thread was the same as test method 1's thread.";
                Assert.AreNotSame(thread1Result.Result, thread2Result.Result, thread2SameAsThread1FailMessage);

                bool testMethod2Completed = reusableThread2.Wait(ReusableThreadTests.testWaitTime);
                string testMethod2NotCompletedFailMessage = string.Format("Test method 2 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Completed, testMethod2NotCompletedFailMessage);

                testMethod1Called = testMethod1CalledEvent.WaitOne(0);
                string testMethod1CalledFailMessage = "Test method 1 was called when only test method 2 should have been called.";
                Assert.IsFalse(testMethod1Called, testMethod1CalledFailMessage);
            }
        }

        #endregion

        #region Start

        /// <summary>
        /// ReusableThread.Start should throw an exception if it is called while another workload is running.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "Start allowed a second test method to be run when a test method was already running.")]
        public void Start_CalledWhileRunning_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethod1CalledEvent = new ManualResetEvent(false))
            using (ManualResetEvent testMethod1WaitEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<bool> testMethod1ReleasedResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod1 = delegate()
                {
                    testMethod1CalledEvent.Set();
                    testMethod1ReleasedResult.Result = testMethod1WaitEvent.WaitOne(ReusableThreadTests.testWaitTime);
                };

                ReusableThreadResult<bool> testMethod2Called = new ReusableThreadResult<bool>();

                ThreadStart testMethod2 = delegate()
                {
                    testMethod2Called.Result = true;
                };

                reusableThread.Start(testMethod1);

                bool testMethod1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string testMethod1NotCalledFailMessage = string.Format("Test method 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Called, testMethod1NotCalledFailMessage);

                try
                {
                    // main test condition (should throw exception)
                    reusableThread.Start(testMethod2);
                }
                finally
                {
                    testMethod1WaitEvent.Set();

                    bool testMethod1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string testMethod1NotCompletedFailMessage = string.Format("Test method 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethod1Completed, testMethod1NotCompletedFailMessage);

                    string threadNotReleasedFailMessage = string.Format("Test Method 1 was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethod1ReleasedResult.Result, threadNotReleasedFailMessage);

                    string testMethod2CalledFailMessage = "Test method 2 was called when only test method 1 should have been called.";
                    Assert.IsFalse(testMethod2Called.Result, testMethod2CalledFailMessage);
                }
            }
        }

        /// <summary>
        /// ReusableThread.Start should clear the ReusableThread.Exception property when starting the next workload.
        /// </summary>
        [TestMethod]
        public void Start_CalledAfterException_ExceptionCleared()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethod2WaitEvent = new ManualResetEvent(false))
            {
                ThreadStart testMethod1 = delegate()
                {
                    throw new ReusableThreadTestException();
                };

                ThreadStart testMethod2 = delegate()
                {
                    testMethod2WaitEvent.WaitOne(ReusableThreadTests.testWaitTime);
                };

                reusableThread.Start(testMethod1);

                bool testMethod1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod1NotCompletedFailMessage = string.Format("Test method 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod1Completed, testMethod1NotCompletedFailMessage);

                reusableThread.Start(testMethod2);

                // main test condition
                string exceptionNotNullFailMessage = "The reusable thread's Exception field is not null after starting a new test method.";
                Assert.IsNull(reusableThread.Exception, exceptionNotNullFailMessage);

                testMethod2WaitEvent.Set();

                bool testMethod2Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethod2NotCompletedFailMessage = string.Format("Test method 2 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethod2Completed, testMethod2NotCompletedFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Start should not allow a test method to be run after the ReusableThread object is disposed.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException), "Start allowed a test method to be run after the ReusableThread was disposed.")]
        public void Start_CalledAfterDisposed_ExceptionThrown()
        {
            ThreadStart testMethod = delegate()
            {
            };

            ReusableThread reusableThread = new ReusableThread();
            try
            {
                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
            finally
            {
                reusableThread.Dispose();
            }

            // main test condition (should throw exception)
            reusableThread.Start(testMethod);
        }

        #endregion

        #region Wait

        /// <summary>
        /// ReusableThread.Wait should return true if a workload has never been started.
        /// </summary>
        [TestMethod]
        public void Wait_CalledBeforeRunning_ReturnsTrue()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                // main test condition
                bool waitCompleted = reusableThread.Wait(ReusableThreadTests.minWaitTimeMilliseconds);
                string waitTimedOutFailMessage = string.Format("The wait timed out after {0} milliseconds.", ReusableThreadTests.minWaitTimeMilliseconds);
                Assert.IsTrue(waitCompleted, waitTimedOutFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait should return true after a workload has completed.
        /// </summary>
        [TestMethod]
        public void Wait_CalledAfterRunning_ReturnsTrue()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                };

                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);

                // main test condition
                bool waitCompleted = reusableThread.Wait(ReusableThreadTests.minWaitTimeMilliseconds);
                string waitTimedOutFailMessage = string.Format("The wait timed out after {0} milliseconds.", ReusableThreadTests.minWaitTimeMilliseconds);
                Assert.IsTrue(waitCompleted, waitTimedOutFailMessage);
            }
        }
        
        /// <summary>
        /// ReusableThread.Wait should return true when waiting on a workload that is aborted by calling
        /// ReusableThread.Abort.
        /// </summary>
        [TestMethod]
        public void Wait_CalledWhileRunningBeforeAbort_ReturnsTrue()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethodCalledEvent = new ManualResetEvent(false))
            {
                ThreadStart testMethod = delegate()
                {
                    testMethodCalledEvent.Set();
                    Thread.Sleep(ReusableThreadTests.testWaitTime);
                };

                ReusableThreadResult<bool> waitResult = new ReusableThreadResult<bool>();

                ThreadStart waitMethod = delegate()
                {
                    waitResult.Result = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                };

                Thread waitThread = new Thread(waitMethod);

                reusableThread.Start(testMethod);

                bool testMethodCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string testMethodNotCalledFailMessage = string.Format("The test method was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCalled, testMethodNotCalledFailMessage);

                waitThread.Start();
                
                // TODO: if possible, avoid a thread sleep
                Thread.Sleep(ReusableThreadTests.minWaitTimeMilliseconds);

                reusableThread.Abort();

                bool waitThreadCompleted = waitThread.Join(ReusableThreadTests.testWaitTime);
                string waitThreadNotCompletedFailMessage = string.Format("The wait thread failed to complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(waitThreadCompleted, waitThreadNotCompletedFailMessage);

                // main test condition
                string waitTimedOutFailMessage = string.Format("The wait timed out after {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(waitResult.Result, waitTimedOutFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait should return true when called on a workload that was aborted by an external
        /// actor calling Thread.Abort.
        /// </summary>
        [TestMethod]
        public void Wait_CalledWithExternalThreadAbort_ReturnsTrue()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    Thread.CurrentThread.Abort();
                };

                reusableThread.Start(testMethod);

                // main test condition
                bool waitResult = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string waitTimedOutFailMessage = string.Format("The wait timed out after {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(waitResult, waitTimedOutFailMessage);
            }
        }

        /// <summary>
        /// ReusableThread.Wait should return true if it is called after the ReusableThread object is disposed.
        /// </summary>
        [TestMethod]
        public void Wait_CalledAfterDisposed_ReturnsTrue()
        {
            ThreadStart testMethod = delegate()
            {
            };

            ReusableThread reusableThread = new ReusableThread();
            try
            {
                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
            finally
            {
                reusableThread.Dispose();
            }

            // main test condition
            bool waitResult = reusableThread.Wait(ReusableThreadTests.minWaitTimeMilliseconds);
            string waitTimedOutFailMessage = string.Format("The wait timed out after {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
            Assert.IsTrue(waitResult, waitTimedOutFailMessage);
        }

        #endregion

        #region Abort

        /// <summary>
        /// ReusableThread.Abort should not throw an exception if it is called before a workload is run.
        /// </summary>
        [TestMethod]
        public void Abort_CalledBeforeRunning_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                // main test condition (exception not thrown)
                reusableThread.Abort();
            }
        }

        /// <summary>
        /// ReusableThread.Abort should not throw an exception if it is called after a workload completes.
        /// </summary>
        [TestMethod]
        public void Abort_CalledAfterRunning_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                };

                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);

                // main test condition (exception not thrown)
                reusableThread.Abort();
            }
        }

        /// <summary>
        /// ReusableThread.Abort should not throw an exception if it is called after the ReusableThread object is
        /// disposed.
        /// </summary>
        [TestMethod]
        public void Abort_CalledAfterDisposed_ExceptionThrown()
        {
            ThreadStart testMethod = delegate()
            {
            };

            ReusableThread reusableThread = new ReusableThread();
            try
            {
                reusableThread.Start(testMethod);

                bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);
            }
            finally
            {
                reusableThread.Dispose();
            }

            // main test condition (should not throw exception)
            reusableThread.Abort();
        }

        #endregion

        #endregion

        #region Helpers

        /// <summary>
        /// Provides a common implementation pattern for tests against ReusableThread.Wait.
        /// </summary>
        /// <param name="reusableThread">The reusable thread object being tested.</param>
        /// <param name="testMethod">The wait test to run.</param>
        private void Wait_RunTestMethod(ReusableThread reusableThread, ThreadStart testMethod)
        {
            using (ManualResetEvent testMethodWaitEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<bool> testMethodReleasedResult = new ReusableThreadResult<bool>();

                ThreadStart startMethod = delegate()
                {
                    testMethodReleasedResult.Result = testMethodWaitEvent.WaitOne(ReusableThreadTests.testWaitTime);
                };

                reusableThread.Start(startMethod);

                try
                {
                    testMethod();
                }
                finally
                {
                    testMethodWaitEvent.Set();

                    bool testMethodCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string testMethodNotCompletedFailMessage = string.Format("The test method did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethodCompleted, testMethodNotCompletedFailMessage);

                    string threadNotReleasedFailMessage = string.Format("The thread was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethodReleasedResult.Result, threadNotReleasedFailMessage);
                }
            }
        }

        #endregion

        #region Support Classes

        /// <summary>
        /// Provides a way to pass results across threads. This is required because assert calls on threads
        /// other than the main test thread will crash the test.
        /// </summary>
        /// <typeparam name="T">The type of the result to be held by this object.</typeparam>
        private sealed class ReusableThreadResult<T>
        {
            public T Result;
        }

        /// <summary>
        /// Provides a unique exception type that can be thrown in test delegates passed to ReusableThread.
        /// </summary>
        private sealed class ReusableThreadTestException : Exception
        {
        }

        #endregion
    }
}
