using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal sealed class _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Digits_0 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int pos = runtextpos;
				if (pos <= inputSpan.Length - 6)
				{
					ReadOnlySpan<char> span = inputSpan.Slice(pos);
					int i;
					for (i = 0; i < span.Length - 5; i++)
					{
						int indexOfPos = span.Slice(i).IndexOfAnyDigit();
						if (indexOfPos < 0)
						{
							break;
						}
						i += indexOfPos;
						if ((uint)(i + 2) >= (uint)span.Length)
						{
							break;
						}
						char ch;
						if ((((ch = span[i + 1]) < '\u0080') ? (("\0\0\uf000Ͽ\0\0\0\0"[(int)ch >> 4] & (1 << (ch & 0xF))) != 0) : RegexRunner.CharInClass(ch, "\0\u0002\u0001,0\t")) && (((ch = span[i + 2]) < '\u0080') ? (("\0\0\uf000Ͽ\0\0\0\0"[(int)ch >> 4] & (1 << (ch & 0xF))) != 0) : RegexRunner.CharInClass(ch, "\0\u0002\u0001,0\t")))
						{
							runtextpos = pos + i;
							return true;
						}
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int pos = runtextpos;
				int matchStart = pos;
				int charloop_starting_pos = 0;
				int charloop_ending_pos = 0;
				ReadOnlySpan<char> slice = inputSpan.Slice(pos);
				if (slice.IsEmpty || !char.IsDigit(slice[0]))
				{
					return false;
				}
				pos++;
				slice = inputSpan.Slice(pos);
				charloop_starting_pos = pos;
				int iteration;
				for (iteration = 0; (uint)iteration < (uint)slice.Length; iteration++)
				{
					char ch;
					if (!(((ch = slice[iteration]) < '\u0080') ? (("\0\0\uf000Ͽ\0\0\0\0"[(int)ch >> 4] & (1 << (ch & 0xF))) != 0) : RegexRunner.CharInClass(ch, "\0\u0002\u0001,0\t")))
					{
						break;
					}
				}
				if (iteration < 4)
				{
					return false;
				}
				slice = slice.Slice(iteration);
				pos += iteration;
				charloop_ending_pos = pos;
				charloop_starting_pos += 4;
				while (slice.IsEmpty || !char.IsDigit(slice[0]))
				{
					if (_003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (charloop_starting_pos >= charloop_ending_pos)
					{
						return false;
					}
					pos = --charloop_ending_pos;
					slice = inputSpan.Slice(pos);
				}
				Capture(0, matchStart, runtextpos = pos + 1);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Digits_0 Instance = new _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Digits_0();

	private _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Digits_0()
	{
		pattern = "\\d[\\d.\\-/,]{4,}\\d";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
