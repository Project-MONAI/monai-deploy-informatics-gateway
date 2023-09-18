/*
 * Copyright 2023 MONAI Consortium
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

using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace Monai.Deploy.InformaticsGateway.Client
{
    internal static class HttpContentExtensions
    {
        public static async System.Threading.Tasks.Task<T> ReadAsAsync<T>(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            using (var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, Configuration.JsonSerializationOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
