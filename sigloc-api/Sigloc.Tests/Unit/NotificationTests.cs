using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class NotificationTests
{
    private readonly INotificationPublisher _publisher = Substitute.For<INotificationPublisher>();
    private readonly IPartnerConnectionRepository _connections = Substitute.For<IPartnerConnectionRepository>();
    private readonly IContractorRepository _contractors = Substitute.For<IContractorRepository>();
    private readonly ICarrierRepository _carriers = Substitute.For<ICarrierRepository>();
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    private AuctionNotifier Notifier() => new(_publisher, _connections, _contractors, _carriers);

    private IReadOnlyCollection<NotificationMessage> Published()
    {
        var call = _publisher.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(INotificationPublisher.PublishAsync));
        return (IReadOnlyCollection<NotificationMessage>)call.GetArguments()[0]!;
    }

    // ---------------------------------------------------------------------
    // AuctionNotifier
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Auction_opened_reaches_only_the_contractors_active_partners()
    {
        var contractor = TestData.Contractor(companyName: "Embarcador SA");
        contractor.TradeName = "Embarcador";
        var active = Guid.NewGuid();
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _connections.GetByContractorAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new PartnerConnection { Id = Guid.NewGuid(), ContractorId = contractor.Id, CarrierId = active, Status = PartnershipStatus.Active },
            new PartnerConnection { Id = Guid.NewGuid(), ContractorId = contractor.Id, CarrierId = Guid.NewGuid(), Status = PartnershipStatus.Rejected },
            new PartnerConnection { Id = Guid.NewGuid(), ContractorId = contractor.Id, CarrierId = Guid.NewGuid(), Status = PartnershipStatus.Pending },
        });
        var auctionId = Guid.NewGuid();

        await Notifier().AuctionOpenedAsync(auctionId, contractor.Id, "Curitiba, PR → Joinville, SC");

        var message = Published().Should().ContainSingle().Subject;
        message.RecipientCompanyId.Should().Be(active);
        message.Type.Should().Be(NotificationCatalog.AuctionOpenedType);
        message.Message.Should().Be("Embarcador abriu um leilão para Curitiba, PR → Joinville, SC.");
        message.Link.Should().Be($"/bid-analysis/{auctionId}");
    }

    [Fact]
    public async Task Bid_placed_tells_the_contractor_and_the_outbid_carrier()
    {
        var carrier = TestData.Carrier(companyName: "Transportes Ltda");
        _carriers.GetByIdAsync(carrier.Id, Arg.Any<CancellationToken>()).Returns(carrier);
        var contractorId = Guid.NewGuid();
        var outbid = Guid.NewGuid();
        var auctionId = Guid.NewGuid();

        await Notifier().BidPlacedAsync(auctionId, contractorId, carrier.Id, "A → B", 1234.5m, updated: false, outbid);

        var messages = Published().ToList();
        messages.Select(m => (m.RecipientCompanyId, m.Type)).Should().Equal(
            (contractorId, NotificationCatalog.BidReceivedType), (outbid, NotificationCatalog.BidOutbidType));
        messages[0].Title.Should().Be("Novo lance recebido");
        // ICU separates "R$" with a no-break space, NLS with a plain one.
        messages[0].Message.Replace('\u00A0', ' ').Should().Be("Transportes Ltda ofereceu R$ 1.234,50 no leilão A → B.");
        messages[0].Link.Should().Be($"/auction-bids/{auctionId}");
        messages[1].Link.Should().Be($"/bid-analysis/{auctionId}");
    }

    [Fact]
    public async Task Updated_bid_without_outbid_only_tells_the_contractor()
    {
        _carriers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Carrier?)null);

        await Notifier().BidPlacedAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "A → B", 900m, updated: true, null);

        var message = Published().Should().ContainSingle().Subject;
        message.Title.Should().Be("Lance atualizado");
        message.Message.Should().StartWith("Uma transportadora atualizou o lance para");
    }

    [Fact]
    public async Task Award_tells_the_winner_its_trip_and_the_other_bidders_the_auction_closed()
    {
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        await Notifier().AuctionAwardedAsync(tripId, winner, new[] { loser, loser, winner }, "A → B");

        var messages = Published().ToList();
        messages.Select(m => (m.RecipientCompanyId, m.Type)).Should().Equal(
            (winner, NotificationCatalog.AuctionWonType), (loser, NotificationCatalog.AuctionLostType));
        messages[0].Link.Should().Be($"/carrier-route-tracking/{tripId}");
        messages[0].Message.Should().Contain(TripDetailMapper.Reference(tripId));
    }

    [Fact]
    public async Task Cancellation_tells_every_bidder_once()
    {
        var bidder = Guid.NewGuid();

        await Notifier().AuctionCancelledAsync(new[] { bidder, bidder }, "A → B");

        var message = Published().Should().ContainSingle().Subject;
        (message.RecipientCompanyId, message.Type).Should().Be((bidder, NotificationCatalog.AuctionCancelledType));
    }

    [Fact]
    public async Task A_failing_lookup_is_logged_not_thrown_so_the_auction_operation_still_succeeds()
    {
        _contractors.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns<Contractor?>(_ => throw new InvalidOperationException("db down"));

        var act = () => Notifier().AuctionOpenedAsync(Guid.NewGuid(), Guid.NewGuid(), "A → B");

        await act.Should().NotThrowAsync();
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<IReadOnlyCollection<NotificationMessage>>());
    }

    // ---------------------------------------------------------------------
    // NotificationPublisher / NotificationService
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Publisher_persists_truncated_messages_and_swallows_storage_failures()
    {
        IReadOnlyCollection<Notification>? stored = null;
        _repository.AddRangeAsync(Arg.Do<IReadOnlyCollection<Notification>>(n => stored = n), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var recipient = Guid.NewGuid();

        await new NotificationPublisher(_repository).PublishAsync([new NotificationMessage(recipient, "T", new string('t', 200), new string('m', 600), "/x")]);

        var notification = stored.Should().ContainSingle().Subject;
        notification.RecipientCompanyId.Should().Be(recipient);
        notification.Title.Should().HaveLength(120).And.EndWith("…");
        notification.Message.Should().HaveLength(500);
        notification.ReadAt.Should().BeNull();

        _repository.AddRangeAsync(Arg.Any<IReadOnlyCollection<Notification>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("db down")));
        var act = () => new NotificationPublisher(_repository).PublishAsync([new NotificationMessage(recipient, "T", "t", "m")]);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(null, 20)] [InlineData(0, 1)] [InlineData(500, 50)] [InlineData(5, 5)]
    public async Task Service_lists_the_newest_with_a_clamped_limit_and_the_unread_count(int? limit, int expectedTake)
    {
        var companyId = Guid.NewGuid();
        var unread = new Notification { Id = Guid.NewGuid(), RecipientCompanyId = companyId, Type = "T", Title = "t", Message = "m", CreatedAt = DateTimeOffset.UtcNow };
        _repository.ListRecentAsync(companyId, expectedTake, Arg.Any<CancellationToken>()).Returns(new[] { unread });
        _repository.CountUnreadAsync(companyId, Arg.Any<CancellationToken>()).Returns(7);

        var result = await new NotificationService(_repository, TimeProvider.System).ListAsync(companyId, limit);

        result.Unread.Should().Be(7);
        result.Items.Should().ContainSingle().Which.Read.Should().BeFalse();
    }

    [Fact]
    public async Task Marking_another_companys_notification_as_read_is_not_found()
    {
        _repository.MarkReadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => new NotificationService(_repository, TimeProvider.System).MarkReadAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotificationNotFoundException>();
    }

    // ---------------------------------------------------------------------
    // Partnership ended
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Ending_a_partnership_tells_the_other_side_who_ended_it(bool endedByContractor)
    {
        var contractor = TestData.Contractor(companyName: "Embarcador SA");
        var carrier = TestData.Carrier(companyName: "Transportes Ltda");
        var connection = new PartnerConnection
        {
            Id = Guid.NewGuid(), ContractorId = contractor.Id, CarrierId = carrier.Id, Status = PartnershipStatus.Active,
            Contractor = contractor, Carrier = carrier
        };
        _connections.GetTrackedByIdAsync(connection.Id, Arg.Any<CancellationToken>()).Returns(connection);
        var service = new PartnerNetworkService(_connections, Substitute.For<IVehicleRepository>(), Substitute.For<ITripRepository>(),
            Substitute.For<IAuctionRepository>(), _publisher);

        await service.RemovePartnershipAsync(endedByContractor ? contractor.Id : carrier.Id, connection.Id);

        var message = Published().Should().ContainSingle().Subject;
        message.Type.Should().Be(NotificationCatalog.PartnershipEndedType);
        message.RecipientCompanyId.Should().Be(endedByContractor ? carrier.Id : contractor.Id);
        message.Message.Should().StartWith(endedByContractor ? "Embarcador SA encerrou" : "Transportes Ltda encerrou");
    }

    [Fact]
    public async Task Ending_an_already_ended_partnership_notifies_nobody()
    {
        var connection = new PartnerConnection { Id = Guid.NewGuid(), ContractorId = Guid.NewGuid(), CarrierId = Guid.NewGuid(), Status = PartnershipStatus.Rejected };
        _connections.GetTrackedByIdAsync(connection.Id, Arg.Any<CancellationToken>()).Returns(connection);
        var service = new PartnerNetworkService(_connections, Substitute.For<IVehicleRepository>(), Substitute.For<ITripRepository>(),
            Substitute.For<IAuctionRepository>(), _publisher);

        await service.RemovePartnershipAsync(connection.ContractorId, connection.Id);

        await _publisher.DidNotReceive().PublishAsync(Arg.Any<IReadOnlyCollection<NotificationMessage>>());
    }

    // ---------------------------------------------------------------------
    // Catalog
    // ---------------------------------------------------------------------

    [Fact]
    public void Route_label_follows_the_itinerary_order()
    {
        var first = new RouteSegment { OriginAddress = "Curitiba, PR", DestinationAddress = "Joinville, SC", OriginCoordinate = "", DestinationCoordinate = "", RouteSequence = 1 };
        var second = new RouteSegment { OriginAddress = "Joinville, SC", DestinationAddress = "Blumenau, SC", OriginCoordinate = "", DestinationCoordinate = "", RouteSequence = 2 };

        NotificationCatalog.RouteLabel(new[] { second, first }).Should().Be("Curitiba, PR → Blumenau, SC");
        NotificationCatalog.RouteLabel(Array.Empty<RouteSegment>()).Should().Be("rota consolidada");
    }
}
