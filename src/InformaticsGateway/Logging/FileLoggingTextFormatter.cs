// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Globalization;
using System.Text;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public class FileLoggingTextFormatter : FileLogEntryTextBuilder
    {
        public static readonly FileLoggingTextFormatter Default = new FileLoggingTextFormatter();

        

        protected override void AppendTimestamp(StringBuilder sb, DateTimeOffset timestamp)
        {
            sb.Append(timestamp.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)).Append(" ");
        }

        protected override void AppendLogScopeInfo(StringBuilder sb, IExternalScopeProvider scopeProvider)
        {
            scopeProvider.ForEachScope((scope, builder) =>
            {
                builder.Append(' ');

                AppendLogScope(builder, scope);
            }, sb);
        }

        protected override void AppendLogScope(StringBuilder sb, object scope)
        {
            sb.Append("[").Append(scope).Append("]");
        }

        protected override void AppendMessage(StringBuilder sb, string message)
        {
            sb.Append(" => ");

            var length = sb.Length;
            sb.AppendLine(message);
            sb.Replace(Environment.NewLine, " ", length, message.Length);
        }
        

        public override void BuildEntryText(StringBuilder sb, string categoryName, LogLevel logLevel, EventId eventId, string message, Exception exception,
            IExternalScopeProvider scopeProvider, DateTimeOffset timestamp)
        {
            AppendTimestamp(sb, timestamp);

            AppendLogLevel(sb, logLevel);

            AppendCategoryName(sb, categoryName);

            AppendEventId(sb, eventId);

            if (scopeProvider != null)
                AppendLogScopeInfo(sb, scopeProvider);

            if (!string.IsNullOrEmpty(message))
                AppendMessage(sb, message);

            if (exception != null)
                AppendException(sb, exception);
        }
    }
}