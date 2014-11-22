using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ServiceHelper
{
    /// <summary>
    /// Serves as the base class for service implementations that use ServiceHelper. If the parent
    /// class implements IDisposable, Dispose will be called when the service is stopped.
    /// 
    /// For long-running main service methods, it is recommended to design the method with periodic
    /// safe stopping points and use a StopRequested method at each of those points to check whether or
    /// not a service stop has been requested. Please note also that WakeOnStopRequested methods are
    /// provided for points where it is safe to stop the service where a call to Thread.Sleep might
    /// otherwise be used.
    /// </summary>
    public abstract class WindowsServiceImplementation
    {
        #region Implementation Stubs

        /// <summary>
        /// <c>true</c> to instruct the service to sleep between ticks, <c>false</c> to loop immediately
        /// each time Tick completes. Can be dynamically toggled based on whether the service has additional
        /// work that can be performed immediately. If this is overridden to <c>true</c>, either
        /// <see cref="TimeToNextTick"/> or <see cref="TimeBetweenTicks"/> should be overridden as well.
        /// 
        /// Default value is <c>false</c>.
        /// </summary>
        protected internal virtual bool SleepBetweenTicks
        {
            get { return false; }
        }

        /// <summary>
        /// Provides the amount of time to sleep between the completion of one tick and the
        /// start of the next. If set to <c>TimeSpan.Zero</c>, <see cref="TimeBetweenTicks"/> will be used instead.
        /// Ignored if <see cref="SleepBetweenTicks" /> is <c>false</c>.
        /// 
        /// Default value is <c>TimeSpan.Zero</c>.
        /// </summary>
        protected internal virtual TimeSpan TimeToNextTick
        {
            get { return TimeSpan.Zero;  }
        }

        /// <summary>
        /// If <see cref="SleepBetweenTicks" /> is <c>true</c> and <see cref="TimeToNextTick" /> is
        /// <c>TimeSpan.Zero</c>, provides the service with the amount of time to wait from the start of one tick
        /// to the start of the next. If a tick does not complete within this time, the new tick is started
        /// immediately after the previous tick completes. Regardless of other settings, if this is not set to
        /// <c>TimeSpan.Zero</c> and a tick does not complete within the time specified by this property, the
        /// <see cref="TickTimeout" /> event will be fired.
        /// 
        /// Default value is <c>Timeout.InfiniteTimeSpan</c>.
        /// </summary>
        protected internal virtual TimeSpan TimeBetweenTicks
        {
            get { return Constants.InfiniteTimeSpan; }
        }

        /// <summary>
        /// Can be overridden to perform additoinal setup tasks outside of the class constructor.
        /// </summary>
        protected internal virtual void Setup()
        {
        }

        /// <summary>
        /// Provides the implementation for the main service loop.
        /// </summary>
        protected internal abstract void Tick();

        /// <summary>
        /// Can be overridden to perform additional cleanup tasks outside of the Dispose method.
        /// </summary>
        protected internal virtual void Cleanup()
        {
        }

        #endregion

        #region Logging

        protected void DebugLog(string message)
        {
            Console.WriteLine(message);
        }

        protected void DebugLog(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        protected void Log(string message)
        {
            Trace.TraceInformation(message);
        }

        protected void Log(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        protected void LogWarning(string message)
        {
            Trace.TraceWarning(message);
        }

        protected void LogWarning(string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
        }

        protected void LogError(string message)
        {
            Trace.TraceError(message);
        }

        protected void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        #endregion

        #region Stop Request Monitoring

        private WaitHandle serviceStopEvent;

        public WaitHandle ServiceStopEvent
        {
            get { return this.serviceStopEvent; }
        }

        internal void SetServiceStopEvent(WaitHandle serviceStopEvent)
        {
            this.serviceStopEvent = serviceStopEvent;
        }

        /// <summary>
        /// Can be called to check if a service stop has been requested.
        /// </summary>
        /// <returns><c>true</c> if the service is stopping, <c>false</c> otherwise</returns>
        protected bool StopRequested()
        {
            return this.WakeOnStopRequested(TimeSpan.Zero);
        }

        /// <summary>
        /// Can be called instead of <see cref="Thread.Sleep" /> to wake early if a service stop
        /// is requested.
        /// </summary>
        /// <param name="sleepTime">The amount of time to sleep the thread.</param>
        /// <returns><c>true</c> if a service stop has been requested during the sleep, <c>false</c>
        /// otherwise</returns>
        protected bool WakeOnStopRequested(TimeSpan sleepTime)
        {
            if(sleepTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("sleepTime", "Sleep time must be positive.");
            }

            if (this.serviceStopEvent != null)
            {
                return this.serviceStopEvent.WaitOne(sleepTime);
            }
            else
            {
                if (sleepTime != TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                }

                return false;
            }
        }

        protected int WakeOnStopRequested(TimeSpan timeout, params WaitHandle[] waitHandles)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("timeout", "Timeout must be positive.");
            }
            if (waitHandles == null)
            {
                throw new ArgumentNullException("waitHandles");
            }
            if (waitHandles.Length == 0)
            {
                throw new ArgumentException("At least one wait handle must be supplied.", "waitHandles");
            }

            WaitHandle[] concatenatedWaitHandles;
            if (this.serviceStopEvent != null)
            {
                concatenatedWaitHandles = new WaitHandle[waitHandles.Length + 1];
                waitHandles.CopyTo(concatenatedWaitHandles, 0);
                waitHandles[waitHandles.Length] = this.serviceStopEvent;
            }
            else
            {
                concatenatedWaitHandles = waitHandles;
            }

            return WaitHandle.WaitAny(concatenatedWaitHandles);
        }

        /// <summary>
        /// Can be called to abort the current thread if a service stop has been requested.
        /// </summary>
        /// <exception cref="ThreadAbortException">Thrown if a service stop has been requested. This exception
        /// will automatically be re-thrown at the end of any catch block that does not call
        /// <see cref="Thread.ResetAbort" />. It is safe to allow this exception to be thrown out of the
        /// <see cref="WindowsServiceImplementation.Tick" /> method.</exception>
        protected void AbortOnStopRequested()
        {
            this.AbortOnStopRequested(TimeSpan.Zero);
        }

        /// <summary>
        /// Can be called instead of <see cref="Thread.Sleep" /> to abort the current thread if a service
        /// stop is requested.
        /// </summary>
        /// <exception cref="ThreadAbortException">Thrown if a service stop has been requested. This exception
        /// will automatically be re-thrown at the end of any catch block that does not call
        /// <see cref="Thread.ResetAbort" />. It is safe to allow this exception to be thrown out of the
        /// <see cref="WindowsServiceImplementation.Tick" /> method.</exception>
        protected void AbortOnStopRequested(TimeSpan sleepTime)
        {
            if (this.WakeOnStopRequested(sleepTime))
            {
                Thread.CurrentThread.Abort();
            }
        }

        protected int AbortOnStopRequested(TimeSpan timeout, params WaitHandle[] waitHandles)
        {
            int waitResult = this.AbortOnStopRequested(timeout, waitHandles);

            if (waitResult == waitHandles.Length)
            {
                Thread.CurrentThread.Abort();
            }

            return waitResult;
        }

        #endregion

        #region Tick Timeout

        protected event EventHandler<TickTimeoutEventArgs> TickTimeout;

        internal void OnTickTimeout(TickTimeoutEventArgs e)
        {
            if (TickTimeout != null)
            {
                TickTimeout(this, e);
            }
        }

        #endregion
    }
}
