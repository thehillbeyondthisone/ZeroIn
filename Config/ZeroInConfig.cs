using AOSharp.Common.GameData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZeroIn.Config
{
    public class ZeroInConfig
    {
        // Scan settings
        public float ScanSpacing { get; set; } = 40f; // Distance between scan lines (40m with 50m detection = 10m overlap)
        public float PlayerDetectionRange { get; set; } = 50f; // Game's player detection range
        public float MovementSpeed { get; set; } = 1f; // Movement speed multiplier
        public bool ContinuousScanning { get; set; } = false; // Loop scan continuously
        public int ScanDelayMs { get; set; } = 100; // Delay between character scans in milliseconds

        // Zone areas
        public List<ZoneArea> ZoneAreas { get; set; } = new List<ZoneArea>();
        public int CurrentAreaIndex { get; set; } = 0;

        // Output settings
        public bool LogToConsole { get; set; } = true;
        public bool SaveToJson { get; set; } = true;
        public bool SaveToCsv { get; set; } = true;
        public string OutputFolder { get; set; } = "ScanResults";

        // Detection filters
        public bool OnlyAFK { get; set; } = false; // Only log characters that haven't moved
        public int AFKCheckTimeSeconds { get; set; } = 30; // How long to watch for movement
        public bool IgnoreSelf { get; set; } = true;
        public List<string> IgnoreNames { get; set; } = new List<string>();

        public ZeroInConfig()
        {
            // Add a default example area
            ZoneAreas.Add(new ZoneArea(
                "Example Zone",
                new Vector3(100, 100, 0),
                new Vector3(100, 200, 0),
                new Vector3(200, 200, 0),
                new Vector3(200, 100, 0)
            ));
        }

        public static ZeroInConfig Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    var config = new ZeroInConfig();
                    config.Save(path);
                    return config;
                }

                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ZeroInConfig>(json) ?? new ZeroInConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error loading config: {ex.Message}");
                return new ZeroInConfig();
            }
        }

        public void Save(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error saving config: {ex.Message}");
            }
        }

        public ZoneArea GetCurrentArea()
        {
            if (ZoneAreas.Count == 0 || CurrentAreaIndex >= ZoneAreas.Count)
                return null;

            return ZoneAreas[CurrentAreaIndex];
        }
    }
}
