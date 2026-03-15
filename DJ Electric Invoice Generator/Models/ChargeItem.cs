using System.ComponentModel;
using System.Globalization;

namespace DJ_Electric_Invoice_Generator.Models;

public sealed class ChargeItem : INotifyPropertyChanged
{
    private string description = string.Empty;
    private string amountText = string.Empty;

    public string Description
    {
        get => description;
        set
        {
            if (description == value)
            {
                return;
            }

            description = value;
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public string AmountText
    {
        get => amountText;
        set
        {
            if (amountText == value)
            {
                return;
            }

            amountText = value;
            OnPropertyChanged(nameof(AmountText));
            OnPropertyChanged(nameof(AmountDisplay));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public string AmountDisplay => FormatAmount(amountText);

    public bool IsEmpty => string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(amountText);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatAmount(string? value)
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
        return trimmed.StartsWith("$", StringComparison.Ordinal) ? trimmed : $"${trimmed}";
    }
}
