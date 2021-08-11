// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Monai.Deploy.InformaticsGateway.Api;
using System;
using System.Collections.Generic;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Configuration.Test
{
    public class ValidationExtensionsTest
    {
        #region MonaiApplicationEntity.IsValid

        [Fact(DisplayName = "MonaiApplicationEntity - throw when null")]
        public void MonaiApplicationEntity_ShallThrowOnNull()
        {
            MonaiApplicationEntity MonaiApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => MonaiApplicationEntity.IsValid(new List<string>(), out _));

            MonaiApplicationEntity = new MonaiApplicationEntity();
            Assert.Throws<ArgumentNullException>(() => MonaiApplicationEntity.IsValid(null, out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - invalid AE Title")]
        public void MonaiApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var MonaiApplicationEntity = new MonaiApplicationEntity();
            MonaiApplicationEntity.AeTitle = "             ";
            Assert.False(MonaiApplicationEntity.IsValid(new List<string>(), out _));

            MonaiApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(MonaiApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - invalid if already exists")]
        public void MonaiApplicationEntity_InvalidIfAlreadyExists()
        {
            var MonaiApplicationEntity = new MonaiApplicationEntity();
            MonaiApplicationEntity.AeTitle = "AET";
            Assert.False(MonaiApplicationEntity.IsValid(new List<string>() { "AET" }, out _));
        }

        [Fact(DisplayName = "MonaiApplicationEntity - valid")]
        public void MonaiApplicationEntity_Valid()
        {
            var MonaiApplicationEntity = new MonaiApplicationEntity();
            MonaiApplicationEntity.AeTitle = "AET";
            Assert.True(MonaiApplicationEntity.IsValid(new List<string>(), out _));
        }

        #endregion MonaiApplicationEntity.IsValid

        #region DestinationApplicationEntity.IsValid

        [Fact(DisplayName = "DestinationApplicationEntity - throw when null")]
        public void DestinationApplicationEntity_ShallThrowOnNull()
        {
            DestinationApplicationEntity destinationApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => destinationApplicationEntity.IsValid(new List<string>(), out _));

            destinationApplicationEntity = new DestinationApplicationEntity();
            Assert.Throws<ArgumentNullException>(() => destinationApplicationEntity.IsValid(null, out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid AE Title")]
        public void DestinationApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.AeTitle = "             ";
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));

            destinationApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid name")]
        public void DestinationApplicationEntity_InvalidWhenNameIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.Name = "     ";
            destinationApplicationEntity.AeTitle = "AET";
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid host")]
        public void DestinationApplicationEntity_InvalidWhenHostIsEmpty()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.Name = "NAME";
            destinationApplicationEntity.HostIp = "     ";
            destinationApplicationEntity.AeTitle = "AET";
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid port")]
        public void DestinationApplicationEntity_InvalidPort()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.Name = "NAME";
            destinationApplicationEntity.HostIp = "SERVER";
            destinationApplicationEntity.AeTitle = "AET";

            destinationApplicationEntity.Port = 0;
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));

            destinationApplicationEntity.Port = 65536;
            Assert.False(destinationApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid if already exists")]
        public void DestinationApplicationEntity_InvalidIfAlreadyExists()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.Name = "NAME";
            destinationApplicationEntity.AeTitle = "AET";
            Assert.False(destinationApplicationEntity.IsValid(new List<string>() { "NAME" }, out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - valid")]
        public void DestinationApplicationEntity_Valid()
        {
            var destinationApplicationEntity = new DestinationApplicationEntity();
            destinationApplicationEntity.Name = "NAME";
            destinationApplicationEntity.AeTitle = "AET";
            destinationApplicationEntity.HostIp = "HOSTNAME";
            destinationApplicationEntity.Port = 104;
            Assert.True(destinationApplicationEntity.IsValid(new List<string>(), out _));
        }

        #endregion DestinationApplicationEntity.IsValid

        #region SourceApplicationEntity.IsValid

        [Fact(DisplayName = "DestinationApplicationEntity - throw when null")]
        public void SourceApplicationEntity_ShallThrowOnNull()
        {
            SourceApplicationEntity sourceApplicationEntity = null;
            Assert.Throws<ArgumentNullException>(() => sourceApplicationEntity.IsValid(new List<string>(), out _));

            sourceApplicationEntity = new SourceApplicationEntity();
            Assert.Throws<ArgumentNullException>(() => sourceApplicationEntity.IsValid(null, out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid AE Title")]
        public void SourceApplicationEntity_InvalidWhenAeTitleIsEmpty()
        {
            var sourceApplicationEntity = new SourceApplicationEntity();
            sourceApplicationEntity.AeTitle = "             ";
            Assert.False(sourceApplicationEntity.IsValid(new List<string>(), out _));

            sourceApplicationEntity.AeTitle = "ABCDEFGHIJKLMNOPQRSTUVW";
            Assert.False(sourceApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid host")]
        public void SourceApplicationEntity_InvalidWhenHostIsEmpty()
        {
            var sourceApplicationEntity = new SourceApplicationEntity();
            sourceApplicationEntity.HostIp = "     ";
            sourceApplicationEntity.AeTitle = "AET";
            Assert.False(sourceApplicationEntity.IsValid(new List<string>(), out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - invalid if already exists")]
        public void SourceApplicationEntity_InvalidIfAlreadyExists()
        {
            var sourceApplicationEntity = new SourceApplicationEntity();
            sourceApplicationEntity.AeTitle = "AET";
            sourceApplicationEntity.HostIp = "HOST";
            Assert.False(sourceApplicationEntity.IsValid(new List<string>() { "AET" }, out _));
        }

        [Fact(DisplayName = "DestinationApplicationEntity - valid")]
        public void SourceApplicationEntity_Valid()
        {
            var sourceApplicationEntity = new SourceApplicationEntity();
            sourceApplicationEntity.AeTitle = "AET";
            sourceApplicationEntity.HostIp = "HOSTNAME";
            Assert.True(sourceApplicationEntity.IsValid(new List<string>(), out _));
        }

        #endregion SourceApplicationEntity.IsValid
    }
}