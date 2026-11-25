namespace Kluis.Core.Accounts;

/// <summary>
/// A bank account whose statements are imported. v1 shows a single EUR account,
/// but the table exists from the first migration because adding it later would be a migration.
/// </summary>
public sealed class Account
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>ISO 4217 currency code. v1 supports EUR only.</summary>
    public string Currency { get; set; } = "EUR";
}
