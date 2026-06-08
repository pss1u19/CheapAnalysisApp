using CheapAnalysis.Domain.ValueObjects;

namespace CheapAnalysis.UnitTests.ValueObjects;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("BGN")]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("JPY")]
    [InlineData("BHD")]
    public void FromCode_accepts_active_iso_4217_codes(string code)
    {
        var currency = Currency.FromCode(code);

        currency.Code.Should().Be(code);
    }

    [Theory]
    [InlineData("eur", "EUR")]
    [InlineData("  usd  ", "USD")]
    [InlineData("bGn", "BGN")]
    public void FromCode_trims_and_upper_cases_the_input(string input, string expected)
    {
        Currency.FromCode(input).Code.Should().Be(expected);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("ZZZ")]
    [InlineData("123")]
    [InlineData("XXX")] // ISO "no currency" — intentionally rejected
    [InlineData("")]
    [InlineData("   ")]
    public void FromCode_rejects_invalid_or_unknown_codes(string code)
    {
        var act = () => Currency.FromCode(code);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFromCode_returns_false_for_null()
    {
        Currency.TryFromCode(null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryFromCode_returns_true_and_outputs_currency_for_valid_code()
    {
        Currency.TryFromCode("eur", out var currency).Should().BeTrue();
        currency.Code.Should().Be("EUR");
    }

    [Theory]
    [InlineData("EUR", 2)]
    [InlineData("BGN", 2)]
    [InlineData("JPY", 0)]
    [InlineData("KRW", 0)]
    [InlineData("BHD", 3)]
    [InlineData("CLF", 4)]
    public void DecimalPlaces_reflects_iso_4217_minor_units(string code, int expectedDecimalPlaces)
    {
        Currency.FromCode(code).DecimalPlaces.Should().Be(expectedDecimalPlaces);
    }

    [Fact]
    public void Currencies_with_the_same_code_are_equal()
    {
        Currency.FromCode("EUR").Should().Be(Currency.FromCode("eur"));
        (Currency.FromCode("USD") == Currency.FromCode("USD")).Should().BeTrue();
        Currency.FromCode("EUR").Should().NotBe(Currency.FromCode("USD"));
    }

    [Fact]
    public void ToString_returns_the_code()
    {
        Currency.FromCode("BGN").ToString().Should().Be("BGN");
    }
}
