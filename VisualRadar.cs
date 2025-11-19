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
        private bool _debugMode = false;
        private bool _initialized = false;
        private CharacterScanner _scanner;
        private ZeroInConfig _config;
        private SPath _detectionRadiusPath;
        private Dictionary<uint, SPath> _playerMarkers = new Dictionary<uint, SPath>();
        private AutoResetInterval _updateInterval;
        private AutoResetInterval _initDelay;
        private bool _pathsCreated = false;
        private int _drawCallCount = 0;
        private int _errorCount = 0;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public bool DebugMode
        {
            get => _debugMode;
            set
            {
                _debugMode = value;
                if (_debugMode)
                {
                    Chat.WriteLine("[VisualRadar] Debug mode ENABLED - detailed logging active", ChatColor.Yellow);
                    Chat.WriteLine($"[VisualRadar] Current state: Enabled={_enabled}, PathsCreated={_pathsCreated}, DrawCalls={_drawCallCount}, Errors={_errorCount}", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine("[VisualRadar] Debug mode DISABLED", ChatColor.Yellow);
                }
            }
        }

        public VisualRadar(CharacterScanner scanner, ZeroInConfig config)
        {
            _scanner = scanner;
            _config = config;
            _updateInterval = new AutoResetInterval(500); // Update every 500ms instead of every frame
            _initDelay = new AutoResetInterval(2000); // Wait 2 seconds after injection before initializing
        }

        /// <summary>
        /// Draw the radar overlay - call this from Game.OnUpdate
        /// </summary>
        public void Draw()
        {
            _drawCallCount++;

            if (!_enabled)
            {
                if (_pathsCreated)
                {
                    if (_debugMode)
                        Chat.WriteLine("[VisualRadar] Radar disabled, cleaning up paths", ChatColor.Yellow);
                    CleanupPaths();
                    _pathsCreated = false;
                }
                return;
            }

            // Wait for initialization delay to pass (prevents errors during injection)
            if (!_initialized)
            {
                if (!_initDelay.Elapsed)
                    return;

                _initialized = true;
                if (_debugMode)
                    Chat.WriteLine("[VisualRadar] Initialization delay complete, radar active", ChatColor.Green);
            }

            // Throttle updates to reduce spam
            if (!_updateInterval.Elapsed)
                return;

            try
            {
                var localPlayer = DynelManager.LocalPlayer;
                if (localPlayer == null || !localPlayer.IsValid)
                {
                    // Don't spam errors during early initialization
                    if (_debugMode && _drawCallCount % 100 == 0 && _initialized)
                        Chat.WriteLine($"[VisualRadar] LocalPlayer is null or invalid (draw #{_drawCallCount})", ChatColor.Yellow);

                    // Clean up paths when player is invalid
                    if (_pathsCreated)
                    {
                        if (_debugMode)
                            Chat.WriteLine("[VisualRadar] Cleaning up paths due to invalid player", ChatColor.Yellow);
                        CleanupPaths();
                        _pathsCreated = false;
                    }
                    return;
                }

                // Validate player position (not zero/default)
                var position = localPlayer.Position;
                if (position.X == 0 && position.Y == 0 && position.Z == 0)
                {
                    // Don't spam errors during early initialization
                    if (_debugMode && _drawCallCount % 100 == 0 && _initialized)
                        Chat.WriteLine($"[VisualRadar] Player position is at origin (0,0,0) - invalid (draw #{_drawCallCount})", ChatColor.Yellow);
                    return;
                }

                // Validate playfield is loaded
                if (Playfield.ModelIdentity.Instance == 0)
                {
                    if (_debugMode && _drawCallCount % 100 == 0)
                        Chat.WriteLine($"[VisualRadar] Playfield not loaded yet", ChatColor.Yellow);
                    return;
                }

                if (_debugMode && _drawCallCount % 200 == 0)
                {
                    Chat.WriteLine($"[VisualRadar] Draw #{_drawCallCount}: Pos=({position.X:F1},{position.Y:F1},{position.Z:F1}), Markers={_playerMarkers.Count}, Errors={_errorCount}", ChatColor.White);
                }

                // Draw detection radius circle around player (if enabled)
                if (_config.ShowDetectionRadius)
                {
                    DrawDetectionRadius(localPlayer);
                }
                else if (_detectionRadiusPath != null)
                {
                    if (_debugMode)
                        Chat.WriteLine("[VisualRadar] Hiding detection radius (toggled off)", ChatColor.Yellow);
                    // Hide detection radius if toggled off
                    _detectionRadiusPath.Delete();
                    _detectionRadiusPath = null;
                }

                // Draw detected players (if enabled)
                if (_config.ShowPlayerMarkers)
                {
                    DrawDetectedPlayers(localPlayer);
                }
                else
                {
                    // Clean up player markers if toggled off
                    if (_playerMarkers.Count > 0 && _debugMode)
                        Chat.WriteLine($"[VisualRadar] Cleaning up {_playerMarkers.Count} player markers (toggled off)", ChatColor.Yellow);

                    foreach (var marker in _playerMarkers.Values)
                    {
                        marker.Delete();
                    }
                    _playerMarkers.Clear();
                }

                _pathsCreated = true;
            }
            catch (Exception ex)
            {
                _errorCount++;
                // Always log errors to help diagnose issues
                Chat.WriteLine($"[VisualRadar] ERROR #{_errorCount} in Draw(): {ex.Message}", ChatColor.Red);
                Chat.WriteLine($"[VisualRadar] Stack: {ex.StackTrace}", ChatColor.Red);
                ZeroIn.Log?.Warning($"VisualRadar.Draw error #{_errorCount}: {ex}");
            }
        }

        private void DrawDetectionRadius(SimpleChar localPlayer)
        {
            float radius = _config.PlayerDetectionRange;
            var position = localPlayer.Position;

            // Validate radius is reasonable
            if (radius <= 0 || radius > 1000)
            {
                if (_debugMode && _errorCount % 10 == 0)
                    Chat.WriteLine($"[VisualRadar] Invalid radius: {radius}m (must be 0-1000)", ChatColor.Red);
                return;
            }

            // Create a circular path around the player for detection radius visualization
            if (_detectionRadiusPath == null)
            {
                if (_debugMode)
                    Chat.WriteLine($"[VisualRadar] Creating detection radius circle: {radius:F0}m at ({position.X:F1},{position.Y:F1},{position.Z:F1})", ChatColor.Green);

                // Create a circle with 36 points (10 degree increments)
                int numPoints = 36;
                var waypoints = new System.Collections.Generic.List<Vector3>();

                for (int i = 0; i < numPoints; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / numPoints);
                    float x = position.X + radius * (float)Math.Cos(angle);
                    float z = position.Z + radius * (float)Math.Sin(angle);

                    waypoints.Add(new Vector3(x, position.Y, z));
                }

                // Only create path if we have valid, distinct waypoints
                if (waypoints.Count >= 3)
                {
                    _detectionRadiusPath = SPath.Create();
                    _detectionRadiusPath.Name = $"[SCAN_RADIUS] {radius:F0}m";
                    _detectionRadiusPath.PlayfieldId = Playfield.ModelIdentity.Instance;

                    foreach (var wp in waypoints)
                    {
                        _detectionRadiusPath.Waypoints.Add(wp);
                    }

                    _detectionRadiusPath.IsLooping = true; // Make it a closed circle

                    if (_debugMode)
                        Chat.WriteLine($"[VisualRadar] Detection radius path created with {waypoints.Count} waypoints", ChatColor.Green);
                }
                else
                {
                    if (_debugMode)
                        Chat.WriteLine($"[VisualRadar] ERROR: Not enough waypoints ({waypoints.Count}) to create radius path", ChatColor.Red);
                }
            }
            else
            {
                // Update circle position to follow player
                if (_detectionRadiusPath.Waypoints.Count > 0)
                {
                    for (int i = 0; i < _detectionRadiusPath.Waypoints.Count; i++)
                    {
                        float angle = (float)(i * Math.PI * 2 / _detectionRadiusPath.Waypoints.Count);
                        float x = position.X + radius * (float)Math.Cos(angle);
                        float z = position.Z + radius * (float)Math.Sin(angle);

                        _detectionRadiusPath.Waypoints[i] = new Vector3(x, position.Y, z);
                    }
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

                var playerPos = new Vector3(player.PositionX, player.PositionY, player.PositionZ);

                // Determine marker size based on AFK status (AFK players get larger markers)
                bool isAfk = player.IsLikelyAFK();

                // Skip AFK players if ShowAFKPaths is disabled
                if (isAfk && !_config.ShowAFKPaths)
                    continue;

                // Skip active (non-AFK) players if ShowActivePlayerPaths is disabled
                if (!isAfk && !_config.ShowActivePlayerPaths)
                    continue;

                // Only add to activePlayerIds if we're actually going to show this player
                activePlayerIds.Add(player.CharId);

                float markerSize = isAfk ? _config.AFKMarkerSize : _config.ActiveMarkerSize;
                string markerType = isAfk ? "AFK_PLAYER" : "ACTIVE_PLAYER";

                // Create or update marker for this player
                if (!_playerMarkers.ContainsKey(player.CharId))
                {
                    var markerPath = SPath.Create();
                    markerPath.Name = $"[{markerType}] {player.Name}";
                    markerPath.PlayfieldId = Playfield.ModelIdentity.Instance;

                    // Create marker shape
                    AddMarkerShape(markerPath, playerPos, markerSize, _config.PlayerMarkerShape, isAfk);

                    _playerMarkers[player.CharId] = markerPath;
                }
                else
                {
                    // Update existing marker position and size
                    var markerPath = _playerMarkers[player.CharId];
                    markerPath.Name = $"[{markerType}] {player.Name}";

                    markerPath.Waypoints.Clear();
                    AddMarkerShape(markerPath, playerPos, markerSize, _config.PlayerMarkerShape, isAfk);
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

        /// <summary>
        /// Adds waypoints to create different marker shapes
        /// </summary>
        private void AddMarkerShape(SPath path, Vector3 center, float size, string shape, bool isAfk)
        {
            // Validate size
            if (size <= 0 || size > 100)
            {
                if (_debugMode)
                    Chat.WriteLine($"[VisualRadar] Invalid marker size: {size}m, using default 2m", ChatColor.Yellow);
                size = 2f; // Default to 2m if invalid
            }

            // Validate center position is not at origin (likely invalid)
            if (center.X == 0 && center.Y == 0 && center.Z == 0)
            {
                if (_debugMode)
                    Chat.WriteLine($"[VisualRadar] ERROR: Cannot create marker at origin (0,0,0)", ChatColor.Red);
                return;
            }

            // Minimal waypoint mode: just a single point (minimal visibility, like zone waypoints)
            if (!_config.ShowMarkerLines)
            {
                path.Waypoints.Add(center);
                return;
            }

            // AFK players get diamond markers, active players get the configured shape
            string actualShape = isAfk ? "diamond" : shape;

            switch (actualShape.ToLower())
            {
                case "cross":
                    // Cross shape (X pattern) - separate line segments
                    // Horizontal line
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z));
                    path.Waypoints.Add(new Vector3(center.X + size, center.Y, center.Z));
                    // Vertical line (needs center point to create gap)
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z));
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z - size));
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z + size));
                    break;

                case "circle":
                    // Circle shape (8 points)
                    int numPoints = 8;
                    for (int i = 0; i <= numPoints; i++) // <= to close the circle
                    {
                        float angle = (float)(i * Math.PI * 2 / numPoints);
                        float x = center.X + size * (float)Math.Cos(angle);
                        float z = center.Z + size * (float)Math.Sin(angle);
                        path.Waypoints.Add(new Vector3(x, center.Y, z));
                    }
                    break;

                case "square":
                    // Square shape
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z - size));
                    path.Waypoints.Add(new Vector3(center.X + size, center.Y, center.Z - size));
                    path.Waypoints.Add(new Vector3(center.X + size, center.Y, center.Z + size));
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z + size));
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z - size)); // Close the square
                    break;

                case "diamond":
                    // Diamond shape (rotated square)
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z - size));       // Top
                    path.Waypoints.Add(new Vector3(center.X + size, center.Y, center.Z));      // Right
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z + size));       // Bottom
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z));      // Left
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z - size));       // Close
                    break;

                default:
                    // Default to cross if invalid shape specified
                    path.Waypoints.Add(new Vector3(center.X - size, center.Y, center.Z));
                    path.Waypoints.Add(new Vector3(center.X + size, center.Y, center.Z));
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z));
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z - size));
                    path.Waypoints.Add(new Vector3(center.X, center.Y, center.Z + size));
                    break;
            }
        }

        private void CleanupPaths()
        {
            if (_debugMode)
                Chat.WriteLine($"[VisualRadar] Cleaning up paths: RadiusPath={(_detectionRadiusPath != null)}, PlayerMarkers={_playerMarkers.Count}", ChatColor.Yellow);

            try
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

                if (_debugMode)
                    Chat.WriteLine("[VisualRadar] Cleanup completed successfully", ChatColor.Green);
            }
            catch (Exception ex)
            {
                _errorCount++;
                Chat.WriteLine($"[VisualRadar] ERROR during cleanup: {ex.Message}", ChatColor.Red);
                ZeroIn.Log?.Warning($"VisualRadar.CleanupPaths error: {ex}");
            }
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

            if (_debugMode)
            {
                Chat.WriteLine($"[VisualRadar] Stats - DrawCalls: {_drawCallCount}, Errors: {_errorCount}, PathsCreated: {_pathsCreated}", ChatColor.White);
            }
        }

        public void PrintStats()
        {
            Chat.WriteLine("=== VisualRadar Statistics ===", ChatColor.Yellow);
            Chat.WriteLine($"Enabled: {_enabled}", _enabled ? ChatColor.Green : ChatColor.Red);
            Chat.WriteLine($"Debug Mode: {_debugMode}", _debugMode ? ChatColor.Green : ChatColor.Red);
            Chat.WriteLine($"Paths Created: {_pathsCreated}", ChatColor.White);
            Chat.WriteLine($"Draw Calls: {_drawCallCount}", ChatColor.White);
            Chat.WriteLine($"Errors: {_errorCount}", _errorCount > 0 ? ChatColor.Red : ChatColor.Green);
            Chat.WriteLine($"Active Player Markers: {_playerMarkers.Count}", ChatColor.White);
            Chat.WriteLine($"Detection Radius Path: {(_detectionRadiusPath != null ? "Active" : "None")}", ChatColor.White);

            if (_detectionRadiusPath != null)
            {
                Chat.WriteLine($"  Radius Waypoints: {_detectionRadiusPath.Waypoints.Count}", ChatColor.White);
            }
        }
    }
}
