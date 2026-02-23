using EasyLog.Enums;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Services.Managers
{
    internal class ConfigurationManager
    {
        private Configuration? _currentConfig;
        private static ConfigurationManager? _instance;
        private static readonly object _lock = new object();

        private ConfigurationManager()
        {
            _currentConfig = null;
        }

        public static ConfigurationManager GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ConfigurationManager();
                    }
                }
            }
            return _instance;
        }

        public Configuration LoadConfiguration()
        {
            if (_currentConfig == null)
            {
                _currentConfig = new Configuration();
                _currentConfig.VerifyConfigJson();

                // Load configuration from file
                string configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configTemplate = JsonSerializer.Deserialize<ConfigurationTemplate>(json);

                    if (configTemplate != null)
                    {
                        // Update internal configuration with loaded values
                        _currentConfig = CreateConfigurationFromTemplate(configTemplate);
                    }
                }
            }

            return _currentConfig;
        }

        public void SaveConfiguration(Configuration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string configPath = GetConfigPath();

            var configTemplate = new ConfigurationTemplate
            {
                Language = config.GetLanguage(),
                LogFormat = config.GetLogFormat().ToString(),
                BlockedApplications = config.GetBlockedApplications(),
                LogFilePath = config.GetLogFilePath(),
                StateFilePath = config.GetStateFilePath(),
                CryptosoftPath = config.GetCryptosoftPath(),
                CryptosoftPublicKey = config.GetCryptosoftPublicKey(),
                EncryptedExtensions = config.GetEncryptedExtensions(),
                PriorityExtensions = config.GetPriorityExtensions(),
                DarkMode = config.GetDarkMode(),
                MaxParallelTransferSizeValue = config.GetMaxParallelTransferSizeValue(),
                MaxParallelTransferSizeUnit = config.GetMaxParallelTransferSizeUnit()
            };

            string json = JsonSerializer.Serialize(configTemplate, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
            _currentConfig = config;
        }

        public void UpdateLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            var config = LoadConfiguration();
            config.SetLanguage(language);
            SaveConfiguration(config);
        }

        public void UpdateLogFormat(LogFormat logFormat)
        {
            var config = LoadConfiguration();
            config.SetLogFormat(logFormat);
            SaveConfiguration(config);
        }

        private Configuration CreateConfigurationFromTemplate(ConfigurationTemplate template)
        {
            var config = new Configuration();
            config.SetLanguage(template.Language);

            if (Enum.TryParse<LogFormat>(template.LogFormat, out var logFormat))
            {
                config.SetLogFormat(logFormat);
            }

            config.SetBlockedApplications(template.BlockedApplications ?? new List<string>());

            config.SetLogFilePath(template.LogFilePath);
            config.SetStateFilePath(template.StateFilePath);
            config.SetCryptosoftPath(template.CryptosoftPath ?? string.Empty);
            config.SetCryptosoftPublicKey(template.CryptosoftPublicKey ?? string.Empty);
            config.SetEncryptedExtensions(template.EncryptedExtensions ?? new List<string>());
            config.SetPriorityExtensions(template.PriorityExtensions ?? new List<string>());
            config.SetDarkMode(template.DarkMode);
            config.SetMaxParallelTransferSize(template.MaxParallelTransferSizeValue, template.MaxParallelTransferSizeUnit ?? "GB");

            return config;
        }

        private string GetConfigPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string easySaveDir = Path.Combine(appDataPath, "EasySave");

            if (!Directory.Exists(easySaveDir))
            {
                Directory.CreateDirectory(easySaveDir);
            }

            return Path.Combine(easySaveDir, "Config.json");
        }

        private class ConfigurationTemplate
        {
            public string Language { get; set; } = string.Empty;
            public string LogFormat { get; set; } = string.Empty;
            public List<string>? BlockedApplications { get; set; }
            public string LogFilePath { get; set; } = string.Empty;
            public string StateFilePath { get; set; } = string.Empty;
            public string? CryptosoftPath { get; set; }
            public string? CryptosoftPublicKey { get; set; }
            public List<string>? EncryptedExtensions { get; set; }
            public List<string>? PriorityExtensions { get; set; }
            public bool DarkMode { get; set; }
            public long MaxParallelTransferSizeValue { get; set; }
            public string? MaxParallelTransferSizeUnit { get; set; }
        }
    }
}