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

using Monai.Deploy.InformaticsGateway.Api.Models;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public interface IExternalAppDetailsRepository
    {
        Task AddAsync(ExternalAppDetails details, CancellationToken cancellationToken);

        Task<List<ExternalAppDetails>> GetAsync(string studyInstanceId, CancellationToken cancellationToken);

        Task<ExternalAppDetails?> GetByPatientIdOutboundAsync(string patientId, CancellationToken cancellationToken);

        Task<ExternalAppDetails?> GetByStudyIdOutboundAsync(string studyInstanceId, CancellationToken cancellationToken);
    }
}
