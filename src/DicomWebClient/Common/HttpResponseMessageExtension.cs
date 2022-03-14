// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.Common
{
    internal static class HttpMessageExtension
    {
        public static void AddRange(this HttpRequestMessage request, Tuple<int, int?> byteRange = null)
        {
            Guard.Against.Null(request, nameof(request));
            if (byteRange is null)
            {
                request.Headers.Add(HeaderNames.Range, "byte=0-");
            }
            else
            {
                var end = byteRange.Item2.HasValue ? byteRange.Item2.Value.ToString() : "";
                request.Headers.Add(HeaderNames.Range, $"byte={byteRange.Item1}-{end}");
            }
        }

        public static async IAsyncEnumerable<DicomFile> ToDicomAsyncEnumerable(this HttpResponseMessage response)
        {
            Guard.Against.Null(response, nameof(response));
            Guard.Against.Null(response.Content, nameof(response.Content));
            await foreach (var buffer in DecodeMultipartMessage(response))
            {
                using (var memoryStream = new MemoryStream(buffer))
                {
                    yield return await DicomFile.OpenAsync(memoryStream, FileReadOption.ReadAll);
                }
            }
        }

        public static async Task<byte[]> ToBinaryData(this HttpResponseMessage response)
        {
            Guard.Against.Null(response, nameof(response));
            using (var memoryStream = new MemoryStream())
            {
                await foreach (var buffer in DecodeMultipartMessage(response))
                {
                    await memoryStream.WriteAsync(buffer, 0, buffer.Length);
                }
                return memoryStream.ToArray();
            }
        }

        private static async IAsyncEnumerable<byte[]> DecodeMultipartMessage(HttpResponseMessage response)
        {
            Guard.Against.Null(response, nameof(response));
            var contentType = response.Content.Headers.ContentType;
            if (contentType.MediaType != MimeMappings.MultiPartRelated)
            {
                throw new ResponseDecodeException($"Unexpected media type {contentType.MediaType}.  Expected {MimeMappings.MultiPartRelated}");
            }

            var multipartContent = await response.Content.ReadAsMultipartAsync().ConfigureAwait(false);
            foreach (var content in multipartContent.Contents)
            {
                yield return await content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }
    }
}
