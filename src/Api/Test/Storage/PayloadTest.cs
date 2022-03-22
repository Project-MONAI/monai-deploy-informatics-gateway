// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class PayloadTest
    {
        [Fact(DisplayName = "Payload shall be able to add new instance and reset timer")]
        public async Task Payload_AddsNewInstance()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
            payload.Add(new TestStorageInfo("file"));
            await Task.Delay(450).ConfigureAwait(false);
            Assert.False(payload.HasTimedOut);
            payload.Add(new TestStorageInfo("file"));
            await Task.Delay(450).ConfigureAwait(false);
            Assert.False(payload.HasTimedOut);
            Assert.Equal("key", payload.Key);
        }

        [Fact(DisplayName = "Payload shall not reset timer")]
        public async Task Payload_ShallNotResetTimer()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
            payload.Add(new TestStorageInfo("file"));
            await Task.Delay(1001).ConfigureAwait(false);
            Assert.True(payload.HasTimedOut);
        }

        [Fact(DisplayName = "Payload shall allow retry up to 3 times")]
        public void Payload_ShallAllowRetryUpTo3Times()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);

            Assert.True(payload.CanRetry());
            Assert.True(payload.CanRetry());
            Assert.False(payload.CanRetry());
        }

        [Fact(DisplayName = "Payload shall dispose timer")]
        public void Payload_ShallDisposeTimer()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), 1);
            payload.Add(new TestStorageInfo("file"));
            Assert.Single(payload.Files);
            payload.Dispose();
            Assert.Empty(payload.Files);
            Assert.False(payload.HasTimedOut);
        }
    }
}
