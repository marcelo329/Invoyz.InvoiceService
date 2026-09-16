namespace Invoyz.InvoiceService.Application.CQRS.Invoices;

public static class InvoiceStatuses
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";

    private static readonly string[] All = [Draft, Sent, Paid, Overdue];

    public static string Allowed => string.Join(", ", All);

    public static bool IsAllowed(string? status)
        => All.Any(allowed => allowed.Equals(status, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the canonically cased value, so storage never varies by client casing.</summary>
    public static string Normalise(string status)
        => All.First(allowed => allowed.Equals(status, StringComparison.OrdinalIgnoreCase));
}
