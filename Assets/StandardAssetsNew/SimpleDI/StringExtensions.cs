using System;
using System.Globalization;

public static class StringExtensions {
	public static string Truncate(this string str, int length, bool ellipsis = false) {
		if(str.Length <= length)
			return str;

		return str.Substring(0, length) + (ellipsis ? "..." : String.Empty);
	}

	public static bool ContainsIgnoreCase(this string str, string arg) {
		return CultureInfo.InvariantCulture.CompareInfo.IndexOf(str, arg, CompareOptions.IgnoreCase) >= 0;
	}

	public static string ToStringInvariantCulture(this DateTime dateTime) {
		return dateTime.ToString(CultureInfo.InvariantCulture);
	}

	public static string ToStringInvariantCulture(this DateTime dateTime, string format) {
		return dateTime.ToString(format, CultureInfo.InvariantCulture);
	}

	public static string ToStringInvariantCulture(this DateTimeOffset dateTimeOffset, string format) {
		return dateTimeOffset.ToString(format, CultureInfo.InvariantCulture);
	}

	// HEAVILY adapted from https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity5.html since their
	// code was wrong :(
	public static bool EndsWithFast(this string a, string b) {
		int ap = a.Length - 1;
		int bp = b.Length - 1;

		while(ap >= 0 && bp >= 0 && a[ap] == b[bp]) {
			ap--;
			bp--;
		}

		return (bp < 0);
	}

	public static bool StartsWithFast(this string a, string b) {
		int aLen = a.Length;
		int bLen = b.Length;

		int ap = 0;
		int bp = 0;

		while(ap < aLen && bp < bLen && a[ap] == b[bp]) {
			ap++;
			bp++;
		}

		return (bp == bLen);
	}

	/// <summary>
	/// Adds quotes (") around the string, e.g. to handle file names with spaces
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string Quoted(this string str) {
		return "\"" + str + "\"";
	}

	// Extension methods look like methods, but are not. Calling this on a null string is fine.
	public static bool IsNullOrEmpty(this string str) {
		return string.IsNullOrEmpty(str);
	}
}
