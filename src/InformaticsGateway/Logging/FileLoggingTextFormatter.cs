// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Globalization;
using System.Text;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public class FileLoggingTextFormatter : FileLogEntryTextBuilder
    {
        public static readonly FileLoggingTextFormatter Default = new();

        protected override void AppendTimestamp(StringBuilder sb, DateTimeOffset timestamp)
        {
            sb.Append(timestamp.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)).Append(' ');
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
            sb.Append('[').Append(scope).Append(']');
        }

        protected override void AppendMessage(StringBuilder sb, string message)
        {
            sb.Append(" => ");

            var length = sb.Length;
            sb.AppendLine(message);
            sb.Replace(Environment.NewLine, " ", length, message.Length);
        }

        public override void BuildEntryText(
            StringBuilder sb,
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            string message,
            Exception exception,
            IExternalScopeProvider scopeProvider,
            DateTimeOffset timestamp)
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
