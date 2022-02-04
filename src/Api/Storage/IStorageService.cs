// Copyright 2021-2022 MONAI Consortium
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public interface IStorageService
    {
        /// <summary>
        /// Gets or sets the name of the storage service.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Lists objects in a bucket.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <param name="prefix">Objects with name starts with prefix</param>
        /// <param name="recursive">Whether to recurse into subdirectories</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns></returns>
        IList<VirtualFileInfo> ListObjects(string bucketName, string prefix = "", bool recursive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads an objects as stream.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <param name="objectName">Name of the object in the bucket</param>
        /// <param name="callback">Action to be called when stream is ready</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns>Task</returns>
        Task GetObject(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads an object.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <param name="objectName">Name of the object in the bucket</param>
        /// <param name="data">Stream to upload</param>
        /// <param name="size">Size of the stream</param>
        /// <param name="contentType">Content type of the object</param>
        /// <param name="metadata">Metadata of the object</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns>Task</returns>
        Task PutObject(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies content of an object from source to destination.
        /// </summary>
        /// <param name="sourceBucketName">Name of the source bucket</param>
        /// <param name="sourceObjectName">Name of the object in the source bucket</param>
        /// <param name="destinationBucketName">Name of the destination bucket</param>
        /// <param name="destinationObjectName">Name of the object in the destination bucket</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns>Task</returns>
        Task CopyObject(string sourceBucketName, string sourceObjectName, string destinationBucketName, string destinationObjectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an objects.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <param name="objectName">Name of the object in the bucket</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns>Task</returns>
        Task RemoveObject(string bucketName, string objectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a list of objects.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <param name="objectNames">An enumerable of object names to be removed in the bucket</param>
        /// <param name="cancellationToken">Optional cancellation token. Defaults to default(CancellationToken)</param>
        /// <returns>Task</returns>
        Task RemoveObjects(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default);
    }
}
