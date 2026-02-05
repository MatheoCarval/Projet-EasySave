using EasyLog.Enums;
using Xunit;

namespace EasySave.Tests.EasyLog.Enums
{
    /// <summary>
    /// Unit tests for the LogFormat enumeration, verifying all enum values are defined and accessible.
    /// </summary>
    public class LogFormatTests
    {
        /// <summary>
        /// Verifies that the LogFormat enumeration contains the JSON value.
        /// </summary>
        [Fact]
        public void LogFormat_HasJsonValue()
        {
            var format = LogFormat.JSON;

            Assert.Equal(LogFormat.JSON, format);
        }

        /// <summary>
        /// Verifies that the LogFormat enumeration contains the XML value.
        /// </summary>
        [Fact]
        public void LogFormat_HasXmlValue()
        {
            var format = LogFormat.XML;

            Assert.Equal(LogFormat.XML, format);
        }

        /// <summary>
        /// Verifies that all LogFormat enumeration values are valid and defined.
        /// </summary>
        [Theory]
        [InlineData(LogFormat.JSON)]
        [InlineData(LogFormat.XML)]
        public void LogFormat_AllValuesAreValid(LogFormat format)
        {
            var isDefined = Enum.IsDefined(typeof(LogFormat), format);

            Assert.True(isDefined);
        }
    }
}
