using System;

namespace ServiceHelperUnitTests
{
    internal static class TestConstants
    {
        // TODO: right now this number is just a number that I felt was short enough to keep tests going while
        // being long enough to be reliabliy measurable; consider researching a minimum timeout that isn't just
        // based on gut feeling
        /// <summary>
        /// Tests that need to wait should do so at least this long.
        /// </summary>
        public const int MinWaitTimeMilliseconds = 500;

        public const int MaxWaitTimeMilliseconds = 5 * 1000;

        /// <summary>
        /// Tests that need to wait should do so at least this long.
        /// </summary>
        public static readonly TimeSpan MinWaitTime = TimeSpan.FromMilliseconds(TestConstants.MinWaitTimeMilliseconds);

        /// <summary>
        /// No test should ever wait for more than this amount of time without failing.
        /// </summary>
        public static readonly TimeSpan MaxWaitTime = TimeSpan.FromMilliseconds(TestConstants.MaxWaitTimeMilliseconds);
    }

    /// <summary>
    /// Provides a way to pass results across threads. This is required because assert calls on threads
    /// other than the main test thread will crash the test.
    /// </summary>
    /// <typeparam name="T">The type of the result to be held by this object.</typeparam>
    internal sealed class ThreadResult<T>
    {
        public T Result;
    }

    /// <summary>
    /// Provides a unique exception type that can be thrown in tests.
    /// </summary>
    internal sealed class TestException : Exception
    {
    }
}
