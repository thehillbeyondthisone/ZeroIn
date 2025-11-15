using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZeroIn.Config;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Manages output and logging of scan results
    /// </summary>
    public class ScanMap
    {
        private ZeroInConfig _config;
        private string _outputPath;

        public ScanMap(ZeroInConfig config)
        {
            _config = config;
            _outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AOSharp",
                "AOSP",
                "ZeroIn",
                config.OutputFolder
            );

            // Create output directory
            if (!Directory.Exists(_outputPath))
                Directory.CreateDirectory(_outputPath);
        }

        /// <summary>
        /// Saves scan results to configured output formats
        /// </summary>
        public void SaveResults(List<DetectedCharacter> characters, string areaName)
        {
            if (characters == null || characters.Count == 0)
            {
                Console.WriteLine("[ZeroIn] No characters to save");
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseName = $"{SanitizeFileName(areaName)}_{timestamp}";

            try
            {
                if (_config.SaveToJson)
                {
                    SaveJson(characters, baseName);
                }

                if (_config.SaveToCsv)
                {
                    SaveCsv(characters, baseName);
                }

                // Always save a summary
                SaveSummary(characters, baseName);

                Console.WriteLine($"[ZeroIn] Results saved to: {_outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error saving results: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves results as JSON
        /// </summary>
        private void SaveJson(List<DetectedCharacter> characters, string baseName)
        {
            string filePath = Path.Combine(_outputPath, $"{baseName}.json");
            string json = JsonConvert.SerializeObject(characters, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"[ZeroIn] Saved JSON: {filePath}");
        }

        /// <summary>
        /// Saves results as CSV
        /// </summary>
        private void SaveCsv(List<DetectedCharacter> characters, string baseName)
        {
            string filePath = Path.Combine(_outputPath, $"{baseName}.csv");
            var sb = new StringBuilder();

            // Header
            sb.AppendLine(DetectedCharacter.GetCsvHeader());

            // Data rows
            foreach (var character in characters.OrderBy(c => c.Name))
            {
                sb.AppendLine(character.ToCsv());
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"[ZeroIn] Saved CSV: {filePath}");
        }

        /// <summary>
        /// Saves a text summary
        /// </summary>
        private void SaveSummary(List<DetectedCharacter> characters, string baseName)
        {
            string filePath = Path.Combine(_outputPath, $"{baseName}_summary.txt");
            var sb = new StringBuilder();

            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($" ZeroIn Scan Results - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();
            sb.AppendLine($"Total Characters Detected: {characters.Count}");
            sb.AppendLine($"AFK Characters: {characters.Count(c => !c.HasMoved())}");
            sb.AppendLine($"Moving Characters: {characters.Count(c => c.HasMoved())}");
            sb.AppendLine();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine(" Character List");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            foreach (var character in characters.OrderBy(c => c.Name))
            {
                sb.AppendLine($"[{(character.HasMoved() ? "MOVING" : "AFK   ")}] {character}");
                sb.AppendLine($"           First Seen: {character.FirstSeen:HH:mm:ss}");
                sb.AppendLine($"           Last Seen:  {character.LastSeen:HH:mm:ss}");
                sb.AppendLine($"           Spotted:    {character.TimesSpotted} times");
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"[ZeroIn] Saved summary: {filePath}");
        }

        /// <summary>
        /// Prints results to console
        /// </summary>
        public void PrintResults(List<DetectedCharacter> characters)
        {
            if (characters == null || characters.Count == 0)
            {
                Console.WriteLine("[ZeroIn] No characters detected");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" Scan Complete - {characters.Count} Characters Found");
            Console.WriteLine("=".PadRight(80, '='));

            var afkChars = characters.Where(c => !c.HasMoved()).ToList();
            var movingChars = characters.Where(c => c.HasMoved()).ToList();

            if (afkChars.Count > 0)
            {
                Console.WriteLine($"\nAFK Characters ({afkChars.Count}):");
                foreach (var character in afkChars.OrderBy(c => c.Name))
                {
                    Console.WriteLine($"  {character}");
                }
            }

            if (movingChars.Count > 0)
            {
                Console.WriteLine($"\nMoving Characters ({movingChars.Count}):");
                foreach (var character in movingChars.OrderBy(c => c.Name))
                {
                    Console.WriteLine($"  {character}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Sanitizes a filename
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
