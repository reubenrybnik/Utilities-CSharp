using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHelper;
using System;
using System.Collections.Generic;
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

        #region Setup and Cleanup

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            ReusableThread.AlwaysHandleExceptions();
        }

        #endregion

        #region Functionality Tests

        #region Start

        [TestMethod]
        public void Start_SingleCall_CallsDelegateOnNewThread()
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

                bool delegateCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string delegateNotCalledFailMessage = string.Format("The delegate was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegateCalled, delegateNotCalledFailMessage);

                string threadNotDifferentFailMessage = "The delegate's thread ID was identical to this thread's ID.";
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, threadIdResult.Result, threadNotDifferentFailMessage);

                bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);
            }
        }

        [TestMethod]
        public void Start_MultipleCalls_CallsCorrectDelegateAndReusesThread()
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

                bool delegate1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string delegate1NotCalledFailMessage = string.Format("Delegate 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegate1Called, delegate1NotCalledFailMessage);

                string thread1NotDifferentFailMessage = "Delegate 1's thread was identical to this thread.";
                Assert.AreNotSame(Thread.CurrentThread, thread1Result.Result, thread1NotDifferentFailMessage);

                bool delegate1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string delegate1NotCompletedFailMessage = string.Format("Delegate 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegate1Completed, delegate1NotCompletedFailMessage);

                testMethod1CalledEvent.Reset();
                reusableThread.Start(testMethod2);

                bool delegate2Called = testMethod2CalledEvent.WaitOne(testWaitTime);
                string delegate2NotCalledFailMessage = string.Format("Delegate 2 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegate2Called, delegate2NotCalledFailMessage);

                string thread2NotSameAsThread1FailMessage = "Delegate 2's thread was different from delegate 1's thread.";
                Assert.AreSame(thread1Result.Result, thread2Result.Result, thread2NotSameAsThread1FailMessage);

                bool delegate2Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string delegate2NotCompletedFailMessage = string.Format("Delegate 2 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegate2Completed, delegate2NotCompletedFailMessage);

                delegate1Called = testMethod1CalledEvent.WaitOne(0);
                string delegate1CalledFailMessage = "Delegate 1 was called when only delegate 2 should have been called.";
                Assert.IsFalse(delegate1Called, delegate1CalledFailMessage);
            }
        }

        [TestMethod]
        public void Start_ExceptionThrown_ExceptionReported()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadTestException testException = new ReusableThreadTestException();

                ThreadStart testMethod = delegate()
                {
                    throw testException;
                };

                reusableThread.Start(testMethod);

                bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);

                string exceptionsAreNotSameFailMessage = string.Format("The reported exception does not match the thrown exception.");
                Assert.AreSame(testException, reusableThread.Exception, exceptionsAreNotSameFailMessage);
            }
        }

        #endregion

        #region Wait

        [TestMethod]
        public void Wait_CalledWhileRunning_Waits()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    reusableThread.Wait();
                };

                Wait_LongWait_Interrupted(reusableThread, testMethod);
            }
        }

        #endregion

        #region Abort

        [TestMethod]
        public void Abort_CalledWhileRunning_ThreadAborted()
        {
        }

        #endregion

        #endregion

        #region Argument Tests

        #region .ctor

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullAsThreadName_ExceptionThrown()
        {
            using(ReusableThread reusableThread = new ReusableThread(null))
            {
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_EmptyStringAsThreadName_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread(string.Empty))
            {
            }
        }

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

                bool delegateCalled = testMethodCalledEvent.WaitOne(ReusableThreadTests.testWaitTime);
                string delegateNotCalledFailMessage = string.Format("The delegate was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegateCalled, delegateNotCalledFailMessage);

                string threadNameIncorrectFailMessage = string.Format("The created reusable thread does not have name {0}.", expectedThreadName);
                Assert.AreEqual(expectedThreadName, threadNameResult.Result, threadNameIncorrectFailMessage);

                bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);
            }
        }

        #endregion

        #region Start

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Start_NullAsTask_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                reusableThread.Start(null);
            }
        }

        #endregion

        #region Wait

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Wait_InvalidNumberAsMillisecondsTimeout_ExceptionThrown()
        {
            using(ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    reusableThread.Wait(-2);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Wait_InvalidTimeSpanAsTimeout_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    reusableThread.Wait(TimeSpan.FromSeconds(-2));
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);
            }
        }

        [TestMethod]
        public void Wait_InfiniteWaitAsMillisecondsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    reusableThread.Wait(ReusableThread.InfiniteWait);
                };

                Wait_LongWait_Interrupted(reusableThread, testMethod);
            }
        }

        [TestMethod]
        public void Wait_InfiniteWaitAsTimeout_ExceptionNotThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ThreadStart testMethod = delegate()
                {
                    reusableThread.Wait(ReusableThread.InfiniteWaitTimeSpan);
                };

                Wait_LongWait_Interrupted(reusableThread, testMethod);
            }
        }

        [TestMethod]
        public void Wait_ZeroAsMillisecondsTimeoutWhileRunning_WaitReturnsFalse()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadResult<bool> testMethodResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    testMethodResult.Result = reusableThread.Wait(0);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);

                string waitReturnedTrueFailMessage = "Wait indicated that the test method completed when it should not have.";
                Assert.IsFalse(testMethodResult.Result, waitReturnedTrueFailMessage);
            }
        }

        [TestMethod]
        public void Wait_ZeroAsTimeoutWhileRunning_WaitReturnsFalse()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                ReusableThreadResult<bool> testMethodResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    testMethodResult.Result = reusableThread.Wait(TimeSpan.Zero);
                };

                this.Wait_RunTestMethod(reusableThread, testMethod);

                string waitReturnedTrueFailMessage = "Wait indicated that the test method completed when it should not have.";
                Assert.IsFalse(testMethodResult.Result, waitReturnedTrueFailMessage);
            }
        }

        #endregion

        #endregion

        #region State Tests

        #region Start

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
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

                bool delegate1Called = testMethod1CalledEvent.WaitOne(testWaitTime);
                string delegate1NotCalledFailMessage = string.Format("Delegate 1 was not called within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                Assert.IsTrue(delegate1Called, delegate1NotCalledFailMessage);

                try
                {
                    reusableThread.Start(testMethod2);
                }
                catch (InvalidOperationException)
                {
                    testMethod1WaitEvent.Set();

                    bool delegate1Completed = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string delegate1NotCompletedFailMessage = string.Format("Delegate 1 did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(delegate1Completed, delegate1NotCompletedFailMessage);

                    string threadNotReleasedFailMessage = string.Format("Delegate 1 was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethod1ReleasedResult.Result, threadNotReleasedFailMessage);

                    string delegate2CalledFailMessage = "Delegate 2 was called when only delegate 1 should have been called.";
                    Assert.IsFalse(testMethod2Called.Result, delegate2CalledFailMessage);

                    throw;
                }
                finally
                {
                    testMethod1WaitEvent.Set();
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Start_CalledAfterDisposed_ExceptionThrown()
        {
            ReusableThread reusableThread = new ReusableThread();
            reusableThread.Dispose();

            ThreadStart testMethod = delegate()
            {
            };

            reusableThread.Start(testMethod);
        }

        #endregion

        #region Wait

        public void Wait_CalledBeforeRunning_ReturnsTrue()
        {
        }

        public void Wait_CalledAfterRunning_ReturnsTrue()
        {
        }

        #endregion

        #region Abort

        [TestMethod]
        public void Abort_CalledBeforeRunning_ExceptionNotThrown()
        {
        }

        [TestMethod]
        public void Abort_CalledAfterRunning_ExceptionNotThrown()
        {
        }

        #endregion

        #endregion

        #region Helpers

        /// <summary>
        /// When testing Wait methods, in most cases it is undesirable for both the test method and the method
        /// being invoked by the reusable thread to actually wait for a long period of time. Use
        /// <see cref="Thread.Interrupt" /> to make sure that a call to Wait put the thread into a
        /// <see cref="ThreadState.WaitSleepJoin"/> state without actually doing the whole wait.
        /// </summary>
        /// <param name="reusableThread">The reusable thread being tested.</param>
        /// <param name="longWaitTestMethod">A test method that, when invoked, would normally cause the test
        /// to wait for a long time.</param>
        private void Wait_LongWait_Interrupted(ReusableThread reusableThread, ThreadStart longWaitTestMethod)
        {
            ReusableThreadResult<Exception> interruptableThreadMethodResult = new ReusableThreadResult<Exception>();

            ThreadStart interruptableThreadMethod = delegate()
            {
                try
                {
                    this.Wait_RunTestMethod(reusableThread, longWaitTestMethod);
                }
                catch (Exception ex)
                {
                    interruptableThreadMethodResult.Result = ex;
                }
            };

            Thread interruptableThread = new Thread(interruptableThreadMethod);
            interruptableThread.Start();
            interruptableThread.Interrupt();

            bool interruptableThreadCompleted = interruptableThread.Join(ReusableThreadTests.testWaitTime);
            string interruptableThreadNotCompletedFailMessage = "The interruptable thread did not complete.";
            Assert.IsTrue(interruptableThreadCompleted, interruptableThreadNotCompletedFailMessage);

            Type expectedExceptionType = typeof(ThreadInterruptedException);
            string exceptionTypeIncorrectFailMessage = string.Format("The interruptable thread caught an exception other than type {0}.", expectedExceptionType.Name);
            Assert.IsInstanceOfType(interruptableThreadMethodResult.Result, expectedExceptionType);
        }

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

                    bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);

                    string threadNotReleasedFailMessage = string.Format("The thread was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethodReleasedResult.Result, threadNotReleasedFailMessage);
                }
            }
        }

        #endregion

        #region Support Classes

        private sealed class ReusableThreadResult<T>
        {
            public T Result;
        }

        private sealed class ReusableThreadTestException : Exception
        {
        }

        #endregion
    }
}
