// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Monai.Deploy.InformaticsGateway.Api.Storage;
using System;
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
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
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
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
            payload.Add(new FileStorageInfo());
            await Task.Delay(1000);
            Assert.True(payload.HasTimedOut);
        }

        [RetryFact(DisplayName = "Payload shall allow retry up to 3 times")]
        public void Payload_ShallAllowRetryUpTo3Times()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);

            Assert.True(payload.CanRetry());
            Assert.True(payload.CanRetry());
            Assert.False(payload.CanRetry());
        }

        [RetryFact(DisplayName = "Payload shall dispose timer")]
        public void Payload_ShallDisposeTimer()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
            payload.Add(new FileStorageInfo());
            Assert.Single(payload.Files);
            payload.Dispose();
            Assert.Empty(payload.Files);
            Assert.False(payload.HasTimedOut);
        }
    }
}
