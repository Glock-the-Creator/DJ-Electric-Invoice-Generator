using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using DJ_Electric_Invoice_Generator.Models;

namespace DJ_Electric_Invoice_Generator.ViewModels;

public sealed class InvoiceViewModel : INotifyPropertyChanged
{
    private string companyName = "DJ ELECTRIC HOME SERVICES";
    private string companyTagline = "Licensed and Insured";
    private string companyAddress = "3721 Kawkawlin River Dr.\nBay City, MI 48706";
    private string companyPhone = "989-239-9040";
    private string companyEmail = string.Empty;
    private string invoiceDate = DateTime.Today.ToString("MM/dd/yy", CultureInfo.CurrentCulture);
    private string bidNumber = string.Empty;
    private string billTo = string.Empty;
    private string balanceText = string.Empty;
    private string addendumNotes = string.Empty;
    private string addendumAmountText = string.Empty;
    private string paymentNote = "Make all checks payable to DJ Electric";
    private string contactLabel = "If you have any questions concerning this invoice, contact:";
    private string contactPhone = "989-239-9040";
    private string statusMessage = @"Ready to download PDF invoices to Documents\DJ Electric Invoices.";
    private string templateStatus = "Template not loaded.";
    private string lastSavedDocumentPath = string.Empty;

    private decimal chargesTotal;
    private decimal balanceValue;
    private decimal addendumValue;
    private decimal total;
    private bool balanceManuallyEdited;
    private bool updatingBalanceInternally;

    public InvoiceViewModel()
    {
        Charges.CollectionChanged += Charges_CollectionChanged;
        Charges.Add(new ChargeItem());
        RecalculateTotals();
    }

    public ObservableCollection<ChargeItem> Charges { get; } = new();

    public string CompanyName
    {
        get => companyName;
        set
        {
            if (companyName == value)
            {
                return;
            }

            companyName = value;
            OnPropertyChanged(nameof(CompanyName));
        }
    }

    public string CompanyTagline
    {
        get => companyTagline;
        set
        {
            if (companyTagline == value)
            {
                return;
            }

            companyTagline = value;
            OnPropertyChanged(nameof(CompanyTagline));
        }
    }

    public string CompanyAddress
    {
        get => companyAddress;
        set
        {
            if (companyAddress == value)
            {
                return;
            }

            companyAddress = value;
            OnPropertyChanged(nameof(CompanyAddress));
        }
    }

    public string CompanyPhone
    {
        get => companyPhone;
        set
        {
            if (companyPhone == value)
            {
                return;
            }

            companyPhone = value;
            OnPropertyChanged(nameof(CompanyPhone));
        }
    }

    public string CompanyEmail
    {
        get => companyEmail;
        set
        {
            if (companyEmail == value)
            {
                return;
            }

            companyEmail = value;
            OnPropertyChanged(nameof(CompanyEmail));
        }
    }

    public string InvoiceDate
    {
        get => invoiceDate;
        set
        {
            if (invoiceDate == value)
            {
                return;
            }

            invoiceDate = value;
            OnPropertyChanged(nameof(InvoiceDate));
        }
    }

    public string BidNumber
    {
        get => bidNumber;
        set
        {
            if (bidNumber == value)
            {
                return;
            }

            bidNumber = value;
            OnPropertyChanged(nameof(BidNumber));
            OnPropertyChanged(nameof(BidDisplay));
        }
    }

    public string BillTo
    {
        get => billTo;
        set
        {
            if (billTo == value)
            {
                return;
            }

            billTo = value;
            OnPropertyChanged(nameof(BillTo));
        }
    }

    public string BalanceText
    {
        get => balanceText;
        set
        {
            if (balanceText == value)
            {
                return;
            }

            balanceText = value;
            if (!updatingBalanceInternally)
            {
                balanceManuallyEdited = !string.IsNullOrWhiteSpace(value);
            }

            OnPropertyChanged(nameof(BalanceText));
            RecalculateTotals();
        }
    }

    public string AddendumNotes
    {
        get => addendumNotes;
        set
        {
            if (addendumNotes == value)
            {
                return;
            }

            addendumNotes = value;
            OnPropertyChanged(nameof(AddendumNotes));
        }
    }

    public string AddendumAmountText
    {
        get => addendumAmountText;
        set
        {
            if (addendumAmountText == value)
            {
                return;
            }

            addendumAmountText = value;
            OnPropertyChanged(nameof(AddendumAmountText));
            RecalculateTotals();
        }
    }

    public string PaymentNote
    {
        get => paymentNote;
        set
        {
            if (paymentNote == value)
            {
                return;
            }

            paymentNote = value;
            OnPropertyChanged(nameof(PaymentNote));
        }
    }

    public string ContactLabel
    {
        get => contactLabel;
        set
        {
            if (contactLabel == value)
            {
                return;
            }

            contactLabel = value;
            OnPropertyChanged(nameof(ContactLabel));
        }
    }

    public string ContactPhone
    {
        get => contactPhone;
        set
        {
            if (contactPhone == value)
            {
                return;
            }

            contactPhone = value;
            OnPropertyChanged(nameof(ContactPhone));
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        set
        {
            if (statusMessage == value)
            {
                return;
            }

            statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public string TemplateStatus
    {
        get => templateStatus;
        set
        {
            if (templateStatus == value)
            {
                return;
            }

            templateStatus = value;
            OnPropertyChanged(nameof(TemplateStatus));
        }
    }

    public string LastSavedDocumentPath
    {
        get => lastSavedDocumentPath;
        set
        {
            if (lastSavedDocumentPath == value)
            {
                return;
            }

            lastSavedDocumentPath = value;
            OnPropertyChanged(nameof(LastSavedDocumentPath));
        }
    }

    public string ChargesTotalDisplay => chargesTotal.ToString("C", CultureInfo.CurrentCulture);

    public string BidDisplay => FormatCurrencyDisplay(bidNumber);

    public string BalanceDisplay => balanceValue.ToString("C", CultureInfo.CurrentCulture);

    public string AddendumAmountDisplay => addendumValue.ToString("C", CultureInfo.CurrentCulture);

    public string TotalDisplay => total.ToString("C", CultureInfo.CurrentCulture);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Charges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ChargeItem item in e.NewItems)
            {
                item.PropertyChanged += ChargeItem_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ChargeItem item in e.OldItems)
            {
                item.PropertyChanged -= ChargeItem_PropertyChanged;
            }
        }

        RecalculateTotals();
    }

    private void ChargeItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        chargesTotal = 0m;

        foreach (var item in Charges)
        {
            chargesTotal += ParseCurrency(item.AmountText);
        }

        if (!balanceManuallyEdited || string.IsNullOrWhiteSpace(balanceText))
        {
            SetBalanceTextInternal(FormatEditableCurrency(chargesTotal));
        }

        balanceValue = ParseCurrency(balanceText);
        addendumValue = ParseCurrency(addendumAmountText);
        total = balanceValue + addendumValue;

        OnPropertyChanged(nameof(ChargesTotalDisplay));
        OnPropertyChanged(nameof(BalanceDisplay));
        OnPropertyChanged(nameof(AddendumAmountDisplay));
        OnPropertyChanged(nameof(TotalDisplay));
    }

    private static decimal ParseCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value,
            NumberStyles.Currency,
            CultureInfo.CurrentCulture,
            out var amount))
        {
            return amount;
        }

        return 0m;
    }

    private void SetBalanceTextInternal(string value)
    {
        if (balanceText == value)
        {
            return;
        }

        updatingBalanceInternally = true;
        balanceText = value;
        OnPropertyChanged(nameof(BalanceText));
        updatingBalanceInternally = false;
    }

    private static string FormatEditableCurrency(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.CurrentCulture);
    }

    private static string FormatCurrencyDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (decimal.TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount))
        {
            return amount.ToString("C", CultureInfo.CurrentCulture);
        }

        var trimmed = value.Trim();
        var currencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
        return trimmed.StartsWith(currencySymbol, StringComparison.CurrentCulture)
            ? trimmed
            : $"{currencySymbol}{trimmed}";
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ApplyTemplateDefaults(InvoiceTemplateDefaults defaults)
    {
        if (!string.IsNullOrWhiteSpace(defaults.CompanyName))
        {
            CompanyName = defaults.CompanyName;
        }

        if (!string.IsNullOrWhiteSpace(defaults.CompanyTagline))
        {
            CompanyTagline = defaults.CompanyTagline;
        }

        if (!string.IsNullOrWhiteSpace(defaults.CompanyAddress))
        {
            CompanyAddress = defaults.CompanyAddress;
        }

        if (!string.IsNullOrWhiteSpace(defaults.CompanyPhone))
        {
            CompanyPhone = defaults.CompanyPhone;
        }

        if (!string.IsNullOrWhiteSpace(defaults.CompanyEmail))
        {
            CompanyEmail = defaults.CompanyEmail;
        }

        if (!string.IsNullOrWhiteSpace(defaults.PaymentNote))
        {
            PaymentNote = defaults.PaymentNote;
        }

        if (!string.IsNullOrWhiteSpace(defaults.ContactLabel))
        {
            ContactLabel = defaults.ContactLabel;
        }

        if (!string.IsNullOrWhiteSpace(defaults.ContactPhone))
        {
            ContactPhone = defaults.ContactPhone;
        }
    }
}
