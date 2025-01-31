using System;
using System.IO;
using System.Text.Json;

namespace BIXOLON_SamplePg
{
    public class ConfigManager
    {
        private static ConfigManager _instance;
        private static readonly object _lock = new object();
        public string ApiUrl { get; private set; }
        public string PrinterName { get; private set; }
        public int PrinterId { get; private set; }

        private ConfigManager()
        {
            LoadConfiguration();
        }

        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null) // Double-check locking
                        {
                            _instance = new ConfigManager(); // Initialize the instance
                        }
                    }
                }
                return _instance;
            }
        }

        private void LoadConfiguration()
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            try
            {
                string configContent = File.ReadAllText(configFilePath);
                var config = JsonSerializer.Deserialize<Config>(configContent);
                ApiUrl = config?.api_domain_url ?? throw new Exception("API URL not found in the config file.");
                PrinterName = config?.printer_name ?? throw new Exception("Printer name not found in the config file.");
                PrinterId = config?.printer_id ?? throw new Exception("Printer ID not found in the config file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
                throw;
            }
        }

        private class Config
        {
            public string api_domain_url { get; set; }
            public string printer_name { get; set; }
            public int printer_id { get; set; }
        }
    }
}
