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

using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api;
using System;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Configuration.Test
{
    public class ValidationExtensionsTest
    {
        #region MonaiApplicationEntity.IsValid

        [Fact(DisplayName = "MonaiApplicationEntity - throw when null")]
        public void MonaiApplicationEntity_ShallThrowOnNull()
        {
            MonaiApplicationEntity monaiApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => monaiApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - invalid AE Title")]
        public void MonaiApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var monaiApplicationEntity = new MonaiApplicationEntity
            {
                AeTitle = "             "
            };
            Assert.False(monaiApplicationEntity.IsValid(out _));

            monaiApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(monaiApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - unsupported dicom tag for grouping")]
        public void MonaiApplicationEntity_UnsupportedGrouping()
        {
            var monaiApplicationEntity = new MonaiApplicationEntity
            {
                AeTitle = "AET",
                Grouping = DicomTag.PagePositionID.ToString()
            };
            Assert.False(monaiApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - invalid dicom tag for grouping")]
        public void MonaiApplicationEntity_InvlalidGrouping()
        {
            var monaiApplicationEntity = new MonaiApplicationEntity
            {
                AeTitle = "AET",
                Grouping = "12345678"
            };
            Assert.False(monaiApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - valid")]
        public void MonaiApplicationEntity_Valid()
        {
            var monaiApplicationEntity = new MonaiApplicationEntity
            {
                AeTitle = "AET",
                Grouping = DicomTag.StudyInstanceUID.ToString()
            };
            Assert.True(monaiApplicationEntity.IsValid(out _));
        }

        #endregion MonaiApplicationEntity.IsValid

        #region DestinationApplicationEntity.IsValid

        [Fact(DisplayName = "DestinationApplicationEntity - throw when null")]
        public void DestinationApplicationEntity_ShallThrowOnNull()
        {
            DestinationApplicationEntity destinationApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => destinationApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid AE Title")]
        public void DestinationApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity
            {
                AeTitle = "             "
            };
            Assert.False(destinationApplicationEntity.IsValid(out _));

            destinationApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(destinationApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid name")]
        public void DestinationApplicationEntity_InvalidWhenNameIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity
            {
                Name = "     ",
                AeTitle = "AET"
            };
            Assert.False(destinationApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid host")]
        public void DestinationApplicationEntity_InvalidWhenHostIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity
            {
                Name = "NAME",
                HostIp = "     ",
                AeTitle = "AET"
            };
            Assert.False(destinationApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid port")]
        public void DestinationApplicationEntity_InvalidPort()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity
            {
                Name = "NAME",
                HostIp = "SERVER",
                AeTitle = "AET",

                Port = 0
            };
            Assert.False(destinationApplicationEntity.IsValid(out _));

            destinationApplicationEntity.Port = 65536;
            Assert.False(destinationApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - valid")]
        public void DestinationApplicationEntity_Valid()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity
            {
                Name = "NAME",
                AeTitle = "AET",
                HostIp = "HOSTNAME",
                Port = 104
            };
            Assert.True(destinationApplicationEntity.IsValid(out _));
        }

        #endregion DestinationApplicationEntity.IsValid

        #region SourceApplicationEntity.IsValid

        [Fact(DisplayName = "DestinationApplicationEntity - throw when null")]
        public void SourceApplicationEntity_ShallThrowOnNull()
        {
            SourceApplicationEntity sourceApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => sourceApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid AE Title")]
        public void SourceApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var sourceApplicationEntity = new SourceApplicationEntity
            {
                AeTitle = "             "
            };
            Assert.False(sourceApplicationEntity.IsValid(out _));

            sourceApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(sourceApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid host")]
        public void SourceApplicationEntity_InvalidWhenHostIsEmpty()
        {
            var sourceApplicationEntity = new SourceApplicationEntity
            {
                HostIp = "     ",
                AeTitle = "AET"
            };
            Assert.False(sourceApplicationEntity.IsValid(out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - valid")]
        public void SourceApplicationEntity_Valid()
        {
            var sourceApplicationEntity = new SourceApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "HOSTNAME"
            };
            Assert.True(sourceApplicationEntity.IsValid(out _));
        }

        #endregion SourceApplicationEntity.IsValid
    }
}
