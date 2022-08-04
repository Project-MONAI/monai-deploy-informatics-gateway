// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal static class Resources
    {
        public const int AcceptAcknowledgementType = 15;

        public const char AsciiVT = (char)0x0B;
        public const char AsciiFS = (char)0x1C;

        public const string AcknowledgmentTypeNever = "NE";
        public const string AcknowledgmentTypeError = "ER";
        public const string AcknowledgmentTypeSuccessful = "SU";

        public const string MessageHeaderSegment = "MSH";
    }
}
