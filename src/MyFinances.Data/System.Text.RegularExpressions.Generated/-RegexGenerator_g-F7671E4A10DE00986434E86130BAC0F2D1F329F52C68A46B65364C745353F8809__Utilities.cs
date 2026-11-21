using System.Buffers;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal static class _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeout) ? timeout : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	internal static readonly SearchValues<char> s_asciiExceptDigits = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyDigit(this ReadOnlySpan<char> span)
	{
		int i = span.IndexOfAnyExcept(s_asciiExceptDigits);
		if ((uint)i < (uint)span.Length)
		{
			if (char.IsAscii(span[i]))
			{
				return i;
			}
			do
			{
				if (char.IsDigit(span[i]))
				{
					return i;
				}
				i++;
			}
			while ((uint)i < (uint)span.Length);
		}
		return -1;
	}
}
