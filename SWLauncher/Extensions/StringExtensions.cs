using System;
using System.Text.RegularExpressions;

namespace SWLauncher.Extensions
{
    internal static class StringExtensions
    {
        // Thanks to http://stackoverflow.com/a/8082171
        public static string Between(this string src, string findFrom, string findTo)
        {
            var start = src.IndexOf(findFrom, StringComparison.Ordinal);
            var to = src.IndexOf(findTo, start + findFrom.Length, StringComparison.Ordinal);

            if (start < 0 || to < 0) return "";

            return src.Substring(start + findFrom.Length, to - start - findFrom.Length);
        }

        /// <summary>
        ///     Returns the group named "value".
        /// </summary>
        /// <param name="src">The string.</param>
        /// <param name="pattern">The pattern for regex.</param>
        /// <param name="options">The regex options.</param>
        /// <returns>Returns the parsed "value" group.</returns>
        public static string ParseRegex(this string src, string pattern, RegexOptions options = RegexOptions.None)
        {
            var match = Regex.Match(src, pattern, options);
            if(!match.Success)
                throw new Exception($"Couldn't find pattern: {pattern}");

            return match.Groups["value"].Value;
        }
    }
}
