/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Logging;
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
