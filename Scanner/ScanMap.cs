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

            var afkCount = characters.Count(c => c.IsLikelyAFK());
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" ZeroIn Scan Results - {characters.Count} characters detected ({afkCount} likely AFK)");
            Console.WriteLine("=".PadRight(80, '='));

            foreach (var character in characters.OrderBy(c => c.Name))
            {
                var age = (DateTime.UtcNow - character.LastSeen).TotalSeconds;
                var afkStatus = character.IsLikelyAFK() ? $" [AFK {character.GetAFKConfidence()}%]" : "";
                Console.WriteLine($"  {character.Name}{afkStatus}");
                Console.WriteLine($"    Position: ({character.PositionX:F1}, {character.PositionY:F1}, {character.PositionZ:F1})");
                Console.WriteLine($"    Playfield: {character.PlayfieldName} ({character.PlayfieldId})");
                Console.WriteLine($"    Distance: {character.Distance:F1}m");
                Console.WriteLine($"    Times Spotted: {character.TimesSpotted} | Distance Moved: {character.TotalDistanceMoved:F1}m");
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
                // Use the enhanced CSV header with AFK detection fields
                sb.AppendLine(DetectedCharacter.GetCsvHeader());

                foreach (var character in characters.OrderBy(c => c.Name))
                {
                    // Use the enhanced ToCsv method with all AFK tracking data
                    sb.AppendLine(character.ToCsv());
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

                var afkCount = characters.Count(c => c.IsLikelyAFK());
                var sb = new StringBuilder();
                sb.AppendLine($"ZeroIn Scan Results - {areaName}");
                sb.AppendLine($"Scan Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Characters Detected: {characters.Count} ({afkCount} likely AFK)");
                sb.AppendLine();
                sb.AppendLine("=".PadRight(80, '='));

                foreach (var character in characters.OrderBy(c => c.Name))
                {
                    var age = (DateTime.UtcNow - character.LastSeen).TotalSeconds;
                    var afkStatus = character.IsLikelyAFK() ? $" [AFK {character.GetAFKConfidence()}%]" : "";
                    sb.AppendLine($"{character.Name}{afkStatus}");
                    sb.AppendLine($"  Position: ({character.PositionX:F1}, {character.PositionY:F1}, {character.PositionZ:F1})");
                    sb.AppendLine($"  Playfield: {character.PlayfieldName} ({character.PlayfieldId})");
                    sb.AppendLine($"  Distance: {character.Distance:F1}m");
                    sb.AppendLine($"  Times Spotted: {character.TimesSpotted} | Distance Moved: {character.TotalDistanceMoved:F1}m");
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
