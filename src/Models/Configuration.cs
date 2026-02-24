using EasyLog.Enums;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EasySave.Models
{
    internal class Configuration
    {
        private string Language { get; set; }
        private LogFormat LogFormat { get; set; }
        private List<string> BlockedApplications { get; set; }
        private string LogFilePath { get; set; }
        private string StateFilePath { get; set; }
        private string CryptosoftPath { get; set; }
        private string CryptosoftPublicKey { get; set; }
        private List<string> EncryptedExtensions { get; set; }
        private bool DarkMode { get; set; }
        private bool OnboardingCompleted { get; set; }
        private string AccentColor { get; set; }

        public Configuration()
        {
            Language = "fr-FR";
            LogFormat = LogFormat.JSON;
            BlockedApplications = new List<string>();
            LogFilePath = string.Empty;
            StateFilePath = string.Empty;
            CryptosoftPath = string.Empty;
            CryptosoftPublicKey = string.Empty;
            EncryptedExtensions = new List<string>();
            DarkMode = false;
            OnboardingCompleted = false;
            AccentColor = "Blue";
        }

        // Getters
        public string GetLanguage() => Language;
        public LogFormat GetLogFormat() => LogFormat;
        public List<string> GetBlockedApplications() => new List<string>(BlockedApplications);
        public string GetLogFilePath() => LogFilePath;
        public string GetStateFilePath() => StateFilePath;
        public string GetCryptosoftPath() => CryptosoftPath;
        public string GetCryptosoftPublicKey() => CryptosoftPublicKey;
        public List<string> GetEncryptedExtensions() => new List<string>(EncryptedExtensions);
        public bool GetDarkMode() => DarkMode;
        public bool GetOnboardingCompleted() => OnboardingCompleted;
        public string GetAccentColor() => AccentColor;

        // Setters
        public void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));
            Language = language;
        }

        public void SetLogFormat(LogFormat logFormat)
        {
            LogFormat = logFormat;
        }

        public void SetBlockedApplications(IEnumerable<string> blockedApplications)
        {
            if (blockedApplications == null)
            {
                BlockedApplications = new List<string>();
                return;
            }

            BlockedApplications = blockedApplications
                .Where(app => !string.IsNullOrWhiteSpace(app))
                .Select(app => app.Trim())
                .ToList();
        }

        public void SetLogFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("LogFilePath cannot be null or empty", nameof(path));
            LogFilePath = path;
        }

        public void SetStateFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("StateFilePath cannot be null or empty", nameof(path));
            StateFilePath = path;
        }

        public void SetCryptosoftPath(string path)
        {
            CryptosoftPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();
        }

        public void SetCryptosoftPublicKey(string path)
        {
            CryptosoftPublicKey = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();
        }

        public void SetEncryptedExtensions(IEnumerable<string> extensions)
        {
            if (extensions == null)
            {
                EncryptedExtensions = new List<string>();
                return;
            }

            EncryptedExtensions = extensions
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .Select(NormalizeExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void SetDarkMode(bool darkMode)
        {
            DarkMode = darkMode;
        }

        public void SetOnboardingCompleted(bool completed)
        {
            OnboardingCompleted = completed;
        }

        public void SetAccentColor(string color)
        {
            AccentColor = string.IsNullOrWhiteSpace(color) ? "Blue" : color.Trim();
        }

        private static string NormalizeExtension(string extension)
        {
            string trimmed = extension.Trim();
            if (!trimmed.StartsWith("."))
            {
                trimmed = "." + trimmed;
            }

            return trimmed.ToLowerInvariant();
        }


        /// <summary>
        /// Retourne le chemin du dossier de logs par défaut.
        /// Le dossier est créé s'il n'existe pas.
        /// </summary>
        /// <returns>Chemin du dossier logs (ex: AppData/EasySave/logs)</returns>
        public string GetDefaultLogPath()
        {
            // Crée le chemin vers le sous-dossier logs
            string logsDirectory = Path.Combine(GetAppDataPath(), "logs");

            // Si le dossier logs n'existe pas, le créer
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            return logsDirectory;
        }

        /// <summary>
        /// Vérifie que le configJson existe, sinon le crée par défault
        /// Vérifie qu'il ne contienne pas d'erreur structurelle
        /// Retourne true si le contenu est bon et false si le contenu a été remplacé par la structure par défaut
        /// </summary>
        public bool VerifyConfigJson()
        {
            // Récupère le chemin du fichier Config.json
            string configPath = GetConfigPath();

            // Récupère le contenu par défaut de Config.json
            string defaultConfigContent = GetDefaultConfigContent();

            // Si le dossier AppData/Roaming/EasySave n'existe pas, le créer
            if (!Directory.Exists(GetAppDataPath()))
            {
                Directory.CreateDirectory(GetAppDataPath());
            }

            // Si le fichier n'existe pas, le créer avec la structure par défaut
            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, defaultConfigContent);
                return true;
            }
            else // Si le fichier existe, charge le contenu et vérifie la structure
            {
                try
                {
                    // Lit le contenu du fichier de configuration
                    string json = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ConfigurationTemplate>(json);

                    // Vérifie que tous les champs sont présents et valides
                    if (config is not null &&
                        !string.IsNullOrWhiteSpace(config.Language) &&
                        Enum.TryParse<LogFormat>(config.LogFormat, out _) &&
                        !string.IsNullOrWhiteSpace(config.LogFilePath) &&
                        !string.IsNullOrWhiteSpace(config.StateFilePath))
                    {
                        return true;
                    }
                    else
                    {
                        // TODO : Ajouter un log d'erreur pour indiquer que la structure du fichier de configuration est invalide

                        // Structure invalide, remplace par la structure par défaut
                        File.WriteAllText(configPath, defaultConfigContent);

                        return false;
                    }
                }
                catch
                {
                    // TODO : Ajouter un log d'erreur pour indiquer que le fichier de configuration est corrompu ou illisible

                    // Erreur de lecture ou de parsing, remplace par la structure par défaut
                    File.WriteAllText(configPath, defaultConfigContent);

                    return false;
                }
            }
        }

        /// <summary>
        /// Récupérer le contenu par défaut du fichier Config.json
        /// </summary>
        /// <returns></returns>
        private string GetDefaultConfigContent()
        {
            // Récupère le chemin du fichier Config.json
            string configPath = GetConfigPath();
            // Récupère le chemin du fichier log selon le format de log
            string logPath = GetDefaultLogPath();

            // Construit un objet de configuration par défaut avec des valeurs prédéfinies
            string statePath = Path.Combine(GetAppDataPath(), "state.json");
            var configurationTemplate = new ConfigurationTemplate()
            {
                Language = "fr-FR",
                LogFormat = LogFormat == LogFormat.JSON ? "JSON" : "XML",
                BlockedApplications = new List<string>(),
                LogFilePath = logPath,
                StateFilePath = statePath,
                CryptosoftPath = string.Empty,
                CryptosoftPublicKey = string.Empty,
                EncryptedExtensions = new List<string>(),
                DarkMode = false,
                OnboardingCompleted = false,
                AccentColor = "Blue"
            };

            // Retourne la sérialisation de la configuration par défaut au format JSON
            return System.Text.Json.JsonSerializer.Serialize(configurationTemplate);
        }

        /// <summary>
        /// Récupérer le chemin du fichier Config.json dans AppData/Roaming/EasySave
        /// </summary>
        /// <returns></returns>
        private string GetConfigPath()
        {
            return Path.Combine(GetAppDataPath(), "Config.json");
        }

        /// <summary>
        /// Récupérer le chemin du dossier AppData/Roaming/EasySave
        /// </summary>
        /// <returns></returns>
        private string GetAppDataPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            const string EasySaveDir = "EasySave";
            string configPath = Path.Combine(appDataPath, EasySaveDir);
            return configPath;
        }

        /// <summary>
        /// Classe temporaire pour la désérialisation
        /// </summary>
        private class ConfigurationTemplate
        {
            public string Language { get; set; } = string.Empty;
            public string LogFormat { get; set; } = string.Empty;
            public List<string> BlockedApplications { get; set; } = new();
            public string LogFilePath { get; set; } = string.Empty;
            public string StateFilePath { get; set; } = string.Empty;
            public string CryptosoftPath { get; set; } = string.Empty;
            public string CryptosoftPublicKey { get; set; } = string.Empty;
            public List<string> EncryptedExtensions { get; set; } = new();
            public bool DarkMode { get; set; }
            public bool OnboardingCompleted { get; set; }
            public string AccentColor { get; set; } = "Blue";
        }
    }

}