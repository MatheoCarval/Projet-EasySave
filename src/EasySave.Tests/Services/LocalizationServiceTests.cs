using EasySave.Services;
using Xunit;
using System;

namespace EasySave.Tests.Services
{
    /// <summary>
    /// Unit tests for the LocalizationService class, verifying language initialization, translation retrieval, language switching, and error handling.
    /// </summary>
    public class LocalizationServiceTests
    {
        /// <summary>
        /// Verifies that LocalizationService constructor with a valid language initializes successfully.
        /// </summary>
        [Fact]
        public void Constructor_WithValidLanguage_InitializesCorrectly()
        {
            var service = new LocalizationService("fr");

            Assert.NotNull(service);
        }

        /// <summary>
        /// Verifies that GetTextTranslated returns non-empty translated text for a valid translation key.
        /// </summary>
        [Fact]
        public void GetTextTranslated_WithValidKey_ReturnsTranslatedText()
        {
            var service = new LocalizationService("fr");

            var result = service.GetTextTranslated("error");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        /// <summary>
        /// Verifies that ChangeLanguage successfully switches from French to English and returns correct English translation.
        /// </summary>
        [Fact]
        public void ChangeLanguage_ToEnglish_UpdatesLanguage()
        {
            var service = new LocalizationService("fr");

            service.ChangeLanguage("en");
            var result = service.GetTextTranslated("error");

            Assert.Equal("Error", result);
        }

        /// <summary>
        /// Verifies that ChangeLanguage successfully switches from English to French and returns correct French translation.
        /// </summary>
        [Fact]
        public void ChangeLanguage_ToFrench_UpdatesLanguage()
        {
            var service = new LocalizationService("en");

            service.ChangeLanguage("fr");
            var result = service.GetTextTranslated("error");

            Assert.Equal("Erreur", result);
        }

        /// <summary>
        /// Verifies that ChangeLanguage throws ArgumentException when given an unsupported language code.
        /// </summary>
        [Fact]
        public void ChangeLanguage_WithInvalidLanguage_ThrowsArgumentException()
        {
            var service = new LocalizationService("fr");

            var exception = Assert.Throws<ArgumentException>(() => service.ChangeLanguage("es"));
            Assert.Contains("Language 'es' is not available", exception.Message);
        }

        /// <summary>
        /// Verifies that GetAvailableLanguages returns a collection containing both English and French language codes.
        /// </summary>
        [Fact]
        public void GetAvailableLanguages_ReturnsAllLanguages()
        {
            var service = new LocalizationService("fr");

            var languages = service.GetAvailableLanguages();

            Assert.NotNull(languages);
            Assert.Contains("fr", languages);
            Assert.Contains("en", languages);
        }

        /// <summary>
        /// Verifies that LocalizationService constructor initializes correctly with different language codes and retrieves translations successfully.
        /// </summary>
        [Theory]
        [InlineData("fr")]
        [InlineData("en")]
        public void Constructor_WithDifferentLanguages_WorksCorrectly(string language)
        {
            var service = new LocalizationService(language);

            Assert.NotNull(service);
            var text = service.GetTextTranslated("ok");
            Assert.NotNull(text);
        }
    }
}