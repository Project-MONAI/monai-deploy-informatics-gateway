﻿/*
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


using System.Threading.Tasks;
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Api.Mllp
{
    public interface IMllpExtract
    {
        Task<Message> ExtractInfo(Hl7FileStorageMetadata meta, Message message, Hl7ApplicationConfigEntity configItem);

        Task<Hl7ApplicationConfigEntity?> GetConfigItem(Message message);
    }
}
