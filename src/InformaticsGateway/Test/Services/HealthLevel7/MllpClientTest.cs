﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.HealthLevel7
{
    public class MllpClientTest
    {
        private const string SampleMessage = "MSH|^~\\&|MD|MD HOSPITAL|MD Test|MONAI Deploy|202207130000|SECURITY|MD^A01^ADT_A01|MSG00001|P|2.8|||<ACK>|\r\n";

        private readonly Mock<ITcpClientAdapter> _tcpClient;
        private readonly Hl7Configuration _config;
        private readonly Mock<ILogger<MllpClient>> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MllpClientTest()
        {
            _tcpClient = new Mock<ITcpClientAdapter>();
            _config = new Hl7Configuration();
            _logger = new Mock<ILogger<MllpClient>>();
            _cancellationTokenSource = new CancellationTokenSource();

            _tcpClient.SetupGet(p => p.RemoteEndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 100));
        }

        [Fact(DisplayName = "Constructor")]
        public void Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new MllpClient(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpClient(_tcpClient.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpClient(_tcpClient.Object, _config, null));

            new MllpClient(_tcpClient.Object, _config, _logger.Object);
        }

        [Fact(DisplayName = "ReceiveData - records exception thrown by network stream")]
        public async Task ReceiveData_ExceptionReadingStream()
        {
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Empty(results.Messages);
                Assert.NotNull(results.AggregateException);
                Assert.Single(results.AggregateException.InnerExceptions);
                Assert.Equal("error", results.AggregateException.InnerExceptions.First().Message);
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - no data")]
        public async Task ReceiveData_ZeroByte()
        {
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Empty(results.Messages);
                Assert.Null(results.AggregateException);
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - invalid message")]
        public async Task ReceiveData_InvalidMessage()
        {
            var message = @$"{Resources.AsciiVT}HELLO WORLD{Resources.AsciiFS}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hl7Message = new Message(SampleMessage);
            hl7Message.ParseMessage();

            var index = 0;
            var size = 0;
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
            stream.Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()));
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns<Memory<byte>, CancellationToken>((data, cancellationToken) =>
                {
                    var toBeCopied = messageBytes.Skip(index).Take(data.Length).ToArray();
                    toBeCopied.CopyTo(data);
                    index += toBeCopied.Length;
                    size = toBeCopied.Length;
                    return ValueTask.FromResult(size);
                });

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Empty(results.Messages);
                Assert.NotNull(results.AggregateException);
                Assert.Single(results.AggregateException.InnerExceptions);
                Assert.Contains("Failed to validate the message with error", results.AggregateException.InnerExceptions.First().Message);
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - disabled acknowledgment")]
        public async Task ReceiveData_DisabledAck()
        {
            _config.SendAcknowledgment = false;

            var originalMessage = SampleMessage.Replace("<ACK>", string.Empty);
            var message = @$"{Resources.AsciiVT}{originalMessage}{Resources.AsciiFS}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hl7Message = new Message(originalMessage);
            hl7Message.ParseMessage();

            var index = 0;
            var size = 0;
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
            stream.Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()));
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns<Memory<byte>, CancellationToken>((data, cancellationToken) =>
                {
                    var toBeCopied = messageBytes.Skip(index).Take(data.Length).ToArray();
                    toBeCopied.CopyTo(data);
                    index += toBeCopied.Length;
                    size = toBeCopied.Length;
                    return ValueTask.FromResult(size);
                });

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Single(results.Messages);
                Assert.Equal(originalMessage, results.Messages.First().HL7Message);
                Assert.Null(results.AggregateException);

                stream.Verify(p => p.FlushAsync(It.IsAny<CancellationToken>()), Times.Never());
                stream.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never());
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - user request never send acknowledgment")]
        public async Task ReceiveData_NeverSendAck()
        {
            var originalMessage = SampleMessage.Replace("<ACK>", Resources.AcknowledgmentTypeNever);
            var message = @$"{Resources.AsciiVT}{originalMessage}{Resources.AsciiFS}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hl7Message = new Message(originalMessage);
            hl7Message.ParseMessage();

            var index = 0;
            var size = 0;
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
            stream.Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()));
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns<Memory<byte>, CancellationToken>((data, cancellationToken) =>
                {
                    var toBeCopied = messageBytes.Skip(index).Take(data.Length).ToArray();
                    toBeCopied.CopyTo(data);
                    index += toBeCopied.Length;
                    size = toBeCopied.Length;
                    return ValueTask.FromResult(size);
                });

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Single(results.Messages);
                Assert.Equal(originalMessage, results.Messages.First().HL7Message);
                Assert.Null(results.AggregateException);

                stream.Verify(p => p.FlushAsync(It.IsAny<CancellationToken>()), Times.Never());
                stream.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never());
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - exception sending acknowledgment")]
        public async Task ReceiveData_ExceptionSendingAck()
        {
            var originalMessage = SampleMessage.Replace("<ACK>", string.Empty);
            var message = @$"{Resources.AsciiVT}{originalMessage}{Resources.AsciiFS}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hl7Message = new Message(originalMessage);
            hl7Message.ParseMessage();

            var index = 0;
            var size = 0;
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns<Memory<byte>, CancellationToken>((data, cancellationToken) =>
                {
                    var toBeCopied = messageBytes.Skip(index).Take(data.Length).ToArray();
                    toBeCopied.CopyTo(data);
                    index += toBeCopied.Length;
                    size = toBeCopied.Length;
                    return ValueTask.FromResult(size);
                });

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Single(results.Messages);
                Assert.Equal(originalMessage, results.Messages.First().HL7Message);
                Assert.NotNull(results.AggregateException);
                Assert.Single(results.AggregateException.InnerExceptions);
                Assert.Equal("error", results.AggregateException.InnerExceptions.First().Message);
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }

        [Fact(DisplayName = "ReceiveData - complete workflow")]
        public async Task ReceiveData_CompleteWorkflow()
        {
            var originalMessage = SampleMessage.Replace("<ACK>", string.Empty);
            var message = @$"{Resources.AsciiVT}{originalMessage}{Resources.AsciiFS}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hl7Message = new Message(originalMessage);
            hl7Message.ParseMessage();

            var index = 0;
            var size = 0;
            var stream = new Mock<INetworkStream>();
            stream.Setup(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
            stream.Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()));
            stream.Setup(p => p.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns<Memory<byte>, CancellationToken>((data, cancellationToken) =>
                {
                    var toBeCopied = messageBytes.Skip(index).Take(data.Length).ToArray();
                    toBeCopied.CopyTo(data);
                    index += toBeCopied.Length;
                    size = toBeCopied.Length;
                    return ValueTask.FromResult(size);
                });

            _tcpClient.Setup(p => p.GetStream()).Returns(stream.Object);
            var client = new MllpClient(_tcpClient.Object, _config, _logger.Object);

            var action = new Action<IMllpClient, MllpClientResult>((client, results) =>
            {
                Assert.Single(results.Messages);
                Assert.Equal(originalMessage, results.Messages.First().HL7Message);
                Assert.Null(results.AggregateException);

                stream.Verify(p => p.FlushAsync(It.IsAny<CancellationToken>()), Times.Once());
                stream.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once());
            });
            await client.Start(action, _cancellationTokenSource.Token);
        }
    }
}
