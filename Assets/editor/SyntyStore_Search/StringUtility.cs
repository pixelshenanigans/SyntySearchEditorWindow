using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PixelShenanigans.FileUtilities
{
    public static class StringUtility
	{
        public static string[] SplitCamelCase(this string str)
        {
            string splitCamelCase = Regex.Replace(str, @"(?<!(^|[A-Z]))(?=[A-Z])|(?<!^)(?=[A-Z][a-z])", "$1_");

            string removeNumbers = Regex.Replace(splitCamelCase, @"[\d-]", string.Empty);

            string removeUnderscores = removeNumbers.Replace("__", "_");

            return removeUnderscores.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}