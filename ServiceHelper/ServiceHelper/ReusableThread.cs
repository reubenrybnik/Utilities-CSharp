﻿using System;
using System.Diagnostics;
using System.Threading;

namespace ServiceHelper
{
    /// <summary>
    /// This class exists for the limited purpose of reusing a thread to run methods while being able
    /// to abort them if they take longer than expected to complete. While this class is generally
    /// expected to use the same thread for every workload, this is not guaranteed. Specifically,
    /// if a workload is aborted, the thread that was aborted will be discarded and a new thread
    /// will be created in its place.
    /// 
    /// Threads that are created by objects of this type are designated as background threads,
    /// so workloads run through this class will not prevent the executable from terminating if no
    /// living foreground threads remain.
    /// 
    /// Please note that any objects that are allocated in thread-local storage will not be reset unless
    /// this object replaces its thread. This can be forced by calling <see cref="ReusableThread.Abort" />.
    /// </summary>
    public sealed class ReusableThread : IDisposable
    {
        /// <summary>
        /// Represents an infinite wait timeout; also defined by <see cref="Timeout.Infinite" />.
        /// </summary>
        public const int InfiniteWait = Timeout.Infinite;

        /// <summary>
        /// Represents an infinite wait timeout; also defined by <see cref="Timeout.InfiniteTimeSpan" /> in .NET 4.5 and above.
        /// </summary>
        public static readonly TimeSpan InfiniteWaitTimeSpan = TimeSpan.FromMilliseconds(ReusableThread.InfiniteWait);

        #region Debug Exception Handling

        private static bool handleExceptions = !Debugger.IsAttached;

        public static void AlwaysHandleExceptions()
        {
            ReusableThread.handleExceptions = true;
        }

        #endregion

        private readonly string threadName;
        private bool isDisposed;
        private ThreadContext threadContext;

        /// <summary>
        /// The exception that occurred (if any) when running the last task.
        /// </summary>
        public Exception Exception
        {
            get { return (this.threadContext != null ? this.threadContext.Exception : null); }
        }

        /// <summary>
        /// Creates a new reusable thread with the default name.
        /// </summary>
        public ReusableThread()
            : this("Reusable Thread")
        {
        }

        /// <summary>
        /// Creates a new reusable thread with the specified name.
        /// </summary>
        /// <param name="threadName">The name to assign to the thread created by this object.</param>
        public ReusableThread(string threadName)
        {
            if (string.IsNullOrEmpty(threadName))
            {
                throw new ArgumentNullException("threadName", "threadName cannot be null or empty.");
            }

            this.threadName = threadName;
        }

        /// <summary>
        /// Terminates the reusable thread.
        /// </summary>
        public void Dispose()
        {
            this.isDisposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            this.Abort();
        }

        ~ReusableThread()
        {
            this.Dispose(false);
        }

        public void Start(ThreadStart task)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("This reusable thread has been disposed.");
            }
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (this.threadContext == null)
            {
                this.threadContext = new ThreadContext(this.threadName);
            }
            else if (!this.threadContext.IsAlive)
            {
                this.threadContext.Dispose();
                this.threadContext = new ThreadContext(this.threadName);
            }

            this.threadContext.Start(task);
        }

        public bool Wait()
        {
            return this.Wait(0);
        }

        public bool Wait(int millisecondsTimeout)
        {
            if (millisecondsTimeout != ReusableThread.InfiniteWait && millisecondsTimeout < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", "millisecondsTimeout must be a nonnegative integer or ReusableThread.Infinite (same as Timeout.Infinite).");
            }

            return
            (
                this.threadContext == null ||
                !this.threadContext.IsAlive ||
                this.threadContext.AsyncWaitHandle.WaitOne(millisecondsTimeout)
            );
        }

        public bool Wait(TimeSpan timeout)
        {
            if (timeout != ReusableThread.InfiniteWaitTimeSpan && timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("timeout", "timeout must be a nonnegative length of time or ReusableThread.InfiniteTimeSpan (same as Timeout.InfiniteTimeSpan).");
            }

            return
            (
                this.threadContext == null ||
                !this.threadContext.IsAlive ||
                this.threadContext.AsyncWaitHandle.WaitOne(timeout)
            );
        }

        public void Abort()
        {
            if (this.threadContext != null)
            {
                this.threadContext.Dispose();
            }
        }

        private sealed class ThreadContext : IAsyncResult, IDisposable
        {
            private readonly Thread thread;
            private readonly AutoResetEvent taskStartEvent = new AutoResetEvent(false);
            private readonly ManualResetEvent taskCompleteEvent = new ManualResetEvent(true);

            private volatile bool isAlive = true;
            private volatile ThreadStart task;
            private volatile Exception exception;

            public Exception Exception
            {
                get { return this.exception; }
            }

            public bool IsAlive
            {
                get { return this.isAlive; }
            }

            #region IAsyncResult Members

            public object AsyncState
            {
                get { return null; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return this.taskCompleteEvent; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return (!this.IsAlive || this.taskCompleteEvent.WaitOne(0)); }
            }

            #endregion

            public ThreadContext(string threadName)
            {
                this.thread = new Thread(TaskThread)
                {
                    Name = threadName,
                    IsBackground = true
                };
            }

            public void Dispose()
            {
                this.isAlive = false;

                if (this.thread.IsAlive)
                {
                    try
                    {
                        if (this.taskCompleteEvent.WaitOne(0))
                        {
                            this.taskStartEvent.Set();
                        }
                        else
                        {
                            this.thread.Abort();
                        }
                    }
                    catch
                    {
                    }
                }

                this.taskCompleteEvent.Set();
                this.taskStartEvent.Close();
                this.taskCompleteEvent.Close();
            }

            public void Start(ThreadStart task)
            {
                if (!this.IsCompleted)
                {
                    throw new InvalidOperationException("This thread is already running a task.");
                }
                if (!this.IsAlive)
                {
                    throw new ObjectDisposedException("This thread cannot be reused.");
                }

                this.taskCompleteEvent.Reset();
                this.task = task;

                if (this.thread.IsAlive)
                {
                    this.taskStartEvent.Set();
                }
                else
                {
                    thread.Start();
                }
            }

            private void TaskThread()
            {
                while (this.IsAlive)
                {
                    if (ReusableThread.handleExceptions)
                    {
                        try
                        {
                            this.task();
                        }
                        catch (Exception ex)
                        {
                            this.exception = ex;
                        }
                    }
                    else
                    {
                        // if a debugger is attached, skip the exception handling to help make finding
                        // the cause of the unhandled exception easier
                        this.task();
                    }

                    WaitHandle.SignalAndWait(this.taskCompleteEvent, this.taskStartEvent);
                }
            }
        }
    }
}
