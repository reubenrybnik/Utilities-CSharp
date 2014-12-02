using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
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

#if DEBUG
        private static bool alwaysHandleExceptions;

        /// <summary>
        /// Call this method to ensure that exceptions on the reusable thread will always be handled
        /// even if a debugger is attached.
        /// </summary>
        public static void AlwaysHandleExceptions()
        {
            ReusableThread.alwaysHandleExceptions = true;
        }
#else
        // always handle exceptions in release versions of this code
        private const bool alwaysHandleExceptions = true;
#endif

        #endregion

        private readonly string threadName;
        private bool isDisposed;
        private ThreadContext threadContext;

        /// <summary>
        /// The exception that occurred (if any) when running the last task. If the thread was aborted
        /// via a call to <see cref="ReusableThread.Abort" /> or by any external call to <see cref="Thread.Abort" />,
        /// this will contain a <see cref="ThreadAbortException" />.
        /// </summary>
        public Exception Exception
        {
            get { return (this.threadContext != null ? this.threadContext.Exception : null); }
        }

        public bool IsBusy
        {
            get { return (this.threadContext != null && !this.threadContext.IsCompleted); }
        }

        /// <summary>
        /// Creates a new reusable thread with the default name.
        /// </summary>
        public ReusableThread()
            : this(typeof(ReusableThread).Name)
        {
        }

        /// <summary>
        /// Creates a new reusable thread with the specified name.
        /// </summary>
        /// <param name="threadName">The name to assign to the thread created by this object.</param>
        public ReusableThread(string threadName)
        {
            this.threadName = threadName;
        }

        /// <summary>
        /// Releases synchronization objects used by the reusable thread, but does not terminate
        /// a workload if one is currently running. To ensure that the current workload is terminated,
        /// call <see cref="ReusableThread.Abort" /> before calling this method.
        /// </summary>
        public void Dispose()
        {
            this.isDisposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the thread context for this thread.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this object is being disposed, <c>false</c> if it is being
        /// finalized</param>
        private void Dispose(bool disposing)
        {
            if (this.threadContext != null)
            {
                this.threadContext.Dispose();
            }
        }

        /// <summary>
        /// Ensure that the thread context is disposed so its thread will terminate at the conclusion of its
        /// current workload (if any).
        /// </summary>
        ~ReusableThread()
        {
            this.Dispose(false);
        }

        // TODO: consider changing the parameters for this method to (Delegate, params object[] args)
        // and use Delegate.DynamicInvoke so it can run any delegate
        /// <summary>
        /// Starts a new workload on this reusable thread.
        /// </summary>
        /// <param name="task">A delegate representing the workload to run.</param>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
        /// <exception cref="InvalidOperationException">This object is currently running a task.</exception>
        public void Start(ThreadStart task)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(string.Format("Reusable thread {0} has been disposed.", this.ToString()));
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

        /// <summary>
        /// Waits indefinitely for the current workload to complete.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reusable thread has been disposed.</exception>
        public void Wait()
        {
            this.Wait(ReusableThread.InfiniteWait);
        }

        /// <summary>
        /// Waits for the current workload to complete within the specified timeout. Wait(0) can be called to
        /// test whether a workload is currently running.
        /// </summary>
        /// <param name="millisecondsTimeout">The maximum amount of time to wait for the workload to complete.</param>
        /// <returns><c>true</c> if the workload completes within the specified timeout or if no workload
        /// is currently running, <c>false</c> if the wait times out</returns>
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

        /// <summary>
        /// Waits for the current workload to complete within the specified timeout. Wait(TimeSpan.Zero) can be
        /// called to test whether a workload is currently running.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait for the workload to complete.</param>
        /// <returns><c>true</c> if the workload completes within the specified timeout or if no workload
        /// is currently running, <c>false</c> if the wait times out</returns>
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

        /// <summary>
        /// Aborts the current workload. This will cause the thread currently being reused to be dropped
        /// whether or not a workload is running. This object cannot start a new workload until the current one
        /// is aborted successfully; callers should call one of the <see cref="ReusableThread.Wait"/> methods to
        /// make sure that the abort is successful.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reusable thread has been disposed.</exception>
        public void Abort()
        {
            if (this.threadContext != null)
            {
                this.threadContext.Abort();
            }
        }

        /// <summary>
        /// Returns the name assigned to this ReusableThread or <see cref="string.Empty" />
        /// if the name is null.
        /// </summary>
        /// <returns>The name of the thread.</returns>
        public override string ToString()
        {
            return this.threadName ?? string.Empty;
        }

        /// <summary>
        /// Container for the thread being reused and its associated information.
        /// 
        /// It is extremely important that this object be disposed. If it is not disposed, it will never be
        /// garbage collected and its thread will most likely never end.
        /// </summary>
        private sealed class ThreadContext : IAsyncResult, IDisposable
        {
            private readonly GCHandle gcHandle;
            private readonly Thread thread;
            private readonly AutoResetEvent taskStartEvent = new AutoResetEvent(false);
            private readonly ManualResetEvent taskCompleteEvent = new ManualResetEvent(true);

            private volatile bool isAlive = true;
            private volatile ThreadStart task;
            private volatile Exception exception;

            /// <summary>
            /// The exception that occurred (if any) when running the last task. If the thread was aborted
            /// via a call to <see cref="ReusableThread.Abort" /> or by any external call to <see cref="Thread.Abort" />,
            /// this will contain a <see cref="ThreadAbortException" />.
            /// </summary>
            public Exception Exception
            {
                get { return this.exception; }
            }

            /// <summary>
            /// <c>true</c> if this context has not been aborted internally or externally, <c>false</c> if an
            /// abort has occurred.
            /// </summary>
            public bool IsAlive
            {
                get { return this.isAlive; }
            }

            #region IAsyncResult Members

            /// <summary>
            /// <c>null</c> always.
            /// </summary>
            public object AsyncState
            {
                get { return null; }
            }

            /// <summary>
            /// A wait handle that can be used to wait for a workload to complete.
            /// </summary>
            public WaitHandle AsyncWaitHandle
            {
                get { return this.taskCompleteEvent; }
            }

            /// <summary>
            /// <c>false</c> always.
            /// </summary>
            public bool CompletedSynchronously
            {
                get { return false; }
            }

            /// <summary>
            /// <c>true</c> if no workload is currently running, <c>false</c> otherwise.
            /// </summary>
            public bool IsCompleted
            {
                get { return (!this.IsAlive || this.taskCompleteEvent.WaitOne(0)); }
            }

            #endregion

            /// <summary>
            /// Creates a new reusable thread context.
            /// </summary>
            /// <param name="threadName"></param>
            public ThreadContext(string threadName)
            {
                this.thread = new Thread(TaskThread)
                {
                    Name = threadName,
                    IsBackground = true
                };

                // Prevent this object from being garbage collected until dispose is called. This will allow the
                // finalizer of the wrapping ReusableThread object to have a guaranteed alive reference to this
                // object even when the ReusableThread is being finalized and holds the only reference to this
                // object. In most cases, this is redundant because this object will also be rooted to a never-ending
                // thread, but in the rare case where Abort is called or some other exception that terminates its
                // thread occurs, this may otherwise be collected before its wrapping ReusableThread object is
                // finalized.
                this.gcHandle = GCHandle.Alloc(this);
            }

            /// <summary>
            /// Frees this object to be garbage collected and sets and closes all event handles.
            /// </summary>
            public void Dispose()
            {
                if (this.gcHandle.IsAllocated)
                {
                    this.isAlive = false;
                    this.gcHandle.Free();

                    this.taskStartEvent.Set();
                    this.taskCompleteEvent.Set();
                    this.taskStartEvent.Close();
                    this.taskCompleteEvent.Close();
                }
            }

            /// <summary>
            /// Starts a new workload on the thread wrapped by this object.
            /// </summary>
            /// <param name="task">A delegate representing the workload to run.</param>
            /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
            /// <exception cref="InvalidOperationException">This context is no longer alive and cannot be reused.</exception>
            /// <exception cref="InvalidOperationException">This object is currently running a task.</exception>
            public void Start(ThreadStart task)
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }
                if (!this.IsAlive)
                {
                    throw new InvalidOperationException("This thread is no longer alive and cannot be reused.");
                }
                if (!this.IsCompleted)
                {
                    throw new InvalidOperationException("This thread is already running a task.");
                }

                this.taskCompleteEvent.Reset();
                this.task = task;
                this.exception = null;

                if (this.thread.IsAlive)
                {
                    this.taskStartEvent.Set();
                }
                else
                {
                    thread.Start();
                }
            }

            public void Abort()
            {
                if (this.thread.IsAlive)
                {
                    this.thread.Abort();
                }
            }

            private void TaskThread()
            {
                try
                {
                    while (this.isAlive)
                    {
                        if (ReusableThread.alwaysHandleExceptions || !Debugger.IsAttached)
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

                        if (this.isAlive)
                        {
                            WaitHandle.SignalAndWait(this.taskCompleteEvent, this.taskStartEvent);
                        }
                    }
                }
                finally
                {
                    if (this.isAlive)
                    {
                        this.isAlive = false;

                        // there's a tiny chance that a race condition could occur here;
                        // catch the exception that will result just in case
                        try
                        {
                            this.taskCompleteEvent.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                }
            }
        }
    }
}
