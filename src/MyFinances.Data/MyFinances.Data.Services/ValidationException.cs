using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinances.Data.Services;

public sealed class ValidationException(IEnumerable<string> errors) : Exception(string.Join(Environment.NewLine, errors))
{
	public IReadOnlyList<string> Errors { get; } = errors.ToList();
}
