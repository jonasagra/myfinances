using System;

namespace MyFinances.Data;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EncryptedAttribute : Attribute
{
}
