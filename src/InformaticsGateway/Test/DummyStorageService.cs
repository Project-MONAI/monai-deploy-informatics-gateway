/*
 * Copyright 2022 MONAI Consortium
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.Storage;
using Monai.Deploy.Storage.API;

namespace Monai.Deploy.InformaticsGateway.Test
{
    internal class DummyStorageRegistrar : ServiceRegistrationBase
    {
        public DummyStorageRegistrar(string fullyQualifiedAssemblyName) : base(fullyQualifiedAssemblyName)
        {
        }

        public override IServiceCollection Configure(IServiceCollection services) => services;
    }

    internal class DummyStorageService : IStorageService
    {
        public string Name => "Dummy Storage Service";

        public Task CopyObjectAsync(string sourceBucketName, string sourceObjectName, string destinationBucketName, string destinationObjectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task CopyObjectWithCredentialsAsync(string sourceBucketName, string sourceObjectName, string destinationBucketName, string destinationObjectName, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task CreateFolderAsync(string bucketName, string folderPath, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task CreateFolderWithCredentialsAsync(string bucketName, string folderPath, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Credentials> CreateTemporaryCredentialsAsync(string bucketName, string folderName, int durationSeconds = 3600, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Stream> GetObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Stream> GetObjectWithCredentialsAsync(string bucketName, string objectName, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IList<VirtualFileInfo>> ListObjectsAsync(string bucketName, string prefix = "", bool recursive = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IList<VirtualFileInfo>> ListObjectsWithCredentialsAsync(string bucketName, Credentials credentials, string prefix = "", bool recursive = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metadata, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task PutObjectWithCredentialsAsync(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metadata, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RemoveObjectsAsync(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RemoveObjectsWithCredentialsAsync(string bucketName, IEnumerable<string> objectNames, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RemoveObjectWithCredentialsAsync(string bucketName, string objectName, Credentials credentials, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<KeyValuePair<string, string>> VerifyObjectExistsAsync(string bucketName, KeyValuePair<string, string> objectPair) => throw new NotImplementedException();

        public Task<Dictionary<string, string>> VerifyObjectsExistAsync(string bucketName, Dictionary<string, string> objectDict) => throw new NotImplementedException();
    }
}
