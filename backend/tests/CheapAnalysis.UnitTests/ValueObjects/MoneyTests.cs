using CheapAnalysis.Domain.ValueObjects;

namespace CheapAnalysis.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    private static readonly Currency Eur = Currency.FromCode("EUR");
    private static readonly Currency Usd = Currency.FromCode("USD");

    [Fact]
    public void Zero_is_zero_in_the_given_currency()
    {
        var zero = Money.Zero(Eur);

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be(Eur);
    }

    [Fact]
    public void Addition_sums_amounts_of_the_same_currency()
    {
        var sum = new Money(10.50m, Eur) + new Money(4.25m, Eur);

        sum.Should().Be(new Money(14.75m, Eur));
    }

    [Fact]
    public void Subtraction_subtracts_amounts_of_the_same_currency()
    {
        var difference = new Money(10m, Eur) - new Money(3.50m, Eur);

        difference.Should().Be(new Money(6.50m, Eur));
    }

    [Fact]
    public void Unary_negation_flips_the_sign_and_keeps_the_currency()
    {
        (-new Money(7.25m, Eur)).Should().Be(new Money(-7.25m, Eur));
    }

    [Fact]
    public void Multiplication_scales_the_amount_from_either_side()
    {
        (new Money(4m, Eur) * 2.5m).Should().Be(new Money(10m, Eur));
        (2.5m * new Money(4m, Eur)).Should().Be(new Money(10m, Eur));
    }

    [Fact]
    public void Division_scales_the_amount_down()
    {
        (new Money(10m, Eur) / 4m).Should().Be(new Money(2.5m, Eur));
    }

    [Fact]
    public void Comparison_operators_order_amounts_of_the_same_currency()
    {
        var smaller = new Money(5m, Eur);
        var larger = new Money(9m, Eur);

        (smaller < larger).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (smaller <= new Money(5m, Eur)).Should().BeTrue();
        (larger >= new Money(9m, Eur)).Should().BeTrue();
        smaller.CompareTo(larger).Should().BeNegative();
    }

    [Fact]
    public void Mixing_currencies_throws_on_arithmetic()
    {
        var add = () => new Money(1m, Eur) + new Money(1m, Usd);
        var subtract = () => new Money(1m, Eur) - new Money(1m, Usd);
        var compare = () => new Money(1m, Eur).CompareTo(new Money(1m, Usd));

        add.Should().Throw<InvalidOperationException>();
        subtract.Should().Throw<InvalidOperationException>();
        compare.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Operating_on_a_default_money_throws()
    {
        var act = () => default(Money) + Money.Zero(Eur);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("EUR", 1.005, 1.00)] // banker's rounding: .005 rounds to even
    [InlineData("EUR", 1.015, 1.02)]
    [InlineData("JPY", 123.45, 123)]
    [InlineData("BHD", 1.23449, 1.234)]
    public void Round_uses_the_currency_minor_unit_precision(string code, decimal amount, decimal expected)
    {
        var rounded = new Money(amount, Currency.FromCode(code)).Round();

        rounded.Amount.Should().Be(expected);
    }

    [Fact]
    public void Equality_requires_both_amount_and_currency_to_match()
    {
        new Money(5m, Eur).Should().Be(new Money(5m, Eur));
        new Money(5m, Eur).Should().NotBe(new Money(5m, Usd));
        new Money(5m, Eur).Should().NotBe(new Money(6m, Eur));
    }

    [Fact]
    public void ToString_includes_the_amount_and_code()
    {
        new Money(12.34m, Eur).ToString().Should().Be("12.34 EUR");
    }
}
