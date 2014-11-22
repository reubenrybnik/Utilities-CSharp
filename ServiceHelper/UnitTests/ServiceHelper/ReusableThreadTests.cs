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
        private static readonly TimeSpan testWaitTime = TimeSpan.FromSeconds(10);

        #region Setup and Cleanup

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            ReusableThread.AlwaysHandleExceptions();
        }

        #endregion

        #region Functionality Tests

        #region .ctor

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
            ReusableThreadTestException testException = new ReusableThreadTestException();

            using (ReusableThread reusableThread = new ReusableThread())
            {
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

        #endregion

        #region Abort

        #endregion

        #endregion

        #region Invalid Argument Tests

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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Start_NullAsTask_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            {
                reusableThread.Start(null);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Wait_InvalidNumberAsMillisecondsTimeout_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethodWaitEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<bool> testMethodReleasedResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    testMethodReleasedResult.Result = testMethodWaitEvent.WaitOne(ReusableThreadTests.testWaitTime);
                };

                reusableThread.Start(testMethod);

                try
                {
                    reusableThread.Wait(-2);
                    testMethodWaitEvent.Set();

                    string exceptionNotThrownFailMessage = "No exception was thrown by ReusableThread.Wait.";
                    Assert.Fail(exceptionNotThrownFailMessage);
                }
                catch
                {
                    testMethodWaitEvent.Set();

                    string threadNotReleasedFailMessage = string.Format("The thread was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethodReleasedResult.Result, threadNotReleasedFailMessage);

                    bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);

                    throw;
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Wait_InvalidTimeSpanAsTimeout_ExceptionThrown()
        {
            using (ReusableThread reusableThread = new ReusableThread())
            using (ManualResetEvent testMethodWaitEvent = new ManualResetEvent(false))
            {
                ReusableThreadResult<bool> testMethodReleasedResult = new ReusableThreadResult<bool>();

                ThreadStart testMethod = delegate()
                {
                    testMethodReleasedResult.Result = testMethodWaitEvent.WaitOne(ReusableThreadTests.testWaitTime);
                };

                reusableThread.Start(testMethod);

                try
                {
                    reusableThread.Wait(TimeSpan.FromSeconds(-2));
                }
                catch (ArgumentOutOfRangeException)
                {
                    testMethodWaitEvent.Set();

                    bool delegateCompleted = reusableThread.Wait(ReusableThreadTests.testWaitTime);
                    string delegateNotCompletedFailMessage = string.Format("The delegate did not complete within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(delegateCompleted, delegateNotCompletedFailMessage);

                    string threadNotReleasedFailMessage = string.Format("The delegate was not released within the timeout of {0} seconds.", ReusableThreadTests.testWaitTime.TotalSeconds);
                    Assert.IsTrue(testMethodReleasedResult.Result, threadNotReleasedFailMessage);

                    throw;
                }
                finally
                {
                    testMethodWaitEvent.Set();
                }
            }
        }

        #endregion

        #region Invalid State Tests

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
