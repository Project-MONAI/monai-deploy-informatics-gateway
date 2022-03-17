// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.Test
{
    public class ProblemExceptionTest
    {
        [Fact]
        public void TestProblemExceptionSerialization()
        {
            var exception = new ProblemException(new ProblemDetails
            {
                Detail = "details",
                Title = "title",
                Status = 100
            });

            var data = SerializeToBytes(exception);
            var result = DeserializeFromBytes<ProblemException>(data);

            Assert.Equal(exception.ProblemDetails.Detail, result.ProblemDetails.Detail);
            Assert.Equal(exception.ProblemDetails.Title, result.ProblemDetails.Title);
            Assert.Equal(exception.ProblemDetails.Status, result.ProblemDetails.Status);
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
