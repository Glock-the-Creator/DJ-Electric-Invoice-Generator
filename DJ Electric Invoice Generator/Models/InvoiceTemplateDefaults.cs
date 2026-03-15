namespace DJ_Electric_Invoice_Generator.Models;

public sealed class InvoiceTemplateDefaults
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyTagline { get; init; } = string.Empty;
    public string CompanyAddress { get; init; } = string.Empty;
    public string CompanyPhone { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string InvoiceDate { get; init; } = string.Empty;
    public string BidNumber { get; init; } = string.Empty;
    public string PaymentNote { get; init; } = string.Empty;
    public string ContactLabel { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string StatusMessage { get; init; } = string.Empty;

    public bool HasValues =>
        !string.IsNullOrWhiteSpace(CompanyName) ||
        !string.IsNullOrWhiteSpace(CompanyTagline) ||
        !string.IsNullOrWhiteSpace(CompanyAddress) ||
        !string.IsNullOrWhiteSpace(CompanyPhone) ||
        !string.IsNullOrWhiteSpace(CompanyEmail) ||
        !string.IsNullOrWhiteSpace(InvoiceDate) ||
        !string.IsNullOrWhiteSpace(BidNumber) ||
        !string.IsNullOrWhiteSpace(PaymentNote) ||
        !string.IsNullOrWhiteSpace(ContactLabel) ||
        !string.IsNullOrWhiteSpace(ContactPhone);
}
