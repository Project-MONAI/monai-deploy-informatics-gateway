/*
 * Copyright 2021-2023 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public static class ExitCodes
    {
        public const int Success = 0;

        public const int Config_NotConfigured = 100;
        public const int Config_ErrorSaving = 101;
        public const int Config_ErrorInitializing = 102;
        public const int Config_ErrorShowing = 103;

        public const int MonaiScp_ErrorList = 200;
        public const int MonaiScp_ErrorDelete = 201;
        public const int MonaiScp_ErrorCreate = 202;
        public const int MonaiScp_ErrorUpdate = 203;
        public const int MonaiScp_ErrorPlugIns = 204;

        public const int DestinationAe_ErrorList = 300;
        public const int DestinationAe_ErrorDelete = 301;
        public const int DestinationAe_ErrorCreate = 302;
        public const int DestinationAe_ErrorCEcho = 303;
        public const int DestinationAe_ErrorUpdate = 304;
        public const int DestinationAe_ErrorPlugIns = 305;

        public const int SourceAe_ErrorList = 400;
        public const int SourceAe_ErrorDelete = 401;
        public const int SourceAe_ErrorCreate = 402;
        public const int SourceAe_ErrorUpdate = 403;

        public const int Restart_Cancelled = 500;
        public const int Restart_Error = 501;

        public const int Start_Cancelled = 600;
        public const int Start_Error = 601;
        public const int Start_Error_ApplicationNotFound = 602;
        public const int Start_Error_ApplicationAlreadyRunning = 603;

        public const int Stop_Cancelled = 700;
        public const int Stop_Error = 701;

        public const int Status_Error = 800;
    }
}
