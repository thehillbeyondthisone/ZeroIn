using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Manages output and logging of scan results
    /// </summary>
    public class ScanMap
    {
        private readonly string _outputPath;

        public ScanMap(string basePath)
        {
            _outputPath = Path.Combine(basePath, "ScanResults");

            // Create output directory
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }

        /// <summary>
        /// Saves detected characters to files
        /// </summary>
        public void SaveResults(List<DetectedCharacter> characters, string areaName)
        {
            if (characters == null || characters.Count == 0)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string safeAreaName = MakeSafeFilename(areaName);

            // Save as JSON
            SaveAsJson(characters, safeAreaName, timestamp);

            // Save as CSV
            SaveAsCsv(characters, safeAreaName, timestamp);

            // Save summary text
            SaveAsSummary(characters, safeAreaName, timestamp);
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

            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" ZeroIn Scan Results - {characters.Count} characters detected");
            Console.WriteLine("=".PadRight(80, '='));

            foreach (var character in characters.OrderBy(c => c.Name))
            {
                var age = (DateTime.UtcNow - character.LastSeen).TotalSeconds;
                Console.WriteLine($"  {character.Name}");
                Console.WriteLine($"    Position: ({character.PositionX:F1}, {character.PositionY:F1}, {character.PositionZ:F1})");
                Console.WriteLine($"    Distance: {character.Distance:F1}m");
                Console.WriteLine($"    Times Spotted: {character.TimesSpotted}");
                Console.WriteLine($"    Last Seen: {age:F0}s ago");
                Console.WriteLine();
            }
        }

        private void SaveAsJson(List<DetectedCharacter> characters, string areaName, string timestamp)
        {
            try
            {
                string filename = $"scan_{areaName}_{timestamp}.json";
                string path = Path.Combine(_outputPath, filename);

                var json = JsonConvert.SerializeObject(characters, Formatting.Indented);
                File.WriteAllText(path, json);

                Console.WriteLine($"[ZeroIn] Saved JSON: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error saving JSON: {ex.Message}");
            }
        }

        private void SaveAsCsv(List<DetectedCharacter> characters, string areaName, string timestamp)
        {
            try
            {
                string filename = $"scan_{areaName}_{timestamp}.csv";
                string path = Path.Combine(_outputPath, filename);

                var sb = new StringBuilder();
                sb.AppendLine("CharId,Name,TimesSpotted,PositionX,PositionY,PositionZ,Distance,Health,FirstSeen,LastSeen");

                foreach (var character in characters.OrderBy(c => c.Name))
                {
                    sb.AppendLine($"{character.CharId},{EscapeCsv(character.Name)},{character.TimesSpotted}," +
                                  $"{character.PositionX:F2},{character.PositionY:F2},{character.PositionZ:F2}," +
                                  $"{character.Distance:F2},{character.Health}," +
                                  $"{character.FirstSeen:yyyy-MM-dd HH:mm:ss},{character.LastSeen:yyyy-MM-dd HH:mm:ss}");
                }

                File.WriteAllText(path, sb.ToString());
                Console.WriteLine($"[ZeroIn] Saved CSV: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error saving CSV: {ex.Message}");
            }
        }

        private void SaveAsSummary(List<DetectedCharacter> characters, string areaName, string timestamp)
        {
            try
            {
                string filename = $"scan_{areaName}_{timestamp}.txt";
                string path = Path.Combine(_outputPath, filename);

                var sb = new StringBuilder();
                sb.AppendLine($"ZeroIn Scan Results - {areaName}");
                sb.AppendLine($"Scan Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Characters Detected: {characters.Count}");
                sb.AppendLine();
                sb.AppendLine("=".PadRight(80, '='));

                foreach (var character in characters.OrderBy(c => c.Name))
                {
                    var age = (DateTime.UtcNow - character.LastSeen).TotalSeconds;
                    sb.AppendLine($"{character.Name}");
                    sb.AppendLine($"  Position: ({character.PositionX:F1}, {character.PositionY:F1}, {character.PositionZ:F1})");
                    sb.AppendLine($"  Distance: {character.Distance:F1}m");
                    sb.AppendLine($"  Times Spotted: {character.TimesSpotted}");
                    sb.AppendLine($"  Last Seen: {age:F0}s ago");
                    sb.AppendLine();
                }

                File.WriteAllText(path, sb.ToString());
                Console.WriteLine($"[ZeroIn] Saved Summary: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error saving summary: {ex.Message}");
            }
        }

        private string MakeSafeFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return "Unknown";

            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", filename.Split(invalid));
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}
