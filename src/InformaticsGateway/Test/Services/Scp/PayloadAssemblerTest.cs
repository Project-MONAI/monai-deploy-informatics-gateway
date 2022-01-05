using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class PayloadAssemblerTest
    {
        private readonly Mock<ILogger<PayloadAssembler>> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PayloadAssemblerTest()
        {
            _logger = new Mock<ILogger<PayloadAssembler>>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [RetryFact(DisplayName = "PayloadAssembler shall queue items using default timeout")]
        public async void PayloadAssembler_ShallQueueWithDefaultTimeout()
        {
            var payloadAssembler = new PayloadAssembler(_logger.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            payloadAssembler.Queue("A", new Api.FileStorageInfo());

            _logger.VerifyLogging($"Bucket A created with timeout {PayloadAssembler.DEFAULT_TIMEOUT}s.", LogLevel.Information, Times.Once());
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();
        }

        [RetryFact(DisplayName = "PayloadAssembler shall be disposed properly")]
        public async Task PayloadAssembler_ShallBeDisposedProperly()
        {
            var payloadAssembler = new PayloadAssembler(_logger.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            payloadAssembler.Queue("A", new Api.FileStorageInfo());

            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();

            await Task.Delay(1000);
            _logger.VerifyLoggingMessageBeginsWith($"Number of collections in queue", LogLevel.Trace, Times.Never());
        }

        [RetryFact(DisplayName = "PayloadAssembler shall enqueue payload on timed event")]
        public async Task PayloadAssembler_ShallEnqueuePayloadOnTimedEvent()
        {
            var payloadAssembler = new PayloadAssembler(_logger.Object);

            payloadAssembler.Queue("A", new Api.FileStorageInfo(), 1);
            await Task.Delay(1001);
            var result = payloadAssembler.Dequeue(_cancellationTokenSource.Token);
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of collections in queue: 1.", LogLevel.Trace, Times.Once());
            Assert.Single(result);
            _logger.VerifyLoggingMessageBeginsWith($"Bucket A sent to processing queue", LogLevel.Information, Times.Once());
        }
    }
}
