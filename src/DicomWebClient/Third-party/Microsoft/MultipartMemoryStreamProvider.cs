// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http.Headers;
using Ardalis.GuardClauses;

namespace System.Net.Http
{
    /// <summary>
    /// Provides a <see cref="MultipartStreamProvider"/> implementation that returns a <see cref="MemoryStream"/> instance.
    /// This facilitates deserialization or other manipulation of the contents in memory.
    /// </summary>
    public class MultipartMemoryStreamProvider : MultipartStreamProvider
    {
        /// <summary>
        /// This <see cref="MultipartStreamProvider"/> implementation returns a <see cref="MemoryStream"/> instance.
        /// This facilitates deserialization or other manipulation of the contents in memory.
        /// </summary>
        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            Guard.Against.Null(parent, nameof(parent));
            Guard.Against.Null(headers, nameof(headers));

            return new MemoryStream();
        }
    }
}
