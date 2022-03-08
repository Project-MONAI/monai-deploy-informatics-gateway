// Copyright 2021-2022, MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Logging;
using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Logging
{
    public class FileLoggingTextFormatterTest
    {
        [Fact(DisplayName = "BuildEntryText")]
        public void BuildEntryText()
        {
            var sb = new StringBuilder();
            var timestamp = DateTimeOffset.Now;
            var cateogry = "Test";
            var eventId = new EventId(100);
            var message = "This is a test";
            var exception = new Exception("Exception");
            var scopeProvider = new LoggerExternalScopeProvider();
            scopeProvider.Push("StateA");
            scopeProvider.Push("StateB");

            var formatter = FileLoggingTextFormatter.Default;
            formatter.BuildEntryText(
                sb, cateogry, LogLevel.Information, eventId, message,
                exception, scopeProvider, timestamp);

            var result = sb.ToString();
            Assert.Contains(timestamp.ToLocalTime().ToString("o", CultureInfo.InvariantCulture), result);
            Assert.Contains($"info: {cateogry}[{eventId.Id}] [StateA] [StateB] => {message}", result);
            Assert.Contains("System.Exception: Exception", result);
        }
    }
}
