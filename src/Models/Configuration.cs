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
        private int MaxBackupJobs { get; set; }
        private string LogFilePath { get; set; }
        private string StateFilePath { get; set; }
        private bool DarkMode { get; set; }

        public Configuration()
        {
            Language = "fr-FR";
            LogFormat = LogFormat.JSON;
            MaxBackupJobs = int.MaxValue;
            LogFilePath = string.Empty;
            StateFilePath = string.Empty;
            DarkMode = false;
        }

        // Getters
        public string GetLanguage() => Language;
        public LogFormat GetLogFormat() => LogFormat;
        public int GetMaxBackupJobs() => MaxBackupJobs;
        public string GetLogFilePath() => LogFilePath;
        public string GetStateFilePath() => StateFilePath;
        public bool GetDarkMode() => DarkMode;

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

        public void SetMaxBackupJobs(int maxJobs)
        {
            if (maxJobs <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxJobs), "MaxBackupJobs must be greater than 0");
            MaxBackupJobs = maxJobs;
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

        public void SetDarkMode(bool darkMode)
        {
            DarkMode = darkMode;
        }


        /// <summary>
        ///  Selon LogFormat récupérer le chemin du fichier log avec .json ou .xml
        ///  Si inexistant, il le crée
        /// </summary>
        /// <returns></returns>
        public string GetDefaultLogPath()
        {
            // Détermine le nom du fichier selon le format de log
            string logFileName = LogFormat switch
            {
                LogFormat.JSON => "jobs.json",
                LogFormat.XML => "jobs.xml",
                _ => "jobs.log"
            };

            // Concatène le chemin du dossier AppData/Roaming/EasySave avec le nom du fichier log
            string logFilePath = Path.Combine(GetAppDataPath(), logFileName);

            // Si le fichier n'existe pas, le créer (fichier vide)
            if (!File.Exists(logFilePath))
            {
                // Construit le contenu par défaut du fichier log selon le format de log
                string logFileDefaultContent = LogFormat switch
                {
                    LogFormat.JSON => "{}",
                    LogFormat.XML => "<logs></logs>",
                    _ => ""
                };

                // Si le dossier AppData/Roaming/EasySave n'existe pas, le créer
                if (!Directory.Exists(GetAppDataPath()))
                {
                    Directory.CreateDirectory(GetAppDataPath());
                }

                // Écrit le contenu par défault dans le fichier log
                File.WriteAllText(logFilePath, logFileDefaultContent);
            }

            return logFilePath;
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
                        config.MaxBackupJobs > 0 &&
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
                MaxBackupJobs = 5,
                LogFilePath = logPath,
                StateFilePath = statePath,
                DarkMode = false
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
            public int MaxBackupJobs { get; set; }
            public string LogFilePath { get; set; } = string.Empty;
            public string StateFilePath { get; set; } = string.Empty;
            public bool DarkMode { get; set; }
        }
    }

}