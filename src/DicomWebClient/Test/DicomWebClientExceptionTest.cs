/*
 * Copyright 2022 MONAI Consortium
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

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class DicomWebClientExceptionTest
    {
        [Fact]
        public void TestControlExceptionSerialization()
        {
            var exception = new DicomWebClientException(System.Net.HttpStatusCode.OK, "message", new System.Exception("bla"));

            var data = SerializeToBytes(exception);
            var result = DeserializeFromBytes<DicomWebClientException>(data);

            Assert.Equal(exception.Message, result.Message);
        }

        private static byte[] SerializeToBytes<T>(T e)
        {
            using var stream = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            new BinaryFormatter().Serialize(stream, e);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            return stream.GetBuffer();
        }

        private static T DeserializeFromBytes<T>(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return (T)new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }
    }
}
