// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.Common;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client
{
    internal class WadoService : ServiceBase, IWadoService
    {
        public WadoService(HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        { }

        /// <inheritdoc />
        public async IAsyncEnumerable<DicomFile> Retrieve(
            string studyInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            var studyUri = GetStudiesUri(studyInstanceUid);

            transferSyntaxes = transferSyntaxes.Trim();

            var message = new HttpRequestMessage(HttpMethod.Get, studyUri);
            message.Headers.Add(HeaderNames.Accept, BuildAcceptMediaHeader(MimeType.Dicom, transferSyntaxes));

            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {studyUri}");
            var response = await HttpClient.SendAsync(message).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await foreach (var item in response.ToDicomAsyncEnumerable())
            {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            DicomValidation.ValidateUI(seriesInstanceUid);

            var seriesUri = GetSeriesUri(studyInstanceUid, seriesInstanceUid);

            transferSyntaxes = transferSyntaxes.Trim();

            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {seriesUri}");
            var message = new HttpRequestMessage(HttpMethod.Get, seriesUri);
            message.Headers.Add(HeaderNames.Accept, BuildAcceptMediaHeader(MimeType.Dicom, transferSyntaxes));
            var response = await HttpClient.SendAsync(message).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await foreach (var item in response.ToDicomAsyncEnumerable())
            {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async Task<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            DicomValidation.ValidateUI(seriesInstanceUid);
            Guard.Against.NullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            DicomValidation.ValidateUI(sopInstanceUid);

            var instanceUri = GetInstanceUri(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            transferSyntaxes = transferSyntaxes.Trim();

            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {instanceUri}");
            var message = new HttpRequestMessage(HttpMethod.Get, instanceUri);
            message.Headers.Add(HeaderNames.Accept, BuildAcceptMediaHeader(MimeType.Dicom, transferSyntaxes));
            var response = await HttpClient.SendAsync(message).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            try
            {
                await response.ToDicomAsyncEnumerable().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, ex, "Failed to retrieve instances");
            }

            return null;
        }

        /// <inheritdoc />
        public Task<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            IReadOnlyList<uint> frameNumbers,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            throw new NotImplementedException("Retrieving instance frames API is not yet supported.");
        }

        /// <inheritdoc />
        public Task<byte[]> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            DicomTag dicomTag,
            params DicomTransferSyntax[] transferSyntaxes) =>
                Retrieve(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTag, null, transferSyntaxes);

        /// <inheritdoc />
        public async Task<byte[]> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            DicomTag dicomTag,
            Tuple<int, int?> byteRange = null,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            DicomValidation.ValidateUI(seriesInstanceUid);
            Guard.Against.NullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            DicomValidation.ValidateUI(sopInstanceUid);

            return await Retrieve(new Uri($"{RequestServicePrefix}studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}/bulk/{dicomTag.Group:X4}{dicomTag.Element:X4}", UriKind.Relative), byteRange, transferSyntaxes);
        }

        /// <inheritdoc />
        public Task<byte[]> Retrieve(
            Uri bulkdataUri,
            params DicomTransferSyntax[] transferSyntaxes) =>
                Retrieve(bulkdataUri, null, transferSyntaxes);

        /// <inheritdoc />
        public async Task<byte[]> Retrieve(
            Uri bulkdataUri,
            Tuple<int, int?> byteRange = null,
            params DicomTransferSyntax[] transferSyntaxes)
        {
            Guard.Against.Null(bulkdataUri, nameof(bulkdataUri));

            if (bulkdataUri.IsAbsoluteUri)
            {
                Guard.Against.MalformUri(bulkdataUri, nameof(bulkdataUri));
            }

            transferSyntaxes = transferSyntaxes.Trim();

            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {bulkdataUri}");
            var message = new HttpRequestMessage(HttpMethod.Get, bulkdataUri);
            message.Headers.Add(HeaderNames.Accept, BuildAcceptMediaHeader(MimeType.OctetStreme, transferSyntaxes));
            if (byteRange != null)
            {
                message.AddRange(byteRange);
            }
            var response = await HttpClient.SendAsync(message).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.ToBinaryData();
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> RetrieveMetadata<T>(
            string studyInstanceUid)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            var studyUri = GetStudiesUri(studyInstanceUid);
            var studyMetadataUri = new Uri($"{studyUri}metadata", UriKind.Relative);
            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {studyMetadataUri}");

            await foreach (var metadata in GetMetadata<T>(studyMetadataUri))
            {
                yield return metadata;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> RetrieveMetadata<T>(
            string studyInstanceUid,
            string seriesInstanceUid)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            DicomValidation.ValidateUI(seriesInstanceUid);

            var seriesUri = GetSeriesUri(studyInstanceUid, seriesInstanceUid);
            var seriesMetadataUri = new Uri($"{seriesUri}metadata", UriKind.Relative);
            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {seriesMetadataUri}");
            await foreach (var metadata in GetMetadata<T>(seriesMetadataUri))
            {
                yield return metadata;
            }
        }

        /// <inheritdoc />
        public async Task<T> RetrieveMetadata<T>(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            DicomValidation.ValidateUI(studyInstanceUid);
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            DicomValidation.ValidateUI(seriesInstanceUid);
            Guard.Against.NullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            DicomValidation.ValidateUI(sopInstanceUid);

            var instanceUri = GetInstanceUri(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            var instancMetadataUri = new Uri($"{instanceUri}metadata", UriKind.Relative);
            Logger?.Log(LogLevel.Debug, $"Sending HTTP request to {instancMetadataUri}");

            try
            {
                await GetMetadata<T>(instancMetadataUri).FirstOrDefaultAsync();
            }
            catch (Exception ex) when (ex is not UnsupportedReturnTypeException)
            {
                Logger?.Log(LogLevel.Error, ex, "Failed to retrieve metadata");
            }

            return default;
        }

        private string BuildAcceptMediaHeader(MimeType mimeType, DicomTransferSyntax[] transferSyntaxes)
        {
            if (transferSyntaxes is null || transferSyntaxes.Length == 0 || transferSyntaxes[0].UID.UID == "*")
            {
                return $@"{MimeMappings.MultiPartRelated}; type=""{MimeMappings.MimeTypeMappings[MimeType.Dicom]}""";
            }

            var acceptHeaders = new List<string>();
            foreach (var mediaType in transferSyntaxes)
            {
                if (!MimeMappings.IsValidMediaType(mediaType))
                {
                    throw new ArgumentException($"invalid media type: {mediaType}");
                }
                acceptHeaders.Add($@"{MimeMappings.MultiPartRelated}; type=""{MimeMappings.MimeTypeMappings[mimeType]}""; transfer-syntax={mediaType.UID.UID}");
            }

            var headers = string.Join(", ", acceptHeaders);
            Logger?.Log(LogLevel.Debug, $"Generated headers: {headers}");
            return headers;
        }

        private string GetSeriesUri(string studyInstanceUid = "", string seriesInstanceUid = "")
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid))
            {
                if (!string.IsNullOrWhiteSpace(seriesInstanceUid))
                {
                    Logger?.Log(LogLevel.Warning, "Series Instance UID not provided, will retrieve all instances for study.");
                }
                return $"{RequestServicePrefix}series/";
            }
            else
            {
                var studiesUri = GetStudiesUri(studyInstanceUid);
                return string.IsNullOrWhiteSpace(seriesInstanceUid) ?
                    $"{studiesUri}series/" :
                    $"{studiesUri}series/{seriesInstanceUid}/";
            }
        }

        private string GetInstanceUri(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            if (!string.IsNullOrWhiteSpace(studyInstanceUid) &&
                !string.IsNullOrWhiteSpace(seriesInstanceUid))
            {
                var seriesUri = GetSeriesUri(studyInstanceUid, seriesInstanceUid);
                return string.IsNullOrWhiteSpace(sopInstanceUid) ?
                    $"{seriesUri}instances/" :
                    $"{seriesUri}instances/{sopInstanceUid}/";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(sopInstanceUid))
                {
                    Logger?.Log(LogLevel.Warning, "SOP Instance UID not provided, will retrieve all instances for study.");
                }
                return $"{RequestServicePrefix}instances/";
            }
        }
    }
}
