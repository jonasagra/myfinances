using System;

namespace MyFinances.App.ViewModels;

public sealed record AttachmentRow(Guid Id, string Kind, string Name, string Size, DateTime Data, bool Replaced);
