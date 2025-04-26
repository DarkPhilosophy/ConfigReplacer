using System;
using System.Collections.Generic;

namespace ConfigReplacer
{
    public class AppConfig : Common.BaseConfig
    {
        public List<string> ConfigFilePaths { get; set; } = new List<string>();
        public string OldString { get; set; } = "FFTesterBER";
        public string NewString { get; set; } = "FFTesterSCH";

        /// <summary>
        /// Static constructor to initialize the configuration manager
        /// </summary>
        static AppConfig()
        {
            // Initialize the configuration manager to use LocalApplicationData with shared settings
            // Both applications will use the same settings.json file
            InitializeConfigManager(
                Common.ConfigStorageLocation.LocalApplicationData,
                "settings.json",
                "Flex",
                "FlexTools");
        }

        /// <summary>
        /// Loads the configuration from the configuration file
        /// </summary>
        /// <returns>The loaded configuration</returns>
        public static AppConfig Load()
        {
            // Create a default configuration to use if loading fails
            var defaultConfig = new AppConfig
            {
                ConfigFilePaths = new List<string>
                {
                    @"C:\cpi\config\ViTrox.WS.FlexRomania.PostData\config.json",
                    @"C:\cpi\config\MESScriptComm\Vitrox.MES.Plugin\config.json"
                },
                OldString = "FFTesterBER",
                NewString = "FFTesterSCH",
                Language = "Romanian"
            };

            // Load the configuration using the configuration manager
            return ConfigManager.Load(defaultConfig);
        }
    }
}
