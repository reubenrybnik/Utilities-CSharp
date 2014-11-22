using System;
using System.Collections.Specialized;

namespace ServiceHelper
{
    internal static class NameValueCollectionExtensions
    {
        public static bool Contains(this NameValueCollection collection, string name)
        {
            return (collection.GetValues(name) != null);
        }
    }
}
