using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api
{
    public class LoggingDataDictionaryTest
    {
        [Fact(DisplayName = "ToString")]
        public void ToStringOverride()
        {
            var input = new LoggingDataDictionary<string, string>();
            input.Add("A", "1");
            input.Add("B", "2");
            input.Add("C", "3");

            Assert.Equal("A=1, B=2, C=3", input.ToString());
        }
    }
}
