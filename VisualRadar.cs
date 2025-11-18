using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using AOSharp.Pathfinding;
using AOSharp.Core.Misc;
using System;
using System.Linq;
using System.Collections.Generic;
using ZeroIn.Scanner;

namespace ZeroIn
{
    /// <summary>
    /// Visual radar overlay for displaying detected players and scan radius
    /// </summary>
    public class VisualRadar
    {
        private bool _enabled = true;
        private CharacterScanner _scanner;
        private ZeroInConfig _config;
        private SPath _detectionRadiusPath;
        private Dictionary<uint, SPath> _playerMarkers = new Dictionary<uint, SPath>();
        private AutoResetInterval _updateInterval;
        private bool _pathsCreated = false;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public VisualRadar(CharacterScanner scanner, ZeroInConfig config)
        {
            _scanner = scanner;
            _config = config;
            _updateInterval = new AutoResetInterval(500); // Update every 500ms instead of every frame
        }

        /// <summary>
        /// Draw the radar overlay - call this from Game.OnUpdate
        /// </summary>
        public void Draw()
        {
            if (!_enabled)
            {
                if (_pathsCreated)
                {
                    CleanupPaths();
                    _pathsCreated = false;
                }
                return;
            }

            // Throttle updates to reduce spam
            if (!_updateInterval.Elapsed)
                return;

            try
            {
                var localPlayer = DynelManager.LocalPlayer;
                if (localPlayer == null || !localPlayer.IsValid)
                    return;

                // Draw detection radius circle around player
                DrawDetectionRadius(localPlayer);

                // Draw detected players
                DrawDetectedPlayers(localPlayer);

                _pathsCreated = true;
            }
            catch (Exception ex)
            {
                // Silently catch rendering errors to avoid spam
                ZeroIn.Log?.Warning($"VisualRadar.Draw error: {ex.Message}");
            }
        }

        private void DrawDetectionRadius(SimpleChar localPlayer)
        {
            float radius = _config.PlayerDetectionRange;
            var position = localPlayer.Position;

            // Create a circular path around the player for detection radius visualization
            if (_detectionRadiusPath == null)
            {
                _detectionRadiusPath = SPath.Create();
                _detectionRadiusPath.Name = "ZeroIn_DetectionRadius";
                _detectionRadiusPath.PlayfieldId = Playfield.ModelIdentity.Instance;

                // Create a circle with 36 points (10 degree increments)
                int numPoints = 36;
                for (int i = 0; i < numPoints; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / numPoints);
                    float x = position.X + radius * (float)Math.Cos(angle);
                    float z = position.Z + radius * (float)Math.Sin(angle);

                    _detectionRadiusPath.Waypoints.Add(new Vector3(x, position.Y, z));
                }

                _detectionRadiusPath.IsLooping = true; // Make it a closed circle
            }
            else
            {
                // Update circle position to follow player
                for (int i = 0; i < _detectionRadiusPath.Waypoints.Count; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / _detectionRadiusPath.Waypoints.Count);
                    float x = position.X + radius * (float)Math.Cos(angle);
                    float z = position.Z + radius * (float)Math.Sin(angle);

                    _detectionRadiusPath.Waypoints[i] = new Vector3(x, position.Y, z);
                }
            }
        }

        private void DrawDetectedPlayers(SimpleChar localPlayer)
        {
            var detected = _scanner.GetDetectedCharacters();
            var now = DateTime.UtcNow;
            var activePlayerIds = new HashSet<uint>();

            foreach (var player in detected)
            {
                // Skip stale detections (older than 30 seconds)
                if ((now - player.LastSeen).TotalSeconds > 30)
                    continue;

                activePlayerIds.Add(player.CharId);

                var playerPos = new Vector3(player.PositionX, player.PositionY, player.PositionZ);

                // Determine marker size based on AFK status (AFK players get larger markers)
                bool isAfk = player.IsLikelyAFK();
                float markerSize = isAfk ? 3f : 2f;

                // Create or update marker for this player
                if (!_playerMarkers.ContainsKey(player.CharId))
                {
                    var markerPath = SPath.Create();
                    markerPath.Name = $"ZeroIn_Player_{player.CharId}_{(isAfk ? "AFK" : "Active")}";
                    markerPath.PlayfieldId = Playfield.ModelIdentity.Instance;

                    // Create a small cross marker
                    markerPath.Waypoints.Add(new Vector3(playerPos.X - markerSize, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X + markerSize, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z - markerSize));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z + markerSize));

                    _playerMarkers[player.CharId] = markerPath;
                }
                else
                {
                    // Update existing marker position and size
                    var markerPath = _playerMarkers[player.CharId];
                    markerPath.Name = $"ZeroIn_Player_{player.CharId}_{(isAfk ? "AFK" : "Active")}";

                    markerPath.Waypoints.Clear();
                    markerPath.Waypoints.Add(new Vector3(playerPos.X - markerSize, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X + markerSize, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z - markerSize));
                    markerPath.Waypoints.Add(new Vector3(playerPos.X, playerPos.Y, playerPos.Z + markerSize));
                }
            }

            // Clean up markers for players no longer detected
            var markersToRemove = _playerMarkers.Keys.Where(id => !activePlayerIds.Contains(id)).ToList();
            foreach (var id in markersToRemove)
            {
                _playerMarkers[id].Delete();
                _playerMarkers.Remove(id);
            }
        }

        private void CleanupPaths()
        {
            if (_detectionRadiusPath != null)
            {
                _detectionRadiusPath.Delete();
                _detectionRadiusPath = null;
            }

            foreach (var marker in _playerMarkers.Values)
            {
                marker.Delete();
            }
            _playerMarkers.Clear();
        }

        public void Toggle()
        {
            _enabled = !_enabled;

            if (!_enabled)
            {
                CleanupPaths();
            }

            Chat.WriteLine($"[ZeroIn] Visual radar: {(_enabled ? "ENABLED" : "DISABLED")}",
                _enabled ? ChatColor.Green : ChatColor.Red);
        }
    }
}
