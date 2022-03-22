// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Xunit;
using DicomWebClientClass = Monai.Deploy.InformaticsGateway.DicomWeb.Client.DicomWebClient;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class DicomWebClientTest
    {
        private const string BaseUri = "http://dummy/api/";

        [Fact(DisplayName = "Constructor test")]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new DicomWebClientClass(null, null));
        }

        [Fact(DisplayName = "ConfigureServiceUris - throws on malformed uri root")]
        public void ConfigureServiceUris_ThrowsMalformedUriRoot()
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            Assert.Throws<ArgumentNullException>(() => dicomWebClient.ConfigureServiceUris(null));
        }

        [Fact(DisplayName = "ConfigureServiceUris - set rootUri")]
        public void ConfigureServiceUris_SetAllUris()
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            var rootUri = new Uri(BaseUri);
            var exception = Record.Exception(() => dicomWebClient.ConfigureServiceUris(rootUri));

            Assert.Null(exception);
        }

        [Theory(DisplayName = "ConfigureServiceUris - throws on malformed prefix")]
        [InlineData(DicomWebServiceType.Qido, "/bla\\?/")]
        [InlineData(DicomWebServiceType.Wado, "/bla\\?/")]
        [InlineData(DicomWebServiceType.Stow, "/bla\\?/")]
        public void ConfigureServicePrefix_ThrowsMalformedPrefixes(DicomWebServiceType serviceType, string prefix)
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            var rootUri = new Uri(BaseUri);
            dicomWebClient.ConfigureServiceUris(rootUri);
            Assert.Throws<ArgumentException>(() => dicomWebClient.ConfigureServicePrefix(serviceType, prefix));
        }

        [Fact(DisplayName = "ConfigureServicePrefix - throws if base address is not configured")]
        public void ConfigureServicePrefix_ThrowsIfBaseAddressIsNotConfigured()
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            var rootUri = new Uri(BaseUri);
            Assert.Throws<InvalidOperationException>(() => dicomWebClient.ConfigureServicePrefix(DicomWebServiceType.Qido, "/prefix"));
        }

        [Theory(DisplayName = "ConfigureServicePrefix - sets service prefix")]
        [InlineData(DicomWebServiceType.Qido, "/qido/")]
        [InlineData(DicomWebServiceType.Wado, "/wado")]
        [InlineData(DicomWebServiceType.Stow, "/stow")]
        public void ConfigureServicePrefix_SetsServicePrefix(DicomWebServiceType serviceType, string prefix)
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            var rootUri = new Uri(BaseUri);

            var exception = Record.Exception(() =>
             {
                 dicomWebClient.ConfigureServiceUris(rootUri);
                 dicomWebClient.ConfigureServicePrefix(serviceType, prefix);
             });

            Assert.Null(exception);
        }

        [Fact(DisplayName = "ConfigureAuthentication - throws if value is null")]
        public void ConfigureAuthentication_ThrowsIfNull()
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);
            var rootUri = new Uri(BaseUri);

            Assert.Throws<ArgumentNullException>(() => dicomWebClient.ConfigureAuthentication(null));
        }

        [Fact(DisplayName = "ConfigureAuthentication - sets auth header")]
        public void ConfigureAuthentication_SetsAuthHeader()
        {
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClientClass(httpClient, null);

            var auth = new AuthenticationHeaderValue("basic", "value");
            dicomWebClient.ConfigureAuthentication(auth);

            Assert.Equal(httpClient.DefaultRequestHeaders.Authorization, auth);
        }
    }
}
