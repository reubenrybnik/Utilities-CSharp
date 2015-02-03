using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
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
        /// error information, call <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        /// <summary>
        /// Attaches the calling process to the console of the specified process.
        /// </summary>
        /// <param name="dwProcessId">The identifier of the process whose console is to be used or
        /// <see cref="NativeMethods.ATTACH_PARENT_PROCESS" /> to attach to the console of the parent
        /// process.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(UInt32 dwProcessId);

        /// <summary>
        /// Parses a Unicode command line string and returns an array of pointers to the command line arguments,
        /// along with a count of such arguments, in a way that is similar to the standard C run-time argv and
        /// argc values.
        /// </summary>
        /// <param name="lpCmdLine">A string that contains the full command line. If this parameter is an empty string
        /// the function returns the path to the current executable file.</param>
        /// <param name="pNumArgs">An int that receives the number of array elements returned, similar to argc.</param>
        /// <returns>an array of LPWSTR values, similar to argv. If the function fails, the value of
        /// <see cref="SafeLocalAllocWStrArray.IsInvalid" /> will be <c>true</c>. To get extended error information, call
        /// <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeLocalAllocWStrArray CommandLineToArgvW(string lpCmdLine, out int pNumArgs);

        /// <summary>
        /// Frees the specified local memory object and invalidates its handle.
        /// </summary>
        /// <param name="hLocal">A handle to the local memory object. This handle is returned by either the
        /// LocalAlloc or LocalReAlloc function. It is not safe to free memory allocated with GlobalAlloc.</param>
        /// <returns>If the function succeeds, the return value is <see cref="IntPtr.Zero" />. If the function fails,
        /// the return value is equal to a handle to the local memory object. To get extended error information,
        /// call <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr hLocal);

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
        /// error information, call <see cref="Marshal.GetLastWin32Error" />.</returns>
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
        /// <see cref="Marshal.SecureStringToGlobalAllocUnicode" />. Once user logon is complete, clear the string
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
        /// error information, call <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, IntPtr lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out SafeTokenHandle phToken);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
        /// error information, call <see cref="Marshal.GetLastWin32Error" />.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// A wrapper around a pointer to an array of Unicode strings (LPWSTR*) using a contiguous block of
        /// memory that can be freed by a single call to LocalFree.
        /// </summary>
        public sealed class SafeLocalAllocWStrArray : SafeLocalAllocArray<string>
        {
            /// <summary>
            /// Creates a new SafeLocalAllocWStrArray. This should only be done by P/Invoke.
            /// </summary>
            private SafeLocalAllocWStrArray()
                : base(true)
            {
            }

            /// <summary>
            /// Creates a new SafeLocalallocWStrArray to wrap the specified array.
            /// </summary>
            /// <param name="handle">The pointer to the unmanaged array to wrap.</param>
            /// <param name="ownHandle"><c>true</c> to release the array when this object
            /// is disposed or finalized, <c>false</c> otherwise.</param>
            public SafeLocalAllocWStrArray(IntPtr handle, bool ownHandle)
                : base(ownHandle)
            {
                this.SetHandle(handle);
            }

            /// <summary>
            /// Returns the Unicode string referred to by an unmanaged pointer in the wrapped array.
            /// </summary>
            /// <param name="index">The index of the value to retrieve.</param>
            /// <returns>the value at the position specified by <paramref name="index" /> as a string.</returns>
            protected override string GetArrayValue(int index)
            {
                return Marshal.PtrToStringUni(Marshal.ReadIntPtr(this.handle + IntPtr.Size * index));
            }
        }

        // This class is similar to the built-in SafeBuffer class. Major differences are:
        // 1. This class is less safe because it does not implicitly know the length of the array it wraps.
        // 2. The array is read-only.
        // 3. The type parameter is not limited to value types.
        /// <summary>
        /// Wraps a pointer to a contiguous unmanaged array of objects that can be freed by a single call to
        /// LocalFree on the wrapped pointer.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the array.</typeparam>
        public abstract class SafeLocalAllocArray<T> : SafeHandleZeroOrMinusOneIsInvalid
        {
            /// <summary>
            /// Creates a new SafeLocalArray which specifies that the array should be freed when this
            /// object is disposed or finalized.
            /// <param name="ownsHandle"><c>true</c> to reliably release the handle during the finalization phase;
            /// <c>false</c> to prevent reliable release (not recommended).</param>
            /// </summary>
            protected SafeLocalAllocArray(bool ownsHandle)
                : base(ownsHandle)
            {
            }

            /// <summary>
            /// Converts the unmanaged object referred to by <paramref name="valuePointer" /> to a managed object
            /// of type T.
            /// </summary>
            /// <param name="index">The index of the value to retrieve.</param>
            /// <returns>the value at the position specified by <paramref name="index" /> as a managed object of
            /// type T.</returns>
            protected abstract T GetArrayValue(int index);

            // 
            /// <summary>
            /// Frees the wrapped array by calling LocalFree.
            /// </summary>
            /// <returns><c>true</c> if the call to LocalFree succeeds, <c>false</c> if the call fails.</returns>
            protected override bool ReleaseHandle()
            {
                return (NativeMethods.LocalFree(this.handle) == IntPtr.Zero);
            }

            /// <summary>
            /// Copies the unmanaged array to the specified managed array.
            /// 
            /// It is important that the length of <paramref name="array"/> be less than or equal to the length of
            /// the unmanaged array wrapped by this object. If it is not, at best garbage will be read and at worst
            /// an exception of type <see cref="AccessViolationException" /> will be thrown.
            /// </summary>
            /// <param name="array">The managed array to copy the unmanaged values to.</param>
            /// <exception cref="ObjectDisposedException">The unmanaged array wrapped by this object has been
            /// freed.</exception>
            /// <exception cref="InvalidOperationException">The pointer to the unmanaged array wrapped by this object
            /// is invalid.</exception>
            /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
            public void CopyTo(T[] array)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }

                this.CopyTo(array, 0, array.Length);
            }

            /// <summary>
            /// Copies the unmanaged array to the specified managed array.
            /// 
            /// It is important that <paramref name="length" /> be less than or equal to the length of
            /// the array wrapped by this object. If it is not, at best garbage will be read and at worst
            /// an exception of type <see cref="AccessViolationException" /> will be thrown.
            /// </summary>
            /// <param name="array">The managed array to copy the unmanaged values to.</param>
            /// <param name="index">The index to start at when copying to <paramref name="array" />.</param>
            /// <param name="length">The number of items to copy to <paramref name="array" /></param>
            /// <exception cref="ObjectDisposedException">The unmanaged array wrapped by this object has been
            /// freed.</exception>
            /// <exception cref="InvalidOperationException">The pointer to the unmanaged array wrapped by this object
            /// is invalid.</exception>
            /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.-or- 
            /// <paramref name="index" /> is greater than the length of <paramref name="array"/>.-or-
            /// <paramref name="length"/> is less than zero.</exception>
            /// <exception cref="ArgumentException">The sum of <paramref name="index" /> and <paramref name="length" />
            /// is greater than the length of <paramref name="array" />.</exception>
            public void CopyTo(T[] array, int index, int length)
            {
                if (this.IsClosed)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
                if (this.IsInvalid)
                {
                    throw new InvalidOperationException("This object's buffer is invalid.");
                }
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if (index < 0 || array.Length < index)
                {
                    throw new ArgumentOutOfRangeException("index", "index must be a nonnegative integer that is less than array's length.");
                }
                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException("length", "length must be a nonnegative integer.");
                }
                if (array.Length < index + length)
                {
                    throw new ArgumentException("length", "length is greater than the number of elements from index to the end of array.");
                }

                for (int i = 0; i < length; ++i)
                {
                    array[index + i] = this.GetArrayValue(i);
                }
            }
        }

        /// <summary>
        /// Wraps a handle to a user token.
        /// </summary>
        public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            /// <summary>
            /// Creates a new SafeTokenHandle. This should only be done by P/Invoke.
            /// </summary>
            private SafeTokenHandle()
                : base(true)
            {
            }

            /// <summary>
            /// Creates a new SafeTokenHandle to wrap the specified user token.
            /// </summary>
            /// <param name="arrayPointer">The user token to wrap.</param>
            /// <param name="ownHandle"><c>true</c> to close the token when this object is disposed or finalized,
            /// <c>false</c> otherwise.</param>
            public SafeTokenHandle(IntPtr handle, bool ownHandle)
                : base(ownHandle)
            {
                this.SetHandle(handle);
            }

            /// <summary>
            /// Provides a <see cref="WindowsIdentity" /> object created from this user token. Depending
            /// on the type of token, this can be used to impersonate the user. The WindowsIdentity
            /// class will duplicate the token, so it is safe to use the WindowsIdentity object created by
            /// this method after disposing this object.
            /// </summary>
            /// <returns>a <see cref="WindowsIdentity" /> for the user that this token represents.</returns>
            /// <exception cref="InvalidOperationException">This object does not contain a valid handle.</exception>
            /// <exception cref="ObjectDisposedException">This object has been disposed and its token has
            /// been released.</exception>
            public WindowsIdentity GetWindowsIdentity()
            {
                if (this.IsClosed)
                {
                    throw new ObjectDisposedException("The user token has been released.");
                }
                if (this.IsInvalid)
                {
                    throw new InvalidOperationException("The user token is invalid.");
                }

                return new WindowsIdentity(this.handle);
            }

            /// <summary>
            /// Calls <see cref="NativeMethods.CloseHandle" /> to release this user token.
            /// </summary>
            /// <returns><c>true</c> if the function succeeds, <c>false otherwise</c>. To get extended
            /// error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(this.handle);
            }
        }

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
