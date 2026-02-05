using EasyLog.Enums;
using Xunit;

namespace EasySave.Tests.EasyLog.Enums
{
    public class LogFormatTests
    {
        [Fact]
        public void LogFormat_HasJsonValue()
        {
            // Arrange & Act
            var format = LogFormat.JSON;

            // Assert
            Assert.Equal(LogFormat.JSON, format);
        }

        [Fact]
        public void LogFormat_HasXmlValue()
        {
            // Arrange & Act
            var format = LogFormat.XML;

            // Assert
            Assert.Equal(LogFormat.XML, format);
        }

        [Theory]
        [InlineData(LogFormat.JSON)]
        [InlineData(LogFormat.XML)]
        public void LogFormat_AllValuesAreValid(LogFormat format)
        {
            // Arrange & Act
            var isDefined = Enum.IsDefined(typeof(LogFormat), format);

            // Assert
            Assert.True(isDefined);
        }
    }
}
