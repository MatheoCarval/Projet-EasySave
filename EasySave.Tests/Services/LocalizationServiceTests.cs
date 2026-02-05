using EasySave.Services;
using Xunit;
using System;

namespace EasySave.Tests.Services
{
    public class LocalizationServiceTests
    {
        [Fact]
        public void Constructor_WithValidLanguage_InitializesCorrectly()
        {
            // Arrange & Act
            var service = new LocalizationService("fr");

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void GetTextTranslated_WithValidKey_ReturnsTranslatedText()
        {
            // Arrange
            var service = new LocalizationService("fr");

            // Act
            var result = service.GetTextTranslated("error");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ChangeLanguage_ToEnglish_UpdatesLanguage()
        {
            // Arrange
            var service = new LocalizationService("fr");

            // Act
            service.ChangeLanguage("en");
            var result = service.GetTextTranslated("error");

            // Assert
            Assert.Equal("Error", result);
        }

        [Fact]
        public void ChangeLanguage_ToFrench_UpdatesLanguage()
        {
            // Arrange
            var service = new LocalizationService("en");

            // Act
            service.ChangeLanguage("fr");
            var result = service.GetTextTranslated("error");

            // Assert
            Assert.Equal("Erreur", result);
        }

        [Fact]
        public void ChangeLanguage_WithInvalidLanguage_ThrowsArgumentException()
        {
            // Arrange
            var service = new LocalizationService("fr");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.ChangeLanguage("es"));
            Assert.Contains("Language 'es' is not available", exception.Message);
        }

        [Fact]
        public void GetAvailableLanguages_ReturnsAllLanguages()
        {
            // Arrange
            var service = new LocalizationService("fr");

            // Act
            var languages = service.GetAvailableLanguages();

            // Assert
            Assert.NotNull(languages);
            Assert.Contains("fr", languages);
            Assert.Contains("en", languages);
        }

        [Theory]
        [InlineData("fr")]
        [InlineData("en")]
        public void Constructor_WithDifferentLanguages_WorksCorrectly(string language)
        {
            // Arrange & Act
            var service = new LocalizationService(language);

            // Assert
            Assert.NotNull(service);
            var text = service.GetTextTranslated("ok");
            Assert.NotNull(text);
        }
    }
}