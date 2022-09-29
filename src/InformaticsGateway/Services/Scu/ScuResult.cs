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

namespace Monai.Deploy.InformaticsGateway.Services.Scu
{
    public enum ResponseStatus
    {
        Success,
        Failure,
        Unknown
    }

    public enum ResponseError
    {
        None,
        AssociationRejected,
        CEchoError,
        Unhandled,
        UnsupportedRequestType,
        Unknown,
        AssociationAborted
    }

    public class ScuResponse
    {
        internal static readonly ScuResponse NullResponse = new ScuResponse { Status = ResponseStatus.Unknown, Error = ResponseError.Unknown };

        public ResponseStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public ResponseError Error { get; internal set; }
    }
}
