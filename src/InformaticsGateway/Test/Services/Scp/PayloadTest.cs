using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class PayloadTest
    {
        [RetryFact(DisplayName = "Payload shall be able to add new instance and reset timer")]
        public async Task Payload_AddsNewInstance()
        {
            var payload = new Payload("key", 1);
            payload.Add(new FileStorageInfo());
            await Task.Delay(500);
            Assert.False(payload.HasTimedOut);
            payload.Add(new FileStorageInfo());
            await Task.Delay(500);
            Assert.False(payload.HasTimedOut);
            Assert.Equal("key", payload.Key);
        }

        [RetryFact(DisplayName = "Payload shall not reset timer")]
        public async Task Payload_ShallNotResetTimer()
        {
            var payload = new Payload("key", 1);
            payload.Add(new FileStorageInfo());
            await Task.Delay(1000);
            Assert.True(payload.HasTimedOut);
        }

        [RetryFact(DisplayName = "Payload shall allow retry up to 3 times")]
        public void Payload_ShallAllowRetryUpTo3Times()
        {
            var payload = new Payload("key", 1);

            Assert.True(payload.CanRetry());
            Assert.True(payload.CanRetry());
            Assert.False(payload.CanRetry());
        }

        [RetryFact(DisplayName = "Payload shall dispose timer")]
        public void Payload_ShallDisposeTimer()
        {
            var payload = new Payload("key", 1);
            payload.Add(new FileStorageInfo());
            Assert.Single(payload);
            payload.Dispose();
            Assert.Empty(payload);
            Assert.False(payload.HasTimedOut);
        }
    }
}
