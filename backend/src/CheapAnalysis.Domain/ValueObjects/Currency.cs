namespace CheapAnalysis.Domain.ValueObjects;

/// <summary>
/// An ISO 4217 currency, identified by its three-letter alphabetic code
/// (e.g. <c>BGN</c>, <c>EUR</c>, <c>USD</c>). Validated against the active
/// ISO 4217 code list on construction, so an instance is always a real currency.
/// </summary>
public sealed record Currency
{
    private Currency(string code, int decimalPlaces)
    {
        Code = code;
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>The upper-cased three-letter ISO 4217 alphabetic code.</summary>
    public string Code { get; }

    /// <summary>
    /// The number of minor-unit digits the currency is subdivided into
    /// (2 for most currencies, 0 for e.g. JPY, 3 for e.g. BHD).
    /// </summary>
    public int DecimalPlaces { get; }

    /// <summary>
    /// Parses an ISO 4217 alphabetic code into a <see cref="Currency"/>.
    /// Surrounding whitespace is trimmed and the code is upper-cased before validation.
    /// </summary>
    /// <param name="code">A three-letter ISO 4217 alphabetic code.</param>
    /// <returns>The matching <see cref="Currency"/>.</returns>
    /// <exception cref="ArgumentException">The code is not a recognised active ISO 4217 currency.</exception>
    public static Currency FromCode(string code)
    {
        if (!TryFromCode(code, out var currency))
        {
            throw new ArgumentException($"'{code}' is not a valid ISO 4217 currency code.", nameof(code));
        }

        return currency;
    }

    /// <summary>
    /// Attempts to parse an ISO 4217 alphabetic code into a <see cref="Currency"/>
    /// without throwing on failure.
    /// </summary>
    /// <param name="code">A three-letter ISO 4217 alphabetic code; may be null.</param>
    /// <param name="currency">The parsed currency when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the code is a recognised active ISO 4217 currency.</returns>
    public static bool TryFromCode(string? code, out Currency currency)
    {
        currency = null!;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!MinorUnits.TryGetValue(normalized, out var decimalPlaces))
        {
            return false;
        }

        currency = new Currency(normalized, decimalPlaces);
        return true;
    }

    /// <summary>Returns the ISO 4217 code, e.g. <c>"EUR"</c>.</summary>
    public override string ToString() => Code;

    // Active ISO 4217 alphabetic codes mapped to their minor-unit digit count.
    // Most currencies use 2; the rest are listed explicitly above the default block.
    // The settlement/no-currency codes (XXX, XTS) are intentionally excluded so a
    // Money value can never carry "no currency". Precious metals and XDR have no
    // defined minor unit in ISO 4217 and are modelled here as 0.
    private static readonly Dictionary<string, int> MinorUnits = BuildMinorUnits();

    private static Dictionary<string, int> BuildMinorUnits()
    {
        var zeroDecimal = new[]
        {
            "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF",
            "UGX", "UYI", "VND", "VUV", "XAF", "XOF", "XPF",
            "XAU", "XAG", "XPT", "XPD", "XDR",
        };

        var threeDecimal = new[] { "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND" };

        var fourDecimal = new[] { "CLF", "UYW" };

        var twoDecimal = new[]
        {
            "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN",
            "BAM", "BBD", "BDT", "BGN", "BMD", "BND", "BOB", "BRL", "BSD", "BTN",
            "BWP", "BYN", "BZD", "CAD", "CDF", "CHF", "CNY", "COP", "CRC", "CUP",
            "CVE", "CZK", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD",
            "FKP", "GBP", "GEL", "GHS", "GIP", "GMD", "GTQ", "GYD", "HKD", "HNL",
            "HTG", "HUF", "IDR", "ILS", "INR", "IRR", "JMD", "KES", "KGS", "KHR",
            "KPW", "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "MAD", "MDL",
            "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN",
            "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "PAB", "PEN",
            "PGK", "PHP", "PKR", "PLN", "QAR", "RON", "RSD", "RUB", "SAR", "SBD",
            "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SOS", "SRD", "SSP", "STN",
            "SVC", "SYP", "SZL", "THB", "TJS", "TMT", "TOP", "TRY", "TTD", "TWD",
            "TZS", "UAH", "USD", "UYU", "UZS", "VED", "VES", "WST", "YER", "ZAR",
            "ZMW", "ZWG",
        };

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var code in twoDecimal)
        {
            map[code] = 2;
        }

        foreach (var code in zeroDecimal)
        {
            map[code] = 0;
        }

        foreach (var code in threeDecimal)
        {
            map[code] = 3;
        }

        foreach (var code in fourDecimal)
        {
            map[code] = 4;
        }

        return map;
    }
}
