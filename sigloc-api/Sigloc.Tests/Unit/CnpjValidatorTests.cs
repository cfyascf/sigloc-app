using Sigloc.Application.Services;

namespace Sigloc.Tests.Unit;

public class CnpjValidatorTests
{
    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11444777000161")]
    public void Valid_cnpjs_pass(string cnpj) => CnpjValidator.IsValid(cnpj).Should().BeTrue();

    [Theory]
    [InlineData("12345678000100")] // wrong check digits
    [InlineData("11222333000180")] // wrong second check digit
    [InlineData("11111111111111")] // repeated digits
    [InlineData("00000000000000")]
    [InlineData("1122233300018")]  // 13 digits
    [InlineData("11.222.333/0001-81")] // masked: callers pass digits only
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_cnpjs_fail(string? cnpj) => CnpjValidator.IsValid(cnpj).Should().BeFalse();
}
