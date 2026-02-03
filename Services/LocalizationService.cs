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
    /// LocalizationService Class : Translation dynamic service usable in public in the project
    /// </summary>
    internal class LocalizationService
    {
        /// <summary>
        /// Current Language selected
        /// </summary>
        private string CurrentLanguage { get; set; }

        /// <summary>
        /// Translation Datas List
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> TranslationDatas { get; set; }

        /// <summary>
        /// When class is initialized
        /// </summary>
        /// <param name="currentLanguage"></param>
        public LocalizationService(string currentLanguage)
        {
            CurrentLanguage = currentLanguage;

            // Load translations directly from embedded resource
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
                    // Fallback: create minimal default translations if resource not found
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
        /// To get the text wanted with good translation
        /// </summary>
        /// <param name="stringToTranslate"></param>
        /// <returns></returns>
        public string GetTextTranslated(string stringToTranslate)
        {

            // According to the current language and the desired text,
            // go to the "en" or "fr" section, then to the corresponding index
            return TranslationDatas[CurrentLanguage][stringToTranslate];
        }

        /// <summary>
        /// To Update the language Setting
        /// </summary>
        /// <param name="newLanguage"></param>
        public void ChangeLanguage(string newLanguage)
        {
            // Check if the requested language exists
            if (!TranslationDatas.ContainsKey(newLanguage))
            {
                throw new ArgumentException($"Language '{newLanguage}' is not available.");
            }

            // Update the current language
            CurrentLanguage = newLanguage;
        }
        /// <summary>
        /// To get available languagues List in App Settings
        /// </summary>
        /// <returns></returns>
        public string[] GetAvailableLanguages()
        {
            // Retrieve available languages as an array
            return TranslationDatas.Keys.ToArray();
        }

        /// <summary>
        /// Class translation to get datas with json formate
        /// </summary>
        private class Translation()
        {
            // Key = language code ("en", "fr", ...)
            // Value = dictionary of texts ("text-1" => "...")
            public Dictionary<string, Dictionary<string, string>> Languages { get; set; } = new();
        }
    }

}