using System;
using System.Collections.Generic;
using AOSharp.Core;
using AOSharp.Common.GameData;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Adapter for Omega-style proximity radar detection.
    /// Scans all nearby players within detection range without requiring path movement.
    /// </summary>
    public class OmegaRadarDetector : IPlayerDetector
    {
        private readonly ZeroInConfig _config;
        private float _scanAccumulator;
        private float _scanInterval = 0.2f; // 5Hz default
        private HashSet<string> _whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string DetectorName => "Omega";
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Detection range in meters
        /// </summary>
        public float DetectionRange { get; set; } = 50f;

        /// <summary>
        /// Scan frequency in Hz (scans per second)
        /// </summary>
        public float ScanFrequency
        {
            get => _scanInterval > 0 ? 1f / _scanInterval : 0f;
            set => _scanInterval = value > 0 ? 1f / value : 0.2f;
        }

        public OmegaRadarDetector(ZeroInConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            IsEnabled = false; // OFF by default like Omega

            // Auto-whitelist local player
            try
            {
                if (DynelManager.LocalPlayer != null)
                {
                    _whitelist.Add(DynelManager.LocalPlayer.Name);
                }
            }
            catch { }
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _scanAccumulator += deltaTime;
            if (_scanAccumulator < _scanInterval)
                return;

            _scanAccumulator = 0f;
        }

        public List<DetectedPlayerInfo> Scan()
        {
            var results = new List<DetectedPlayerInfo>();
            if (!IsEnabled) return results;

            try
            {
                var me = DynelManager.LocalPlayer;
                if (me == null || !me.IsValid) return results;

                var myPos = me.Position;
                var playfieldId = (int)Playfield.ModelIdentity.Instance;
                var playfieldName = Playfield.Name;

                // Scan all characters in range
                foreach (var character in DynelManager.Characters)
                {
                    if (character == null || !character.IsValid) continue;
                    if (character.Identity == me.Identity) continue; // Skip self

                    try
                    {
                        // Skip whitelisted players
                        if (!string.IsNullOrEmpty(character.Name) && _whitelist.Contains(character.Name))
                            continue;

                        // Check if player (not NPC)
                        if (!SafeIsPlayer(character))
                            continue;

                        var distance = Vector3.Distance(myPos, character.Position);

                        // Check if within detection range
                        if (distance > DetectionRange)
                            continue;

                        var info = new DetectedPlayerInfo
                        {
                            InstanceId = character.Identity.Instance,
                            Name = character.Name ?? "Unknown",
                            PositionX = character.Position.X,
                            PositionY = character.Position.Y,
                            PositionZ = character.Position.Z,
                            Distance = distance,
                            Health = character.Health,
                            PlayfieldId = playfieldId,
                            PlayfieldName = playfieldName,
                            DetectorSource = DetectorName
                        };

                        results.Add(info);
                    }
                    catch (Exception ex)
                    {
                        ZeroIn.Log?.Warning($"OmegaRadar: Error scanning character: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ZeroIn.Log?.Error($"OmegaRadar: Scan failed: {ex}");
            }

            return results;
        }

        public void AddWhitelist(string playerName)
        {
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                _whitelist.Add(playerName.Trim());
            }
        }

        public bool RemoveWhitelist(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return false;
            return _whitelist.Remove(playerName.Trim());
        }

        public void ClearWhitelist()
        {
            _whitelist.Clear();

            // Re-add local player
            try
            {
                if (DynelManager.LocalPlayer != null)
                {
                    _whitelist.Add(DynelManager.LocalPlayer.Name);
                }
            }
            catch { }
        }

        public IEnumerable<string> GetWhitelist()
        {
            return new List<string>(_whitelist);
        }

        private static bool SafeIsPlayer(SimpleChar character)
        {
            try
            {
                return character.IsPlayer;
            }
            catch
            {
                try
                {
                    return !character.IsNpc;
                }
                catch
                {
                    // If we can't determine, assume it's a player to be safe
                    return true;
                }
            }
        }
    }
}
