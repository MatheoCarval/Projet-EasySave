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
            // Retrieve all translation data
            CurrentLanguage = currentLanguage;

            // Get the path of the local JSON storage file (AppData)
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string jsonLocalPath = Path.Combine(appDataFolder, "localization.json");

            // Initialize the localization file if it doesn't exist
            InitializeLocalizationFile(appDataFolder, jsonLocalPath);

            // Retrieve available languages from the JSON file (main sections)
            string readJsonContent = File.ReadAllText(jsonLocalPath);

            // To treat json datas as objects C#
            TranslationDatas = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(readJsonContent)
                                ?? new();
        }

        /// <summary>
        /// Initialize the localization file from embedded resource or project data
        /// </summary>
        /// <param name="appDataFolder"></param>
        /// <param name="jsonLocalPath"></param>
        private void InitializeLocalizationFile(string appDataFolder, string jsonLocalPath)
        {
            // Check if the localization file already exists
            if (File.Exists(jsonLocalPath))
            {
                return;
            }

            // Create the EasySave directory in AppData if it doesn't exist
            Directory.CreateDirectory(appDataFolder);

            // Extract the embedded Languages.json resource
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName = "EasySave.Datas.Languages.json";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var fileStream = File.Create(jsonLocalPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                else
                {
                    // Fallback: create a minimal default file if resource doesn't exist
                    var defaultTranslations = new Dictionary<string, Dictionary<string, string>>
                    {
                        ["en"] = new Dictionary<string, string>
                        {
                            ["text-1"] = "This is the text 1",
                            ["text-2"] = "This is the text 2"
                        },
                        ["fr"] = new Dictionary<string, string>
                        {
                            ["text-1"] = "C'est le texte 1",
                            ["text-2"] = "C'est le texte 2"
                        }
                    };

                    string jsonContent = JsonSerializer.Serialize(defaultTranslations, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(jsonLocalPath, jsonContent);
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