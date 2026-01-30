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
    internal class LocalizationService
    {
        private string CurrentLanguage { get; set; }
        private Dictionary<string, Dictionary<string, string>> TranslationDatas { get; set; }

        // When class is initialized
        public LocalizationService(string currentLanguage)
        {
            // Retrieve all translation data
            CurrentLanguage = currentLanguage;

            // Get the path of the local JSON storage file (AppData)
            string jsonLocalPath = "C:/Users/guill/source/repos/Projet-EasySave/Datas/Languages.json";

            // Retrieve available languages from the JSON file (main sections)
            string readJsonContent = File.ReadAllText(jsonLocalPath);

            // To treat json datas as objects C#
            TranslationDatas = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(readJsonContent)
                               ?? new();
        }

        // To get the text wanted with good translation
        public string GetTextTranslated(string stringToTranslate)
        {

            // According to the current language and the desired text,
            // go to the "en" or "fr" section, then to the corresponding index
            return TranslationDatas[CurrentLanguage][stringToTranslate];
        }

        // To Update the language Setting
        public void ChangeLanguage(string stringToTranslate)
        {
            // Get the path of the local JSON storage file (AppData)            

            // Update the language in the appropriate JSON section

            // Reset the console menu
        }

        public string[] GetAvailableLanguages()
        {
            // Retrieve available languages as an array
            return TranslationDatas.Keys.ToArray();
        }

        // Class translation to get datas with json formate
        private class Translation()
        {
            // Key = language code ("en", "fr", ...)
            // Value = dictionary of texts ("text-1" => "...")
            public Dictionary<string, Dictionary<string, string>> Languages { get; set; } = new();
        }
    }

}
