namespace CheapAnalysis.Domain.ValueObjects;

/// <summary>
/// A monetary amount in a specific <see cref="ValueObjects.Currency"/>.
/// Arithmetic between two <see cref="Money"/> values is only defined when their
/// currencies match; mixing currencies throws rather than silently producing a
/// meaningless result.
/// </summary>
/// <param name="Amount">The signed amount, e.g. <c>-12.34m</c>.</param>
/// <param name="Currency">The currency the amount is denominated in.</param>
public readonly record struct Money(decimal Amount, Currency Currency) : IComparable<Money>
{
    /// <summary>A zero amount in the given currency.</summary>
    /// <param name="currency">The currency of the zero amount.</param>
    public static Money Zero(Currency currency) => new(decimal.Zero, currency);

    /// <summary>Adds two amounts of the same currency.</summary>
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left with { Amount = left.Amount + right.Amount };
    }

    /// <summary>Subtracts the right amount from the left, in the same currency.</summary>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left with { Amount = left.Amount - right.Amount };
    }

    /// <summary>Negates the amount, keeping the currency.</summary>
    public static Money operator -(Money value) => value with { Amount = -value.Amount };

    /// <summary>Scales the amount by a unitless factor.</summary>
    public static Money operator *(Money money, decimal factor) => money with { Amount = money.Amount * factor };

    /// <summary>Scales the amount by a unitless factor.</summary>
    public static Money operator *(decimal factor, Money money) => money * factor;

    /// <summary>Divides the amount by a unitless divisor.</summary>
    public static Money operator /(Money money, decimal divisor) => money with { Amount = money.Amount / divisor };

    /// <summary>Compares two amounts of the same currency.</summary>
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    /// <summary>Compares two amounts of the same currency.</summary>
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    /// <summary>Compares two amounts of the same currency.</summary>
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    /// <summary>Compares two amounts of the same currency.</summary>
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Orders this amount relative to another of the same currency.
    /// </summary>
    /// <param name="other">The amount to compare against.</param>
    /// <returns>A signed value: negative if this is smaller, zero if equal, positive if larger.</returns>
    /// <exception cref="InvalidOperationException">The two amounts are in different currencies.</exception>
    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Rounds the amount to the currency's ISO 4217 minor-unit precision
    /// (banker's rounding), e.g. 2 digits for EUR, 0 for JPY.
    /// </summary>
    /// <returns>A new <see cref="Money"/> with the rounded amount and the same currency.</returns>
    public Money Round() => this with { Amount = Math.Round(Amount, Currency.DecimalPlaces, MidpointRounding.ToEven) };

    /// <summary>Returns the amount and code, e.g. <c>"12.34 EUR"</c>.</summary>
    public override string ToString() => $"{Amount} {Currency.Code}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency is null || right.Currency is null)
        {
            throw new InvalidOperationException("Cannot operate on a default (uninitialised) Money value.");
        }

        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on Money values in different currencies ('{left.Currency.Code}' and '{right.Currency.Code}').");
        }
    }
}
