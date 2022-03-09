// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net.Http;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client
{
    internal class QidoService : ServiceBase, IQidoService
    {
        public QidoService(HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        {
        }

        /// <inheritdoc />
        public IAsyncEnumerable<T> SearchForStudies<T>()
            => SearchForStudies<T>(null);

        /// <inheritdoc />
        public IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters)
            => SearchForStudies<T>(queryParameters, null);

        /// <inheritdoc />
        public IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude)
            => SearchForStudies<T>(queryParameters, fieldsToInclude, false);

        /// <inheritdoc />
        public IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching)
            => SearchForStudies<T>(queryParameters, fieldsToInclude, fuzzyMatching, 0);

        /// <inheritdoc />
        public IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching, int limit)
            => SearchForStudies<T>(queryParameters, fieldsToInclude, fuzzyMatching, limit, 0);

        /// <inheritdoc />
        public async IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching, int limit, int offset)
        {
            var studyUri = GetStudiesUri();

            var queries = new List<string>();

            AppendQueryParameters(queries, queryParameters);
            AppendAdditionalFields(queries, fieldsToInclude);
            AppendQueryOptions(queries, fuzzyMatching, limit, offset);

            var searchUri = new Uri($"{studyUri}{(queries.Count > 0 ? "?" : "")}{string.Join("&", queries)}", UriKind.Relative);
            await foreach (var metadata in GetMetadata<T>(searchUri))
            {
                yield return metadata;
            }
        }

        private void AppendQueryOptions(List<string> queries, bool fuzzyMatching, int limit, int offset)
        {
            Guard.Against.Null(queries, nameof(queries));
            if (fuzzyMatching)
            {
                queries.Add("fuzzymatching=true");
            }

            if (limit > 0)
            {
                queries.Add($"limit={limit}");
            }

            if (offset > 0)
            {
                queries.Add($"offset={offset}");
            }
        }

        private void AppendAdditionalFields(List<string> queries, IReadOnlyList<string> fieldsToInclude)
        {
            Guard.Against.Null(queries, nameof(queries));

            if (fieldsToInclude is null || fieldsToInclude.Count == 0)
            {
                return;
            }

            foreach (var item in fieldsToInclude)
            {
                queries.Add($"includefield={item}");
            }
        }

        private void AppendQueryParameters(List<string> queries, IReadOnlyDictionary<string, string> queryParameters)
        {
            Guard.Against.Null(queries, nameof(queries));

            if (queryParameters is null || queryParameters.Count == 0)
            {
                return;
            }

            foreach (var key in queryParameters.Keys)
            {
                queries.Add($"{key}={queryParameters[key]}");
            }
        }
    }
}
