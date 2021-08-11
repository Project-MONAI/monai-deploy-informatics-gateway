using Monai.Deploy.InformaticsGateway.Services.Common;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class AuthenticationHeaderValueExtensionsTest
    {
        [Fact(DisplayName = "ConvertFrom - Basic")]
        public void ConvertFromBasic()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.Basic, "test");
            Assert.Equal("Basic", result.Scheme);
            Assert.Equal("test", result.Parameter);
        }

        [Fact(DisplayName = "ConvertFrom - Bearer")]
        public void ConvertFromBearer()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.Bearer, "test");
            Assert.Equal("Bearer", result.Scheme);
            Assert.Equal("test", result.Parameter);
        }

        [Fact(DisplayName = "ConvertFrom - None")]
        public void ConvertFromNone()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.None, "test");
            Assert.Null(result);
        }
    }
}