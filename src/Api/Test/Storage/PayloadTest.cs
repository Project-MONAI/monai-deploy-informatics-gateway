/*
 * Copyright 2021-2023 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class PayloadTest
    {
        [RetryFact]
        public async Task GivenStorageInfoObjects_WhenAddIsCalled_ShouldAddToPayloadAndResetTime()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 1);
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "souce" }));
            await Task.Delay(450).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.False(payload.HasTimedOut);
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file2", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "souce" }));
            await Task.Delay(450).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.False(payload.HasTimedOut);
            Assert.Equal("key", payload.Key);
        }

        [RetryFact]
        public async Task GivenOneStorageInfoObject_AfterAddIsCalled_ExpectTimerToSet()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 1);
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "souce" }));
            await Task.Delay(1001).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.True(payload.HasTimedOut);
        }

        [RetryFact]
        public void GivenAPayload_WhenDIsposed_ExpecteTImeToBeDiposed()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 1);
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "souce" }));
            Assert.Single(payload.Files);
            payload.Dispose();
            Assert.Empty(payload.Files);
            Assert.False(payload.HasTimedOut);
        }

        [RetryFact]
        public void GivenMultipleStorageObjectsWithDifferentDataOrigins_WhenAddedToPayload_ShouldHaveCorrectTriggerAndOrigins()
        {
            var payload = new Payload("key", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 1);
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.ACR, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.ACR, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DicomWeb, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DicomWeb, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.HL7, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.HL7, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.FHIR, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.FHIR, Destination = "dest", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest1", Source = "souce" }));
            payload.Add(new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt", new Messaging.Events.DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "souce2" }));

            Assert.StrictEqual(payload.DataTrigger, payload.Files[0].DataOrigin);

            Assert.Collection(payload.DataOrigins,
                item => item.Equals(payload.Files[1].DataOrigin),
                item => item.Equals(payload.Files[3].DataOrigin),
                item => item.Equals(payload.Files[5].DataOrigin),
                item => item.Equals(payload.Files[7].DataOrigin),
                item => item.Equals(payload.Files[8].DataOrigin),
                item => item.Equals(payload.Files[9].DataOrigin));

            payload.Dispose();
            Assert.Empty(payload.Files);
            Assert.False(payload.HasTimedOut);
        }
    }
}
