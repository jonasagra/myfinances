using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MyFinances.Core.Labor;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace MyFinances.Data.Services;

public static class PayslipReader
{
	public static Payslip Read(string path)
	{
		try
		{
			using PdfDocument doc = PdfDocument.Open(path);
			return Payslip.Parse(doc.GetPages().SelectMany(TextLines).ToList());
		}
		catch (FormatException ex)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>(ex.Message));
		}
		catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException || ex2 is InvalidOperationException || ex2 is PdfDocumentFormatException) ? true : false)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Não consegui ler esse PDF: " + ex2.Message));
		}
	}

	private static IEnumerable<string> TextLines(Page page)
	{
		List<(double, List<Word>)> rows = new List<(double, List<Word>)>();
		foreach (Word w in from word in page.GetWords()
			orderby word.BoundingBox.Bottom descending
			select word)
		{
			(double, List<Word>) row = rows.FirstOrDefault(((double Y, List<Word> Words) r) => Math.Abs(r.Y - w.BoundingBox.Bottom) < 3.0);
			if (row.Item2 == null)
			{
				double bottom = w.BoundingBox.Bottom;
				int num = 1;
				List<Word> list = new List<Word>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<Word> span = CollectionsMarshal.AsSpan(list);
				int index = 0;
				span[index] = w;
				rows.Add((bottom, list));
			}
			else
			{
				row.Item2.Add(w);
			}
		}
		return rows.OrderByDescending(((double Y, List<Word> Words) r) => r.Y).Select(((double Y, List<Word> Words) r) => string.Join(" ", from word in r.Words
			orderby word.BoundingBox.Left
			select word.Text));
	}
}
