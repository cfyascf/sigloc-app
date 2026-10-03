using System.Net;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Monitoring;
using static Sigloc.Tests.Monitoring.ProviderTestSupport;

namespace Sigloc.Tests.Monitoring;

public sealed class ProviderFailureAndCancellationTests
{
    private static async Task InvokeAsync(bool tracking, HttpClient client,
        CancellationToken token = default, MonitoringProviderSettings? settings = null)
    {
        if (tracking)
            await Tracking(client, settings).GetLatestAsync(7, Now, token);
        else
            await Routing(client, settings).CalculateAsync(Coordinates(2), token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PreCanceledRequestDoesNotCallHttp(bool tracking)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeAsync(tracking, client, cancellation.Token));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task CallerCancellationPropagatesDuringHeadersOrBody(bool tracking, bool duringBody)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        using var handler = WaitingHandler(duringBody, entered);
        using var client = new HttpClient(handler);
        var operation = InvokeAsync(tracking, client, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task ProviderDeadlineCoversHeadersAndBodyAndIsUnavailableNotCallerCancellation(bool tracking, bool duringBody)
    {
        using var handler = WaitingHandler(duringBody, new(TaskCreationOptions.RunContinuationsAsynchronously));
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var exception = await Assert.ThrowsAsync<TrackingUnavailableException>(() => InvokeAsync(tracking, client,
            settings: new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, ProviderTimeoutSeconds = 1 })
            .WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Null(exception.InnerException);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 2)]
    public async Task TransportAndHttpClientTimeoutFailuresAreSanitized(bool tracking, int failure)
    {
        using var handler = new FakeHttpHandler((_, _) => throw failure switch
        {
            0 => new HttpRequestException(Secret),
            1 => new IOException(Secret),
            _ => new TaskCanceledException(Secret)
        });
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<TrackingUnavailableException>(() => InvokeAsync(tracking, client));
        Assert.Equal("Trip monitoring is temporarily unavailable.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(Secret, exception.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationWinsOverConcurrentTransportFailure(bool tracking)
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new FakeHttpHandler((_, _) =>
        {
            cancellation.Cancel();
            throw new HttpRequestException(Secret);
        });
        using var client = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeAsync(tracking, client, cancellation.Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MockModeDoesNotMakeRealAdaptersSilentlyFallback(bool tracking)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Secret, HttpStatusCode.ServiceUnavailable)));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => InvokeAsync(tracking, client,
            settings: new() { MockMode = true, TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret }));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task RoutingRejectsInvalidTimeoutBeforeHttp(int timeout)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => InvokeAsync(false, client,
            settings: new() { ProviderTimeoutSeconds = timeout }));
        Assert.Equal(0, handler.Calls);
    }

    private static FakeHttpHandler WaitingHandler(bool duringBody, TaskCompletionSource entered) => new(async (_, token) =>
    {
        if (duringBody)
            return new(HttpStatusCode.OK) { Content = new StreamContent(new WaitingReadStream(entered)) };
        entered.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, token);
        throw new InvalidOperationException("Cancellation should interrupt the delay.");
    });

    private sealed class WaitingReadStream(TaskCompletionSource entered) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
