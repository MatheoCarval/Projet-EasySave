using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace EasySave.Services
{
    /// <summary>
    /// Provides dynamic language translation services for the application, managing multiple language dictionaries and enabling runtime language switching.
    /// </summary>
    public class LocalizationService
    {
        /// <summary>
        /// Gets or sets the current language code (such as 'en' or 'fr') used for translation lookups.
        /// </summary>
        private string CurrentLanguage { get; set; }

        /// <summary>
        /// Stores translation data as a nested dictionary structure mapping language codes to translation key-value pairs.
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> TranslationDatas { get; set; }

        /// <summary>
        /// Initializes LocalizationService with the specified language and loads translation data from embedded resources, falling back to default translations if resource is unavailable.
        /// </summary>
        public LocalizationService(string currentLanguage)
        {
            CurrentLanguage = currentLanguage;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName = "EasySave.Datas.Languages.json";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonContent = reader.ReadToEnd();
                        TranslationDatas = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonContent)
                                            ?? new();
                    }
                }
                else
                {
                    TranslationDatas = new Dictionary<string, Dictionary<string, string>>
                    {
                        ["en"] = new Dictionary<string, string>
                        {
                            ["error"] = "Error",
                            ["ok"] = "OK"
                        },
                        ["fr"] = new Dictionary<string, string>
                        {
                            ["error"] = "Erreur",
                            ["ok"] = "OK"
                        }
                    };
                }
            }
        }

        /// <summary>
        /// Retrieves the translated text for the specified string key in the current language.
        /// </summary>
        public string GetTextTranslated(string stringToTranslate)
        {
            return TranslationDatas[CurrentLanguage][stringToTranslate];
        }

        /// <summary>
        /// Changes the current language to the specified language code if it is available; throws ArgumentException if the language is not found.
        /// </summary>
        public void ChangeLanguage(string newLanguage)
        {
            if (!TranslationDatas.ContainsKey(newLanguage))
            {
                throw new ArgumentException($"Language '{newLanguage}' is not available.");
            }

            CurrentLanguage = newLanguage;
        }
        /// <summary>
        /// Returns an array of all available language codes in the application settings.
        /// </summary>
        public string[] GetAvailableLanguages()
        {
            return TranslationDatas.Keys.ToArray();
        }

        /// <summary>
        /// Represents the translation data structure for deserializing language and translation key-value pairs from JSON.
        /// </summary>
        private class Translation()
        {
            public Dictionary<string, Dictionary<string, string>> Languages { get; set; } = new();
        }
    }

}