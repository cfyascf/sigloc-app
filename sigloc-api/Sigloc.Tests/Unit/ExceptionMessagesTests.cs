using Sigloc.Application.Exceptions;

namespace Sigloc.Tests.Unit;

// User-facing exception messages reach the UI unchanged, so they must be in Portuguese and
// must not leak raw identifiers.
public class ExceptionMessagesTests
{
    [Fact]
    public void Not_found_messages_are_portuguese_without_ids()
    {
        var id = Guid.NewGuid();

        new TripNotFoundException(id).Message.Should().Be("Viagem não encontrada ou não pertence à sua empresa.");
        new TripStopNotFoundException(id).Message.Should().NotContain(id.ToString());
        new ProofOfDeliveryNotFoundException(id).Message.Should().NotContain(id.ToString());
    }

    [Theory]
    [InlineData("31953385000135", "O CNPJ 31.953.385/0001-35 já está em uso.")]
    [InlineData("123", "O CNPJ 123 já está em uso.")]
    public void Duplicate_cnpj_message_shows_formatted_cnpj(string cnpj, string expected)
        => new CnpjAlreadyExistsException(cnpj).Message.Should().Be(expected);
}
