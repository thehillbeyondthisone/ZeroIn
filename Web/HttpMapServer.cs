using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.Inventory;
using AOSharp.Common.GameData;
using AOSharp.Pathfinding;
using Newtonsoft.Json;
using ZeroIn.Scanner;
using ZeroIn.PlanetMap;

namespace ZeroIn.Web
{
    /// <summary>
    /// Simple HTTP server for the live map interface
    /// </summary>
    public class HttpMapServer
    {
        private HttpListener _listener;
        private Thread _serverThread;
        private bool _running = false;
        private readonly int _port;
        private readonly string _dataPath;
        private readonly MapCoordinateLoader _mapCoords;

        public HttpMapServer(int port, string dataPath, MapCoordinateLoader mapCoords)
        {
            _port = port;
            _dataPath = dataPath;
            _mapCoords = mapCoords;
        }

        public void Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
                _running = true;

                _serverThread = new Thread(Listen);
                _serverThread.IsBackground = true;
                _serverThread.Start();

                Chat.WriteLine($"[HttpMapServer] Started on http://localhost:{_port}", ChatColor.Green);
                Chat.WriteLine($"[HttpMapServer] Open http://localhost:{_port} in your browser to view the live map", ChatColor.Yellow);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[HttpMapServer] Failed to start: {ex.Message}", ChatColor.Red);
                ZeroIn.Log?.Error($"HttpMapServer start failed: {ex}");
            }
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            _listener?.Close();
            Chat.WriteLine("[HttpMapServer] Stopped", ChatColor.Yellow);
        }

        private void Listen()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        ZeroIn.Log?.Warning($"HttpMapServer error: {ex.Message}");
                    }
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Enable CORS
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath;

                switch (path)
                {
                    case "/":
                        ServeHtml(response);
                        break;
                    case "/api/status":
                        ServeStatus(response);
                        break;
                    case "/api/player":
                        ServePlayer(response);
                        break;
                    case "/api/players":
                        ServePlayers(response);
                        break;
                    case "/api/position":
                        ServePosition(response);
                        break;
                    case "/api/movement":
                        ServeMovement(response);
                        break;
                    case "/api/zone":
                        ServeZone(response);
                        break;
                    case "/api/faction":
                        ServeFaction(response);
                        break;
                    case "/api/team":
                        ServeTeam(response);
                        break;
                    case "/api/stats":
                        ServeStats(response);
                        break;
                    case "/api/path":
                        ServePath(response);
                        break;
                    case "/api/config":
                        ServeConfig(response);
                        break;
                    case "/api/mapinfo":
                        ServeMapInfo(response);
                        break;
                    case "/api/inventory":
                        ServeInventory(response);
                        break;
                    case "/api/skills":
                        ServeSkills(response);
                        break;
                    default:
                        // Try to serve map image
                        if (path.StartsWith("/maps/"))
                        {
                            ServeMapImage(response, path);
                        }
                        else
                        {
                            response.StatusCode = 404;
                            SendJson(response, new { error = "Not found" });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ZeroIn.Log?.Warning($"HandleRequest error: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    SendJson(context.Response, new { error = ex.Message });
                }
                catch { }
            }
        }

        private void ServeHtml(HttpListenerResponse response)
        {
            var html = GetMapHtml();
            var bytes = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private void ServeStatus(HttpListenerResponse response)
        {
            var data = new
            {
                online = true,
                timestamp = DateTime.UtcNow,
                playfield = Playfield.Name,
                playfieldId = Playfield.ModelIdentity.Instance,
                radarEnabled = ZeroIn.Radar?.Enabled ?? false,
                scannerActive = ZeroIn.StateMachine?.IsEnabled ?? false
            };
            SendJson(response, data);
        }

        private void ServePlayers(HttpListenerResponse response)
        {
            var players = ZeroIn.Scanner?.GetDetectedCharacters() ?? new List<DetectedCharacter>();
            var data = players.Select(p => new
            {
                id = p.CharId,
                name = p.Name,
                x = p.PositionX,
                y = p.PositionY,
                z = p.PositionZ,
                distance = p.Distance,
                isAfk = p.IsLikelyAFK(),
                afkConfidence = p.GetAFKConfidence(),
                timesSpotted = p.TimesSpotted,
                lastSeen = p.LastSeen,
                playfieldId = p.PlayfieldId,
                playfieldName = p.PlayfieldName
            }).ToList();

            SendJson(response, data);
        }

        private void ServePosition(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var pos = player.Position;
            var data = new
            {
                x = pos.X,
                y = pos.Y,
                z = pos.Z,
                playfieldId = Playfield.ModelIdentity.Instance,
                playfieldName = Playfield.Name,
                detectionRange = ZeroIn.Config?.PlayerDetectionRange ?? 50f
            };

            SendJson(response, data);
        }

        private void ServePath(HttpListenerResponse response)
        {
            var path = ZeroIn.RoamPath?.SPath;
            if (path == null || path.Waypoints == null)
            {
                SendJson(response, new { waypoints = new object[0] });
                return;
            }

            var data = new
            {
                waypoints = path.Waypoints.Select(w => new
                {
                    x = w.X,
                    y = w.Y,
                    z = w.Z
                }).ToList(),
                isLooping = path.IsLooping,
                playfieldId = path.PlayfieldId
            };

            SendJson(response, data);
        }

        private void ServeConfig(HttpListenerResponse response)
        {
            var data = new
            {
                showDetectionRadius = ZeroIn.Config?.ShowDetectionRadius ?? true,
                showPlayerMarkers = ZeroIn.Config?.ShowPlayerMarkers ?? true,
                showAFKPaths = ZeroIn.Config?.ShowAFKPaths ?? true,
                showActivePlayerPaths = ZeroIn.Config?.ShowActivePlayerPaths ?? true,
                detectionRange = ZeroIn.Config?.PlayerDetectionRange ?? 50f,
                afkMarkerSize = ZeroIn.Config?.AFKMarkerSize ?? 5f,
                activeMarkerSize = ZeroIn.Config?.ActiveMarkerSize ?? 2f
            };

            SendJson(response, data);
        }

        private void ServeMapInfo(HttpListenerResponse response)
        {
            var playfieldId = Playfield.ModelIdentity.Instance;
            var mapInfo = _mapCoords?.GetPlayfieldInfo((uint)playfieldId);

            if (mapInfo == null)
            {
                SendJson(response, new
                {
                    error = "No map data for current playfield",
                    playfieldId = playfieldId,
                    playfieldName = Playfield.Name
                });
                return;
            }

            var data = new
            {
                playfieldId = mapInfo.Id,
                playfieldName = mapInfo.Name,
                referenceX = mapInfo.X,
                referenceZ = mapInfo.Z,
                xScale = mapInfo.XScale,
                zScale = mapInfo.ZScale,
                hasMapCoords = true
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Comprehensive player data including stats, faction, location
        /// </summary>
        private void ServePlayer(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var pos = player.Position;
            var playfieldId = Playfield.ModelIdentity.Instance;

            // Transform to map coordinates
            var mapPos = _mapCoords?.GameToMap((uint)playfieldId, pos.X, pos.Z);

            var data = new
            {
                // Identity
                id = player.Identity.Instance,
                name = player.Name,
                level = player.Level,

                // Faction & Organization (using Stats)
                faction = GetFactionName((int)player.GetStat(Stat.Side)),
                factionId = (int)player.GetStat(Stat.Side),

                // Stats
                health = player.Health,
                healthMax = player.MaxHealth,
                healthPercent = player.MaxHealth > 0 ? (player.Health * 100.0 / player.MaxHealth) : 0,
                nano = player.Nano,
                nanoMax = player.MaxNano,
                nanoPercent = player.MaxNano > 0 ? (player.Nano * 100.0 / player.MaxNano) : 0,

                // Position (Game Coordinates)
                position = new
                {
                    x = pos.X,
                    y = pos.Y,
                    z = pos.Z
                },

                // Position (Map Coordinates)
                mapPosition = mapPos != null && mapPos.Valid ? new
                {
                    valid = true,
                    mapX = mapPos.MapX,
                    mapZ = mapPos.MapZ,
                    playfieldId = mapPos.PlayfieldId,
                    playfieldName = mapPos.PlayfieldName
                } : new
                {
                    valid = false,
                    mapX = 0f,
                    mapZ = 0f,
                    playfieldId = (uint)playfieldId,
                    playfieldName = Playfield.Name
                },

                // Movement
                movementState = player.MovementState.ToString(),
                isMoving = player.IsMoving,

                // Combat
                isInCombat = player.IsAttacking || DynelManager.Characters.Any(x => x.FightingTarget?.Identity == player.Identity),
                isFighting = player.FightingTarget != null && player.FightingTarget.IsValid,
                fightingTargetId = player.FightingTarget != null && player.FightingTarget.IsValid ?
                    (int?)player.FightingTarget.Instance : null,

                // Playfield
                playfieldId = playfieldId,
                playfieldName = Playfield.Name,

                // Misc
                profession = player.Profession.ToString(),
                breed = player.Breed.ToString(),
                gender = GetGenderName((int)player.GetStat(Stat.Sex)),

                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Movement data: speed, direction, heading, velocity
        /// </summary>
        private void ServeMovement(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var data = new
            {
                movementState = player.MovementState.ToString(),
                isMoving = player.IsMoving,
                position = new
                {
                    x = player.Position.X,
                    y = player.Position.Y,
                    z = player.Position.Z
                },
                isNavigating = SMovementController.IsNavigating(),
                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Zone/Playfield information with map coordinate support
        /// </summary>
        private void ServeZone(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            var playfieldId = Playfield.ModelIdentity.Instance;
            var mapInfo = _mapCoords?.GetPlayfieldInfo((uint)playfieldId);

            var data = new
            {
                playfieldId = playfieldId,
                playfieldName = Playfield.Name,
                hasMapData = mapInfo != null,
                mapCoordinates = mapInfo != null ? new
                {
                    referenceX = mapInfo.X,
                    referenceZ = mapInfo.Z,
                    xScale = mapInfo.XScale,
                    zScale = mapInfo.ZScale
                } : null,
                playerPosition = player != null && player.IsValid ? new
                {
                    x = player.Position.X,
                    y = player.Position.Y,
                    z = player.Position.Z
                } : null,
                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Faction and organization data
        /// </summary>
        private void ServeFaction(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var data = new
            {
                faction = GetFactionName((int)player.GetStat(Stat.Side)),
                factionId = (int)player.GetStat(Stat.Side),
                profession = player.Profession.ToString(),
                level = player.Level,
                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Team information (all team members)
        /// </summary>
        private void ServeTeam(HttpListenerResponse response)
        {
            var team = Team.Members;

            var teamData = team
                .Where(member => member.Character != null && member.Character.IsValid)
                .Select(member => new
                {
                    id = member.Identity.Instance,
                    name = member.Name,
                    health = member.Character.Health,
                    healthMax = member.Character.MaxHealth,
                    healthPercent = member.Character.MaxHealth > 0 ? (member.Character.Health * 100.0 / member.Character.MaxHealth) : 0,
                    nano = member.Character.Nano,
                    nanoMax = member.Character.MaxNano,
                    nanoPercent = member.Character.MaxNano > 0 ? (member.Character.Nano * 100.0 / member.Character.MaxNano) : 0,
                    profession = member.Profession.ToString(),
                    level = member.Level,
                    isValid = member.Character.IsValid,
                    position = new
                    {
                        x = member.Character.Position.X,
                        y = member.Character.Position.Y,
                        z = member.Character.Position.Z
                    }
                }).ToList();

            var data = new
            {
                teamSize = teamData.Count,
                members = teamData,
                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Character stats
        /// </summary>
        private void ServeStats(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var data = new
            {
                // Core Stats
                health = player.Health,
                healthMax = player.MaxHealth,
                healthPercent = player.MaxHealth > 0 ? (player.Health * 100.0 / player.MaxHealth) : 0,
                nano = player.Nano,
                nanoMax = player.MaxNano,
                nanoPercent = player.MaxNano > 0 ? (player.Nano * 100.0 / player.MaxNano) : 0,

                // Character Info
                name = player.Name,
                level = player.Level,
                profession = player.Profession.ToString(),
                faction = GetFactionName((int)player.GetStat(Stat.Side)),
                breed = player.Breed.ToString(),
                gender = GetGenderName((int)player.GetStat(Stat.Sex)),

                // Combat State
                isInCombat = player.IsAttacking || DynelManager.Characters.Any(x => x.FightingTarget?.Identity == player.Identity),
                isFighting = player.FightingTarget != null && player.FightingTarget.IsValid,

                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Inventory summary
        /// </summary>
        private void ServeInventory(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            try
            {
                var inventory = Inventory.Items;
                var items = inventory.Select(item => new
                {
                    id = item.Identity.Instance,
                    name = item.Name,
                    slot = item.Slot.ToString(),
                    ql = item.QualityLevel,
                    icon = item.IconId,
                    isValid = item.IsValid
                }).ToList();

                var data = new
                {
                    itemCount = items.Count,
                    items = items,
                    timestamp = DateTime.UtcNow
                };

                SendJson(response, data);
            }
            catch (Exception ex)
            {
                SendJson(response, new { error = $"Inventory error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Skills/Abilities data
        /// </summary>
        private void ServeSkills(HttpListenerResponse response)
        {
            var player = DynelManager.LocalPlayer;
            if (player == null || !player.IsValid)
            {
                SendJson(response, new { error = "Player not available" });
                return;
            }

            var data = new
            {
                profession = player.Profession.ToString(),
                level = player.Level,
                timestamp = DateTime.UtcNow
            };

            SendJson(response, data);
        }

        /// <summary>
        /// Helper to convert faction ID to faction name
        /// </summary>
        private string GetFactionName(int factionId)
        {
            switch (factionId)
            {
                case 0: return "Neutral";
                case 1: return "Omni";
                case 2: return "Clan";
                case 3: return "Neutral";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Helper to convert gender ID to gender name
        /// </summary>
        private string GetGenderName(int genderId)
        {
            switch (genderId)
            {
                case 0: return "Male";
                case 1: return "Female";
                case 2: return "Neuter";
                default: return "Unknown";
            }
        }

        private void ServeMapImage(HttpListenerResponse response, string path)
        {
            // Extract filename from path
            var filename = path.Substring("/maps/".Length);

            // Try multiple locations for map files
            string mapPath = null;

            // First try the Maps folder in data path
            var dataMapPath = Path.Combine(_dataPath, "Maps", filename);
            if (File.Exists(dataMapPath))
            {
                mapPath = dataMapPath;
            }
            // Then try the PlanetMap folder in plugin directory
            else
            {
                var pluginMapPath = Path.Combine(ZeroIn.PluginDir, "PlanetMap", filename);
                if (File.Exists(pluginMapPath))
                {
                    mapPath = pluginMapPath;
                }
                // Try with .bin extension (the map files are PNG but named .bin)
                else
                {
                    var binPath = Path.Combine(ZeroIn.PluginDir, "PlanetMap", Path.ChangeExtension(filename, ".bin"));
                    if (File.Exists(binPath))
                    {
                        mapPath = binPath;
                    }
                    // Try subdirectories
                    else
                    {
                        // Try normal/PlanetMapGfxNormal.bin
                        binPath = Path.Combine(ZeroIn.PluginDir, "PlanetMap", "normal", "PlanetMapGfxNormal.bin");
                        if (File.Exists(binPath) && (filename.Contains("normal") || filename.Contains("planet")))
                        {
                            mapPath = binPath;
                        }
                    }
                }
            }

            if (mapPath == null || !File.Exists(mapPath))
            {
                response.StatusCode = 404;
                SendJson(response, new { error = "Map not found", requested = filename });
                return;
            }

            try
            {
                var bytes = File.ReadAllBytes(mapPath);

                // Always serve as PNG since the .bin files are actually PNG images
                response.ContentType = "image/png";
                response.ContentLength64 = bytes.Length;
                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                ZeroIn.Log?.Warning($"Error serving map image: {ex.Message}");
                response.StatusCode = 500;
                SendJson(response, new { error = "Failed to load map image" });
            }
        }

        private void SendJson(HttpListenerResponse response, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private string GetMapHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>ZeroIn Live Map</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #1a1a1a;
            color: #e0e0e0;
            overflow: hidden;
        }
        #container {
            display: flex;
            height: 100vh;
        }
        #sidebar {
            width: 300px;
            background: #2a2a2a;
            padding: 20px;
            overflow-y: auto;
            border-right: 2px solid #444;
        }
        #map-container {
            flex: 1;
            position: relative;
            overflow: hidden;
            background: #0a0a0a;
        }
        #map-canvas {
            position: absolute;
            cursor: grab;
        }
        #map-canvas:active {
            cursor: grabbing;
        }
        .header {
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 20px;
            color: #4CAF50;
        }
        .status {
            margin-bottom: 20px;
            padding: 10px;
            background: #333;
            border-radius: 5px;
        }
        .status-item {
            display: flex;
            justify-content: space-between;
            margin: 5px 0;
            font-size: 14px;
        }
        .status-label {
            color: #999;
        }
        .status-value {
            color: #4CAF50;
            font-weight: bold;
        }
        .player-list {
            margin-top: 20px;
        }
        .player-item {
            background: #333;
            padding: 10px;
            margin: 5px 0;
            border-radius: 5px;
            border-left: 3px solid #4CAF50;
        }
        .player-item.afk {
            border-left-color: #ff9800;
        }
        .player-name {
            font-weight: bold;
            color: #e0e0e0;
        }
        .player-info {
            font-size: 12px;
            color: #999;
            margin-top: 5px;
        }
        .controls {
            margin: 20px 0;
            padding: 10px;
            background: #333;
            border-radius: 5px;
        }
        .control-btn {
            background: #4CAF50;
            color: white;
            border: none;
            padding: 8px 16px;
            margin: 5px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 14px;
        }
        .control-btn:hover {
            background: #45a049;
        }
        .control-btn.secondary {
            background: #555;
        }
        .control-btn.secondary:hover {
            background: #666;
        }
        .legend {
            margin-top: 20px;
            padding: 10px;
            background: #333;
            border-radius: 5px;
            font-size: 12px;
        }
        .legend-item {
            display: flex;
            align-items: center;
            margin: 5px 0;
        }
        .legend-color {
            width: 20px;
            height: 20px;
            margin-right: 10px;
            border-radius: 3px;
        }
        #zoom-controls {
            position: absolute;
            top: 20px;
            right: 20px;
            background: #2a2a2a;
            padding: 10px;
            border-radius: 5px;
            z-index: 100;
        }
        .zoom-btn {
            display: block;
            width: 40px;
            height: 40px;
            margin: 5px 0;
            background: #4CAF50;
            color: white;
            border: none;
            border-radius: 3px;
            font-size: 20px;
            cursor: pointer;
        }
        .zoom-btn:hover {
            background: #45a049;
        }
    </style>
</head>
<body>
    <div id=""container"">
        <div id=""sidebar"">
            <div class=""header"">ZeroIn Live Map</div>

            <div class=""status"">
                <div class=""status-item"">
                    <span class=""status-label"">Status:</span>
                    <span class=""status-value"" id=""status"">Connecting...</span>
                </div>
                <div class=""status-item"">
                    <span class=""status-label"">Playfield:</span>
                    <span class=""status-value"" id=""playfield"">-</span>
                </div>
                <div class=""status-item"">
                    <span class=""status-label"">Position:</span>
                    <span class=""status-value"" id=""position"">-</span>
                </div>
                <div class=""status-item"">
                    <span class=""status-label"">Players Detected:</span>
                    <span class=""status-value"" id=""player-count"">0</span>
                </div>
            </div>

            <div class=""controls"">
                <button class=""control-btn"" onclick=""centerOnPlayer()"">Center on Player</button>
                <button class=""control-btn secondary"" onclick=""togglePath()"">Toggle Path</button>
                <button class=""control-btn secondary"" onclick=""toggleRadius()"">Toggle Radius</button>
            </div>

            <div class=""legend"">
                <div style=""font-weight: bold; margin-bottom: 10px;"">Legend</div>
                <div class=""legend-item"">
                    <div class=""legend-color"" style=""background: #00ff00;""></div>
                    <span>You</span>
                </div>
                <div class=""legend-item"">
                    <div class=""legend-color"" style=""background: #4CAF50;""></div>
                    <span>Active Player</span>
                </div>
                <div class=""legend-item"">
                    <div class=""legend-color"" style=""background: #ff9800;""></div>
                    <span>AFK Player</span>
                </div>
                <div class=""legend-item"">
                    <div class=""legend-color"" style=""background: #2196F3;""></div>
                    <span>Scan Path</span>
                </div>
            </div>

            <div class=""player-list"">
                <div style=""font-weight: bold; margin-bottom: 10px;"">Detected Players</div>
                <div id=""players""></div>
            </div>
        </div>

        <div id=""map-container"">
            <canvas id=""map-canvas""></canvas>
            <div id=""zoom-controls"">
                <button class=""zoom-btn"" onclick=""zoomIn()"">+</button>
                <button class=""zoom-btn"" onclick=""zoomOut()"">-</button>
            </div>
        </div>
    </div>

    <script>
        const canvas = document.getElementById('map-canvas');
        const ctx = canvas.getContext('2d');

        let viewX = 0, viewY = 0;
        let zoom = 1;
        let isDragging = false;
        let dragStartX = 0, dragStartY = 0;
        let showPath = true;
        let showRadius = true;

        let playerData = null;
        let detectedPlayers = [];
        let pathData = null;
        let config = null;

        // Resize canvas to fill container
        function resizeCanvas() {
            const container = document.getElementById('map-container');
            canvas.width = container.clientWidth;
            canvas.height = container.clientHeight;
            draw();
        }
        window.addEventListener('resize', resizeCanvas);
        resizeCanvas();

        // Game coordinates to screen coordinates
        function gameToScreen(gameX, gameY) {
            const centerX = canvas.width / 2;
            const centerY = canvas.height / 2;

            const offsetX = playerData ? (gameX - playerData.x) : gameX;
            const offsetY = playerData ? (gameY - playerData.z) : gameY; // Z is up in AO

            return {
                x: centerX + (offsetX * zoom) + viewX,
                y: centerY - (offsetY * zoom) + viewY // Invert Y
            };
        }

        // Draw detection radius
        function drawRadius(x, y, radius) {
            if (!showRadius || !playerData) return;

            ctx.beginPath();
            ctx.arc(x, y, radius * zoom, 0, 2 * Math.PI);
            ctx.strokeStyle = 'rgba(255, 255, 0, 0.5)';
            ctx.lineWidth = 2;
            ctx.stroke();
        }

        // Draw player marker
        function drawPlayer(x, y, name, isLocal = false, isAfk = false) {
            const size = isLocal ? 8 : (isAfk ? 6 : 4);
            const color = isLocal ? '#00ff00' : (isAfk ? '#ff9800' : '#4CAF50');

            ctx.beginPath();
            ctx.arc(x, y, size, 0, 2 * Math.PI);
            ctx.fillStyle = color;
            ctx.fill();
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 2;
            ctx.stroke();

            // Draw name
            ctx.font = '12px Arial';
            ctx.fillStyle = '#fff';
            ctx.strokeStyle = '#000';
            ctx.lineWidth = 3;
            ctx.strokeText(name, x + 10, y - 10);
            ctx.fillText(name, x + 10, y - 10);
        }

        // Draw scan path
        function drawPath() {
            if (!showPath || !pathData || !pathData.waypoints || pathData.waypoints.length < 2) return;

            ctx.beginPath();
            const first = gameToScreen(pathData.waypoints[0].x, pathData.waypoints[0].z);
            ctx.moveTo(first.x, first.y);

            for (let i = 1; i < pathData.waypoints.length; i++) {
                const pt = gameToScreen(pathData.waypoints[i].x, pathData.waypoints[i].z);
                ctx.lineTo(pt.x, pt.y);
            }

            if (pathData.isLooping && pathData.waypoints.length > 0) {
                ctx.lineTo(first.x, first.y);
            }

            ctx.strokeStyle = '#2196F3';
            ctx.lineWidth = 2;
            ctx.setLineDash([5, 5]);
            ctx.stroke();
            ctx.setLineDash([]);
        }

        // Main draw function
        function draw() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Draw grid
            ctx.strokeStyle = '#333';
            ctx.lineWidth = 1;
            const gridSize = 100 * zoom;
            for (let x = (viewX % gridSize); x < canvas.width; x += gridSize) {
                ctx.beginPath();
                ctx.moveTo(x, 0);
                ctx.lineTo(x, canvas.height);
                ctx.stroke();
            }
            for (let y = (viewY % gridSize); y < canvas.height; y += gridSize) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(canvas.width, y);
                ctx.stroke();
            }

            // Draw path
            drawPath();

            // Draw local player
            if (playerData) {
                const pos = gameToScreen(playerData.x, playerData.z);
                drawRadius(pos.x, pos.y, playerData.detectionRange);
                drawPlayer(pos.x, pos.y, 'You', true);
            }

            // Draw detected players
            detectedPlayers.forEach(player => {
                const pos = gameToScreen(player.x, player.z);
                drawPlayer(pos.x, pos.y, player.name, false, player.isAfk);
            });
        }

        // Update functions
        async function updateStatus() {
            try {
                const res = await fetch('/api/status');
                const data = await res.json();
                document.getElementById('status').textContent = data.online ? 'Online' : 'Offline';
                document.getElementById('playfield').textContent = data.playfieldName;
            } catch (e) {
                document.getElementById('status').textContent = 'Error';
            }
        }

        async function updatePosition() {
            try {
                const res = await fetch('/api/position');
                playerData = await res.json();
                if (!playerData.error) {
                    document.getElementById('position').textContent =
                        `${playerData.x.toFixed(1)}, ${playerData.y.toFixed(1)}, ${playerData.z.toFixed(1)}`;
                }
            } catch (e) {}
        }

        async function updatePlayers() {
            try {
                const res = await fetch('/api/players');
                detectedPlayers = await res.json();
                document.getElementById('player-count').textContent = detectedPlayers.length;

                const playerList = document.getElementById('players');
                playerList.innerHTML = detectedPlayers.map(p => `
                    <div class=""player-item ${p.isAfk ? 'afk' : ''}"">
                        <div class=""player-name"">${p.name}</div>
                        <div class=""player-info"">
                            Distance: ${p.distance.toFixed(1)}m |
                            ${p.isAfk ? `AFK (${p.afkConfidence}%)` : 'Active'}
                        </div>
                    </div>
                `).join('');
            } catch (e) {}
        }

        async function updatePath() {
            try {
                const res = await fetch('/api/path');
                pathData = await res.json();
            } catch (e) {}
        }

        async function updateConfig() {
            try {
                const res = await fetch('/api/config');
                config = await res.json();
            } catch (e) {}
        }

        // Control functions
        function centerOnPlayer() {
            viewX = 0;
            viewY = 0;
            draw();
        }

        function togglePath() {
            showPath = !showPath;
            draw();
        }

        function toggleRadius() {
            showRadius = !showRadius;
            draw();
        }

        function zoomIn() {
            zoom *= 1.2;
            draw();
        }

        function zoomOut() {
            zoom /= 1.2;
            draw();
        }

        // Mouse controls
        canvas.addEventListener('mousedown', e => {
            isDragging = true;
            dragStartX = e.clientX - viewX;
            dragStartY = e.clientY - viewY;
        });

        canvas.addEventListener('mousemove', e => {
            if (isDragging) {
                viewX = e.clientX - dragStartX;
                viewY = e.clientY - dragStartY;
                draw();
            }
        });

        canvas.addEventListener('mouseup', () => {
            isDragging = false;
        });

        canvas.addEventListener('wheel', e => {
            e.preventDefault();
            if (e.deltaY < 0) {
                zoomIn();
            } else {
                zoomOut();
            }
        });

        // Update loop
        async function update() {
            await Promise.all([
                updateStatus(),
                updatePosition(),
                updatePlayers(),
                updatePath(),
                updateConfig()
            ]);
            draw();
        }

        // Start updates
        update();
        setInterval(update, 1000); // Update every second
    </script>
</body>
</html>";
        }
    }
}
