using AOSharp.Common.GameData;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroIn.Config;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Scans for nearby characters during grid pattern movement
    /// </summary>
    public class CharacterScanner
    {
        private Dictionary<uint, DetectedCharacter> _detectedCharacters;
        private ZeroInConfig _config;
        private DateTime _lastScan;

        public CharacterScanner(ZeroInConfig config)
        {
            _config = config;
            _detectedCharacters = new Dictionary<uint, DetectedCharacter>();
            _lastScan = DateTime.MinValue;
        }

        /// <summary>
        /// Gets all detected characters
        /// </summary>
        public List<DetectedCharacter> GetDetectedCharacters()
        {
            return _detectedCharacters.Values.ToList();
        }

        /// <summary>
        /// Gets count of detected characters
        /// </summary>
        public int Count => _detectedCharacters.Count;

        /// <summary>
        /// Performs a scan for nearby characters
        /// </summary>
        public void Scan()
        {
            // Rate limiting
            if ((DateTime.Now - _lastScan).TotalMilliseconds < _config.ScanDelayMs)
                return;

            _lastScan = DateTime.Now;

            try
            {
                // Get all nearby simple characters (players)
                var nearbyChars = DynelManager.Characters
                    .Where(c => c.IsValid &&
                                c is SimpleChar &&
                                !c.IsNpc &&
                                Vector3.Distance(DynelManager.LocalPlayer.Position, c.Position) <= _config.PlayerDetectionRange)
                    .Select(c => c as SimpleChar)
                    .ToList();

                foreach (var character in nearbyChars)
                {
                    if (!ShouldDetect(character))
                        continue;

                    if (_detectedCharacters.ContainsKey(character.Identity.Instance))
                    {
                        // Update existing detection
                        var detected = _detectedCharacters[character.Identity.Instance];
                        detected.Update(character.Position);
                    }
                    else
                    {
                        // New detection
                        var detected = new DetectedCharacter
                        {
                            Name = character.Name,
                            CharId = character.Identity.Instance,
                            Position = character.Position,
                            Profession = character.Profession,
                            Breed = character.Breed,
                            Level = (int)character.Level,
                            Faction = Side.Neutral, // SimpleChar doesn't expose faction directly
                            LastPosition = character.Position
                        };

                        _detectedCharacters[character.Identity.Instance] = detected;

                        if (_config.LogToConsole)
                        {
                            Console.WriteLine($"[ZeroIn] Detected: {detected}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error during scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a character should be detected based on config filters
        /// </summary>
        private bool ShouldDetect(SimpleChar character)
        {
            if (character == null || !character.IsValid)
                return false;

            // Ignore self
            if (_config.IgnoreSelf && character.Identity == DynelManager.LocalPlayer.Identity)
                return false;

            // Ignore names in ignore list
            if (_config.IgnoreNames.Contains(character.Name))
                return false;

            return true;
        }

        /// <summary>
        /// Gets characters filtered by AFK status
        /// </summary>
        public List<DetectedCharacter> GetAFKCharacters()
        {
            return _detectedCharacters.Values
                .Where(c => c.IsLikelyAFK(_config.AFKCheckTimeSeconds))
                .ToList();
        }

        /// <summary>
        /// Gets characters that have moved
        /// </summary>
        public List<DetectedCharacter> GetMovingCharacters()
        {
            return _detectedCharacters.Values
                .Where(c => c.HasMoved)
                .ToList();
        }

        /// <summary>
        /// Clears all detected characters
        /// </summary>
        public void Clear()
        {
            _detectedCharacters.Clear();
        }

        /// <summary>
        /// Gets a summary of scan results
        /// </summary>
        public string GetSummary()
        {
            int total = _detectedCharacters.Count;
            int afk = GetAFKCharacters().Count;
            int moving = GetMovingCharacters().Count;

            return $"Total: {total} | AFK: {afk} | Moving: {moving}";
        }
    }
}
