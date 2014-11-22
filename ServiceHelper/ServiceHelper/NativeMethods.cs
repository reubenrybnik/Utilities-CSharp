using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ServiceHelper
{
    /// <summary>
    /// P/Invoke methods used by this project.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Constant for the AttachConsole method: use the console of the parent of the current process.
        /// </summary>
        public const UInt32 ATTACH_PARENT_PROCESS = unchecked((UInt32)(-1));

        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastError"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        /// <summary>
        /// Attaches the calling process to the console of the specified process.
        /// </summary>
        /// <param name="dwProcessId">The identifier of the process whose console is to be used or
        /// <see cref="NativeMethods.ATTACH_PARENT_PROCESS"/> to attach to the console of the parent
        /// process.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastError"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(UInt32 dwProcessId);

        /// <summary>
        /// The LogonUser function attempts to log a user on to the local computer. The local computer is the
        /// computer from which LogonUser was called. You cannot use LogonUser to log on to a remote computer.
        /// You specify the user with a user name and domain and authenticate the user with a plaintext password.
        /// If the function succeeds, you receive a handle to a token that represents the logged-on user. You can
        /// then use this token handle to impersonate the specified user or, in most cases, to create a process
        /// that runs in the context of the specified user.
        /// </summary>
        /// <param name="lpszUsername">The null-terminated string that specifies the name of the user. This is the
        /// name of the user account to log on to. If you use the user principal name (UPN) format,
        /// User@DNSDomainName, the lpszDomain parameter must be NULL.</param>
        /// <param name="lpszDomain">The null-terminated string that specifies the name of the domain or server
        /// whose account database contains the lpszUsername account. If this parameter is NULL, the user name
        /// must be specified in UPN format. If this parameter is ".", the function validates the account by using
        /// only the local account database.</param>
        /// <param name="lpszPassword">The null-terminated string that specifies the plaintext password for the user
        /// account specified by lpszUsername.</param>
        /// <param name="dwLogonType">The type of logon operation to perform as enumerated by the
        /// <see cref="LogonType" /> enumeration.</param>
        /// <param name="dwLogonProvider">Specifies the logon provider as enumerated by the
        /// <see cref="LogonProvider" /> enumeration.</param>
        /// <param name="phToken">A <see cref="SafeTokenHandle" /> object that receives a handle to a token that
        /// represents the specified user. You can use this object to imersonate a user by calling
        /// <see cref="SafeTokenHandle.GetWindowsIdentity" /> to get a <see cref="WindowsIdentity" /> object
        /// and then calling <see cref="WindowsIdentity.Imersonate" />.
        /// 
        /// When you no longer need this handle, close it by disposing the <see cref="SafeTokenHandle" /> object.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastError"/>.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out SafeTokenHandle phToken);

        /// <summary>
        /// The LogonUser function attempts to log a user on to the local computer. The local computer is the
        /// computer from which LogonUser was called. You cannot use LogonUser to log on to a remote computer.
        /// You specify the user with a user name and domain and authenticate the user with a plaintext password.
        /// If the function succeeds, you receive a handle to a token that represents the logged-on user. You can
        /// then use this token handle to impersonate the specified user or, in most cases, to create a process
        /// that runs in the context of the specified user.
        /// </summary>
        /// <param name="lpszUsername">The null-terminated string that specifies the name of the user. This is the
        /// name of the user account to log on to. If you use the user principal name (UPN) format,
        /// User@DNSDomainName, the lpszDomain parameter must be NULL.</param>
        /// <param name="lpszDomain">The null-terminated string that specifies the name of the domain or server
        /// whose account database contains the lpszUsername account. If this parameter is NULL, the user name
        /// must be specified in UPN format. If this parameter is ".", the function validates the account by using
        /// only the local account database.</param>
        /// <param name="lpszPassword">A pointer to a null-terminated string that specifies the plaintext password
        /// for the user account specified by lpszUsername. This can be retrieved from a
        /// <see cref="System.Security.SecureString"/> object by calling
        /// <see cref="Marshal.SecureStringToGlobalAllocUnicode"/>. Once user logon is complete, clear the string
        /// from memory by using <see cref="Marshal.ZeroFreeGlobalAllocUnicode"/>.</param>
        /// <param name="dwLogonType">The type of logon operation to perform as enumerated by the
        /// <see cref="LogonType" /> enumeration.</param>
        /// <param name="dwLogonProvider">Specifies the logon provider as enumerated by the
        /// <see cref="LogonProvider" /> enumeration.</param>
        /// <param name="phToken">A <see cref="SafeTokenHandle" /> object that receives a handle to a token that
        /// represents the specified user. You can use this object to imersonate a user by calling
        /// <see cref="SafeTokenHandle.GetWindowsIdentity" /> to get a <see cref="WindowsIdentity" /> object
        /// and then calling <see cref="WindowsIdentity.Imersonate" />.
        /// 
        /// When you no longer need this handle, close it by disposing the <see cref="SafeTokenHandle" /> object.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastError"/>.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, IntPtr lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out SafeTokenHandle phToken);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastError"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        // TODO: check these values
        /// <summary>
        /// The type of logon operation to perform.
        /// </summary>
        internal enum LogonType : uint
        {
            /// <summary>
            /// This logon type is intended for batch servers, where processes may be executing on behalf of a user
            /// without their direct intervention. This type is also for higher performance servers that process many
            /// plaintext authentication attempts at a time, such as mail or web servers.
            /// </summary>
            LOGON32_LOGON_BATCH = 1,

            /// <summary>
            /// This logon type is intended for users who will be interactively using the computer, such as a user
            /// being logged on by a terminal server, remote shell, or similar process. This logon type has the
            /// additional expense of caching logon information for disconnected operations; therefore, it is
            /// inappropriate for some client/server applications, such as a mail server.
            /// </summary>
            LOGON32_LOGON_INTERACTIVE = 2,

            /// <summary>
            /// This logon type is intended for high performance servers to authenticate plaintext passwords. The
            /// LogonUser function does not cache credentials for this logon type.
            /// </summary>
            LOGON32_LOGON_NETWORK = 3,

            /// <summary>
            /// This logon type preserves the name and password in the authentication package, which allows the
            /// server to make connections to other network servers while impersonating the client. A server can
            /// accept plaintext credentials from a client, call LogonUser, verify that the user can access the
            /// system across the network, and still communicate with other servers.
            /// </summary>
            LOGON32_LOGON_NETWORK_CLEARTEXT = 4,

            /// <summary>
            /// This logon type allows the caller to clone its current token and specify new credentials for outbound
            /// connections. The new logon session has the same local identifier but uses different credentials for
            /// other network connections.
            ///
            /// This logon type is supported only by the LOGON32_PROVIDER_WINNT50 logon provider.
            /// </summary>
            LOGON32_LOGON_NEW_CREDENTIALS = 5,

            /// <summary>
            ///  Indicates a service-type logon. The account provided must have the service privilege enabled.
            /// </summary>
            LOGON32_LOGON_SERVICE = 6,

            /// <summary>
            /// GINAs are no longer supported.
            ///
            /// Windows Server 2003 and Windows XP:  This logon type is for GINA DLLs that log on users who will
            /// be interactively using the computer. This logon type can generate a unique audit record that shows
            /// when the workstation was unlocked.
            /// </summary>
            LOGON32_LOGON_UNLOCK = 7
        }

        // TODO: check these values
        /// <summary>
        /// The logon provider to use.
        /// </summary>
        internal enum LogonProvider : uint
        {
            /// <summary>
            /// Use the standard logon provider for the system. The default security provider is negotiate, unless
            /// you pass NULL for the domain name and the user name is not in UPN format. In this case, the default
            /// provider is NTLM.
            /// </summary>
            LOGON32_PROVIDER_DEFAULT = 0,

            /// <summary>
            /// Use the negotiate logon provider.
            /// </summary>
            LOGON32_PROVIDER_WINNT50 = 1,

            /// <summary>
            /// Use the NTLM logon provider.
            /// </summary>
            LOGON32_PROVIDER_WINNT40 = 2
        }
    }
}
