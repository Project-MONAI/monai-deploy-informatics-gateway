// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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

        public const int DestinationAe_ErrorList = 300;
        public const int DestinationAe_ErrorDelete = 301;
        public const int DestinationAe_ErrorCreate = 302;

        public const int SourceAe_ErrorList = 400;
        public const int SourceAe_ErrorDelete = 401;
        public const int SourceAe_ErrorCreate = 402;

        public static int Restart_Cancelled = 500;
        public static int Restart_Error = 501;

        public static int Start_Cancelled = 600;
        public static int Start_Error = 601;
        public static int Start_Error_ApplicationNotFound = 602;
        public static int Start_Error_ApplicationAlreadyRunning = 603;

        public static int Stop_Cancelled = 700;
        public static int Stop_Error = 701;

        public static int Status_Error = 800;
    }
}
