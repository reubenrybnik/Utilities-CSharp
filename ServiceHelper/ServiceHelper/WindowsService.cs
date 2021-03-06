﻿using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;

namespace ServiceHelper
{
    /// <summary>
    /// A class for managing the installation and execution of an implementation class written to act
    /// as a Windows service with a method that is periodically run in a loop.
    /// </summary>
    /// <typeparam name="T">The type providing the implementation of the service; must extend
    /// <see cref="ServiceHelper.WindowsServiceImplementation" /> and have a public parameterless
    /// constructor.</typeparam>
    public class WindowsService<T> : ServiceBase
        where T : WindowsServiceImplementation, new()
    {
        #region Entry Point

        private static readonly string name = !string.IsNullOrEmpty(Properties.Settings.Default.ServiceName) ?
                                              Properties.Settings.Default.ServiceName : typeof(T).Name;

        /// <summary>
        /// The name of the service.
        /// </summary>
        public static string Name
        {
            get { return WindowsService<T>.name; }
        }

        #region Test Mode

#if DEBUG
        private static bool testMode;

        /// <summary>
        /// Called to not allocate a console when running the service in debug mode.
        /// </summary>
        internal static void SetTestMode()
        {
            WindowsService<T>.testMode = true;
        }
#else
        private const bool testMode = false;
#endif

        #endregion

        /// <summary>
        /// Provides command line usage information for this service.
        /// </summary>
        private static void Usage()
        {
            string executable = Assembly.GetEntryAssembly().ManifestModule.Name;

            Console.WriteLine(executable + " <Operation>");
            Console.WriteLine();
            Console.WriteLine("Running this program directly provides the means to control, debug, install, or uninstall this service.");
            Console.WriteLine();
            Console.WriteLine("The operations below can be used to control the service instead of using a control mechanism built in to the operating system.");
            Console.WriteLine("/Start - Starts the service.");
            Console.WriteLine("/Stop - Stops the service.");
            Console.WriteLine("/Restart - Restarts the service.");
            Console.WriteLine();
            Console.WriteLine("This program also offers the following advanced operations:");
            Console.WriteLine("/I[nstall] - Install the service on a machine.");
            Console.WriteLine("/U[ninstall] - Remove a service from a machine.");
            Console.WriteLine("/Debug - Run the executable in debug mode without running it as a service.");
            Console.WriteLine("For more information on the additional parameters available for these operations, run " + executable + " <Operation> /?");
        }

        /// <summary>
        /// Provides command line usage information for installing this service.
        /// </summary>
        private static void UsageInstall()
        {
            string executable = Assembly.GetEntryAssembly().ManifestModule.Name;

            string userNameDefaultStatement;
            if (Properties.Settings.Default.DefaultServiceAccount == ServiceAccount.User)
            {
                userNameDefaultStatement = "If this argument is not provided, the user will be prompted to enter a value.";
            }
            else
            {
                userNameDefaultStatement = "The default is " + Properties.Settings.Default.DefaultServiceAccount.ToString() + ".";
            }

            string displayNameDefault;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.DefaultServiceDisplayName))
            {
                displayNameDefault = Properties.Settings.Default.DefaultServiceDisplayName;
            }
            else
            {
                displayNameDefault = WindowsService<T>.Name;
            }
            string descriptionDefaultStatement;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.DefaultServiceDescription))
            {
                descriptionDefaultStatement = "The default is '" + Properties.Settings.Default.DefaultServiceDescription + "'.";
            }
            else
            {
                descriptionDefaultStatement = "By default, no description will be provided.";
            }

            Console.WriteLine(executable + " /I[nstall] [/Interactive|/Quiet] [/StartType:<Automatic|Manual|Disabled>] [/UserName:<LocalSystem|LocalService|NetworkService|UserName> [/Password:<Password>]] [/DisplayName:<FriendlyName>] [/Description:<Description>]");
            Console.WriteLine(executable + " /U[ninstall]");
            Console.WriteLine();
            Console.WriteLine("/Interactive - Prompt the user for each setting.");
            Console.WriteLine("/Quiet - Fail immediately if any user input is required for any reason.");
            Console.WriteLine("/StartType - Whether the service should start automatically, should start on user request, or should be disabled. The default is " + Properties.Settings.Default.DefaultServiceStartType.ToString() + ".");
            Console.WriteLine("/UserName - The user or well-known service account that the service should be run as. " + userNameDefaultStatement);
            Console.WriteLine("/Password - The password for the specified user account. If a user that is not a well-known service account is provided for the UserName argument and this argument is not supplied, the user will be prompted for the appropriate password.");
            Console.WriteLine("/DisplayName - The friendly name for the service. The default is '" + displayNameDefault + "'.");
            Console.WriteLine("/Description - The description to give the service or blank to provide no description. " + descriptionDefaultStatement);
        }

        /// <summary>
        /// Provides command line usage information for debugging this service.
        /// </summary>
        private static void UsageDebug()
        {
            string executable = Assembly.GetEntryAssembly().ManifestModule.Name;

            Console.WriteLine(executable + " /Debug [/Once] [/UserName:<UserName> [/Password:<Password>]] [/LogEvents [/EventLogSource:<SourceName> | /EventLogName:<LogName>]]");
            Console.WriteLine();
            Console.WriteLine("/Once - If this argument is provided, the service loop will be run exactly once. Otherwise, it will be looped as if the service were running normally until enter is pressed.");
            Console.WriteLine("/UserName - If this argument is provided, the service will be run as the specified user, otherwise it will be run as the current user. Service accounts cannot be specified.");
            Console.WriteLine("/Password - The password for the specified user account. If UserName is provided and Password is not, the user will be prompted for the password.");
            // TODO: figure out event logging
        }

        /// <summary>
        /// Called to run the service; should typically be called from the Main method of the referencing
        /// executable.
        /// </summary>
        /// <param name="args">The arguments to run the service with.</param>
        /// <returns>an exit code that indicates operation success or failure</returns>
        public static int Run(string[] args)
        {
            Arguments arguments = new Arguments(args);
            return WindowsService<T>.Run(arguments);
        }

        /// <summary>
        /// Called to run the service; should typically be called from the Main method of the referencing
        /// executable.
        /// </summary>
        /// <param name="arguments">The arguments to run the service with.</param>
        /// <returns>an exit code that indicates operation success or failure</returns>
        public static int Run(Arguments arguments)
        {
            int exitCode = -1;
            bool performServiceOperation = false;
            bool performInstallOperation = false;
            bool runDebugMode = Debugger.IsAttached;

            try
            {
                if (arguments.Count > 0)
                {
                    performServiceOperation =
                    (
                        arguments[0].Equals("/Start", StringComparison.CurrentCultureIgnoreCase) ||
                        arguments[0].Equals("/Stop", StringComparison.CurrentCultureIgnoreCase) ||
                        arguments[0].Equals("/Restart", StringComparison.CurrentCultureIgnoreCase)
                    );

                    performInstallOperation =
                    (
                        arguments[0].Equals("/Install", StringComparison.CurrentCultureIgnoreCase) ||
                        arguments[0].Equals("/Uninstall", StringComparison.CurrentCultureIgnoreCase) ||
                        arguments[0].Equals("/I", StringComparison.CurrentCultureIgnoreCase) ||
                        arguments[0].Equals("/U", StringComparison.CurrentCultureIgnoreCase)
                    );

                    runDebugMode = arguments[0].Equals("/Debug", StringComparison.CurrentCultureIgnoreCase);
                }

                bool consoleAttached = false;

                if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS))
                {
                    if (!(performServiceOperation || performInstallOperation || runDebugMode) ||
                        arguments.Exists("?") ||
                        arguments.Exists("help"))
                    {
                        if (performInstallOperation)
                        {
                            UsageInstall();
                        }
                        else if (runDebugMode)
                        {
                            UsageDebug();
                        }
                        else
                        {
                            Usage();
                        }

                        return 1;
                    }

                    consoleAttached = true;
                }
                else if (performServiceOperation ||
                         performInstallOperation ||
                         runDebugMode)
                {
                    if (!WindowsService<T>.testMode && !NativeMethods.AllocConsole())
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        throw new Win32Exception(lastError, "Failed to create a new console for debugging.");
                    }

                    consoleAttached = true;
                }

                Trace.AutoFlush = true;

                if (consoleAttached)
                {
                    Trace.Listeners.Add(new ConsoleTraceListener());
                }

                if (performServiceOperation)
                {
                    exitCode = WindowsService<T>.PerformOperation(arguments[0]);
                }
                else if (performInstallOperation)
                {
                    WindowsService<T>.InstallService(arguments);
                }
                else
                {
                    if (arguments.Exists("EventLogSource"))
                    {
                        string eventLogSource = arguments["EventLogSource"];
                        EventLogTraceListener eventLogListener = new EventLogTraceListener(eventLogSource);
                        Trace.Listeners.Add(eventLogListener);
                    }
                    else if (arguments.Exists("EventLogName"))
                    {
                        string eventLogName = arguments["EventLogName"];
                        EventLog eventLog = new EventLog(eventLogName);
                        EventLogTraceListener eventLogListener = new EventLogTraceListener(eventLog);
                        Trace.Listeners.Add(eventLogListener);
                    }

                    if (runDebugMode && !arguments.Exists("Service"))
                    {
                        if (Debugger.IsAttached && !WindowsService<T>.testMode)
                        {
                            // if a debugger is attached, run the service in a separate thread so exceptions
                            // will not be handled by the main method exception handler to make finding the
                            // line an exception is occurring on easier
                            ThreadStart runDebugDelegate = delegate()
                            {
                                WindowsService<T>.RunDebug(arguments);
                            };

                            Thread debugThread = new Thread(runDebugDelegate);
                            debugThread.Start();
                            debugThread.Join();
                        }
                        else
                        {
                            WindowsService<T>.RunDebug(arguments);
                        }
                    }
                    else
                    {
                        WindowsService<T>.RunService(arguments);
                    }
                }

                exitCode = 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                if (WindowsService<T>.testMode)
                {
                    throw;
                }
#endif
                if (runDebugMode)
                {
                    Trace.WriteLine("Unhandled exception:");
                    Trace.WriteLine(ex.ToString());
                }
                else
                {
                    Trace.WriteLine("The following error occurred when attempting to perform the operation: " + ex.Message);
                }

                int hr = Marshal.GetHRForException(ex);
                if (hr != 0)
                {
                    exitCode = hr;
                }
            }
            finally
            {
#if DEBUG
                if (!WindowsService<T>.testMode)
                {
                    Trace.Close();
                }
#else
                Trace.Close();
#endif
            }

            return exitCode;
        }

        /// <summary>
        /// Performs an operation on the service.
        /// </summary>
        /// <param name="operation">The operation to perform.</param>
        /// <returns>the status code for the operation. <c>0</c> indicates success and any other </returns>
        private static int PerformOperation(string operation)
        {
            using (ServiceController serviceController = new ServiceController(WindowsService<T>.Name))
            {
                if (!operation.Equals("/Start", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!operation.Equals("/Restart", StringComparison.CurrentCultureIgnoreCase) && !serviceController.CanStop)
                    {
                        Trace.WriteLine(string.Format("The service cannot be stopped in its current state of {0}.", serviceController.Status.ToString()));
                        return Constants.ERROR_INVALID_SERVICE_CONTROL;
                    }

                    Trace.WriteLine("Stopping service...");
                    serviceController.Stop();

                    TimeSpan stopTimeout = Properties.Settings.Default.StopTimeout + Properties.Settings.Default.CleanupTimeout;
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, stopTimeout);

                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                    {
                        Trace.WriteLine("The service failed to be stopped within the timeout of {0} seconds.", stopTimeout.TotalSeconds.ToString());
                        // TODO: a timeout may not be the actual cause; find a way to get the actual HResult for the
                        // failure if possible
                        return Constants.ERROR_SERVICE_REQUEST_TIMEOUT;
                    }

                    Trace.WriteLine("Service stopped successfully.");
                }

                if (!operation.Equals("/Stop", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                    {
                        Trace.WriteLine(string.Format("The service cannot be started in its current state of {0}.", serviceController.Status.ToString()));
                        return Constants.ERROR_SERVICE_ALREADY_RUNNING;
                    }

                    Trace.WriteLine("Starting service...");
                    serviceController.Start();

                    TimeSpan startTimeout = Properties.Settings.Default.SetupTimeout;
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, startTimeout);

                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        Trace.WriteLine(string.Format("The service failed to be started within the timeout of {0} seconds.", startTimeout.TotalSeconds.ToString()));
                        return Constants.ERROR_SERVICE_REQUEST_TIMEOUT;
                    }

                    Trace.WriteLine("Service started successfully.");
                }
            }

            return 0;
        }

        /// <summary>
        /// Executes a service install or uninstall command by calling
        /// ManagedInstallerClass.InstallHelper like InstallUtil.exe does.
        /// </summary>
        /// <param name="arguments">The arguments provided to the service installer.</param>
        private static void InstallService(Arguments arguments)
        {
            arguments.Add("ServiceName", WindowsService<T>.Name);
            ManagedInstallerClass.InstallHelper(arguments.AllArguments);
        }

        /// <summary>
        /// Executes the service as a service by calling ServiceBase.Run.
        /// </summary>
        /// <param name="arguments">The arguments provided to the service.</param>
        private static void RunService(Arguments arguments)
        {
            using (WindowsService<T> service = new WindowsService<T>(true))
            {
                ServiceBase.Run(service);
            }
        }

        /// <summary>
        /// Executes the service as a console application by either calling Setup,
        /// Tick, and Cleanup once manually or by "starting" the service and "stopping"
        /// it when the user presses enter in the console.
        /// </summary>
        /// <param name="arguments">The arguments provided to the service.</param>
        private static void RunDebug(Arguments arguments)
        {
            using (WindowsService<T> service = new WindowsService<T>(false))
            {
                WindowsIdentity identity = null;
                WindowsImpersonationContext impersonationContext = null;

                if (arguments.Exists("UserName"))
                {
                    NativeMethods.SafeTokenHandle userToken = null;

                    try
                    {
                        string userName = arguments["UserName"];

                        string user;
                        string domain;
                        Utils.SplitDomainUserString(userName, out user, out domain);

                        if (arguments.Exists("Password"))
                        {
                            string password = arguments["Password"];

                            if (!NativeMethods.LogonUser(user, domain, password, NativeMethods.LogonType.LOGON32_LOGON_INTERACTIVE, NativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT, out userToken))
                            {
                                throw new InvalidOperationException("The specified username and password are incorrect.");
                            }
                        }
                        else
                        {
                            bool logonSucceeded;

                            do
                            {
                                using (SecureString securePassword = Utils.SecureReadPasswordFromConsole(userName))
                                {
                                    IntPtr unmanagedSecurePassword = Marshal.SecureStringToGlobalAllocUnicode(securePassword);

                                    try
                                    {
                                        logonSucceeded = NativeMethods.LogonUser(user, domain, unmanagedSecurePassword, NativeMethods.LogonType.LOGON32_LOGON_INTERACTIVE, NativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT, out userToken);

                                        if (!logonSucceeded)
                                        {
                                            userToken.Dispose();
                                            Console.WriteLine("The specified password is incorrect.");
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.ZeroFreeGlobalAllocUnicode(unmanagedSecurePassword);
                                    }
                                }
                            }
                            while (!logonSucceeded);
                        }

                        identity = userToken.GetWindowsIdentity();
                        impersonationContext = identity.Impersonate();
                    }
                    catch
                    {
                        if (identity != null)
                        {
                            identity.Dispose();
                        }

                        throw;
                    }
                    finally
                    {
                        if (userToken != null)
                        {
                            userToken.Dispose();
                        }
                    }
                }


                using (identity)
                using (impersonationContext)
                {
                    if (arguments.Exists("Once"))
                    {
                        Console.WriteLine("Running setup...");
                        service.Setup();

                        Console.WriteLine("Running service method...");
                        service.Tick();

                        Console.WriteLine("Running cleanup...");
                        service.Cleanup();

                        Console.WriteLine("Run complete.");
                    }
                    else
                    {
                        Console.WriteLine("Starting service...");
                        service.OnStart(arguments.AllArguments);

                        Console.WriteLine("Service started successfully; press enter to end...");
                        Console.ReadLine();

                        Console.WriteLine("Stopping service...");
                        service.OnStop();

                        Console.WriteLine("Run complete.");
                    }
                }
            }
        }

        #endregion

        #region Service Control

        private bool runAsService;
        private ReusableThread serviceTaskThread;

        /// <summary>
        /// Creates a new WindowsService object.
        /// </summary>
        /// <param name="runAsService"><c>true</c> if this program is being run as a service, <c>false</c> otherwise.</param>
        private WindowsService(bool runAsService)
        {
            base.ServiceName = WindowsService<T>.Name;
            this.runAsService = runAsService;
        }

        /// <summary>
        /// On service start, runs setup and starts the service loop.
        /// </summary>
        /// <param name="args">the arguments provided to the service on startup</param>
        protected override void OnStart(string[] args)
        {
            serviceTaskThread = new ReusableThread("ServiceTask");

            DateTime setupStartTime = DateTime.Now;
            TimeSpan setupTimeout = Properties.Settings.Default.SetupTimeout;
            bool infiniteStartupTimeout = (setupTimeout == TimeSpan.Zero);

            // run the setup method
            this.serviceTaskThread.Start(Setup);

            // if the setup method takes longer than expected, report an error and fail to start
            if (!this.WaitForServiceTask(Properties.Settings.Default.SetupTimeout))
            {
                throw new System.ServiceProcess.TimeoutException("Service initialization failed to complete in a timely fashion.");
            }

            // if the setup method fails, propagate the error and fail to start
            if (this.serviceTaskThread.Exception != null)
            {
                throw new ServiceTaskFailedException("Service startup failed.", this.serviceTaskThread.Exception);
            }

            // start the service loop thread
            Thread serviceThread = new Thread(ServiceLoop)
            {
                Name = "ServiceLoop"
            };

            serviceThread.Start();
        }

        /// <summary>
        /// On service stop, signals a stop on the service loop and runs cleanup.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                // tell the service thread to stop as soon as possible
                serviceStopEvent.Set();

                // wait for the service thread to stop
                bool stopSucceeded = this.WaitForServiceTask(Properties.Settings.Default.StopTimeout);

                // if the service thread takes longer than expected to stop, attempt to abort it
                // to make sure cleanup can run safely
                if (!stopSucceeded)
                {
                    try
                    {
                        this.serviceTaskThread.Abort();
                    }
                    catch { }

                    // if the service thread cannot be aborted, fail immediately and do not clean up
                    if (!this.WaitForServiceTask(TimeSpan.FromSeconds(5)))
                    {
                        throw new System.ServiceProcess.TimeoutException("The service could not be stopped.");
                    }
                }

                // run the cleanup method
                this.serviceTaskThread.Start(Cleanup);

                // if cleanup takes longer than expected, report an error
                if (!this.WaitForServiceTask(Properties.Settings.Default.CleanupTimeout))
                {
                    throw new System.ServiceProcess.TimeoutException("Service cleanup failed to complete in a timely fashion.");
                }

                // if the cleanup method fails, propagate the error
                if (this.serviceTaskThread.Exception != null)
                {
                    throw new ServiceTaskFailedException("Service cleanup failed.", this.serviceTaskThread.Exception);
                }

                // if the service thread took longer than expected to stop, report an error
                if (!stopSucceeded)
                {
                    throw new System.ServiceProcess.TimeoutException("The service failed to stop in a timely fashion and was aborted.");
                }
            }
            finally
            {
                serviceTaskThread.Dispose();
            }
        }

        /// <summary>
        /// Waits for a service task to complete, requesting additional time to complete the operation from the
        /// service control manager if the program is being run as a service.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the service task to complete.</param>
        /// <returns><c>true</c> if the task completed within the specified timeout, <c>false</c> if it
        /// did not</returns>
        private bool WaitForServiceTask(TimeSpan timeout)
        {
            const int waitInterval = 5 * 1000; // five seconds
            DateTime startTime = DateTime.Now;
            bool infiniteTimeout = (timeout == TimeSpan.Zero);

            bool taskCompleted;

            do
            {
                if (this.runAsService)
                {
                    base.RequestAdditionalTime(waitInterval);
                }

                taskCompleted = this.serviceTaskThread.Wait(waitInterval);
            }
            while (!taskCompleted && (infiniteTimeout || DateTime.Now - startTime < timeout));

            return taskCompleted;
        }

        #endregion

        #region Service Implementation

        private WindowsServiceImplementation serviceImplementation;
        private ManualResetEvent serviceStopEvent;

        /// <summary>
        /// Performs setup operations such as creating a new service implemenatation object and calling
        /// Setup on it.
        /// </summary>
        private void Setup()
        {
            this.serviceImplementation = new T();
            this.serviceImplementation.Setup();

            this.serviceStopEvent = new ManualResetEvent(false);
            this.serviceImplementation.SetServiceStopEvent(this.serviceStopEvent);
        }

        /// <summary>
        /// Runs the main service loop and acts as a watchdog.
        /// </summary>
        private void ServiceLoop()
        {
            try
            {
                TimeSpan timeToNextTick;
                ThreadStart tickDelegate = new ThreadStart(Tick);

                do
                {
                    DateTime tickTime = DateTime.Now;

                    this.serviceTaskThread.Start(tickDelegate);

                    if (!this.serviceTaskThread.Wait(this.serviceImplementation.TimeBetweenTicks))
                    {
                        TickTimeoutEventArgs e = new TickTimeoutEventArgs();

                        this.serviceImplementation.OnTickTimeout(e);

                        if (e.Abort)
                        {
                            this.serviceTaskThread.Abort();
                        }

                        this.serviceTaskThread.Wait();
                    }

                    if (this.serviceTaskThread.Exception != null &&
                        !(this.serviceTaskThread.Exception is ThreadAbortException))
                    {
                        this.serviceImplementation.LogError("Unhandled exception in service call:" + Environment.NewLine + this.serviceTaskThread.Exception.ToString());
                    }

                    timeToNextTick = TimeSpan.Zero;

                    if (this.serviceImplementation.SleepBetweenTicks)
                    {
                        if (this.serviceImplementation.TimeToNextTick != TimeSpan.Zero)
                        {
                            timeToNextTick = this.serviceImplementation.TimeToNextTick;
                        }
                        else if (this.serviceImplementation.TimeBetweenTicks != ReusableThread.InfiniteWaitTimeSpan)
                        {
                            timeToNextTick = this.serviceImplementation.TimeBetweenTicks - (DateTime.Now - tickTime);
                        }

                        if (timeToNextTick < TimeSpan.Zero)
                        {
                            timeToNextTick = TimeSpan.Zero;
                        }
                    }
                }
                while (!this.serviceStopEvent.WaitOne(timeToNextTick));
            }
            catch (Exception ex)
            {
                this.serviceImplementation.LogError("Unhandled exception in service loop:" + Environment.NewLine + ex.ToString());
                throw;
            }
            finally
            {
                this.serviceStopEvent.Close();
            }
        }

        /// <summary>
        /// Performs tasks on each service loop such as calling Tick on the service implementation object.
        /// </summary>
        private void Tick()
        {
            this.serviceImplementation.Tick();
        }

        /// <summary>
        /// Performs cleanup operations such as calling Cleanup and Dispose on the service implementation object.
        /// </summary>
        private void Cleanup()
        {
            this.serviceImplementation.Cleanup();

            IDisposable disposableServiceImplementation = this.serviceImplementation as IDisposable;
            if (disposableServiceImplementation != null)
            {
                disposableServiceImplementation.Dispose();
            }
        }

        #endregion
    }
}
