// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ControlExceptionTest
    {
        [Fact]
        public void TestControlExceptionSerialization()
        {
            var exception = new ControlException(100, "error");

            var data = SerializeToBytes(exception);
            var result = DeserializeFromBytes<ControlException>(data);

            Assert.Equal(exception.ErrorCode, result.ErrorCode);
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
