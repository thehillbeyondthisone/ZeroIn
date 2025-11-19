using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using AOSharp.Core;

namespace ZeroIn.PlanetMap
{
    /// <summary>
    /// Loads and transforms game coordinates to map pixel coordinates
    /// </summary>
    public class MapCoordinateLoader
    {
        private Dictionary<uint, PlayfieldMapInfo> _playfieldMaps = new Dictionary<uint, PlayfieldMapInfo>();
        private string _mapDataPath;

        public MapCoordinateLoader(string mapDataPath)
        {
            _mapDataPath = mapDataPath;
        }

        /// <summary>
        /// Load map coordinates from MapCoordinates.xml
        /// </summary>
        public bool LoadCoordinates(string xmlPath)
        {
            try
            {
                if (!File.Exists(xmlPath))
                {
                    Chat.WriteLine($"[MapCoordinateLoader] MapCoordinates.xml not found at: {xmlPath}", ChatColor.Red);
                    return false;
                }

                var doc = XDocument.Load(xmlPath);
                int count = 0;

                foreach (var element in doc.Root.Elements("Playfield"))
                {
                    var info = new PlayfieldMapInfo
                    {
                        Id = uint.Parse(element.Attribute("id").Value),
                        Name = element.Attribute("name").Value,
                        X = float.Parse(element.Attribute("x").Value),
                        Z = float.Parse(element.Attribute("z").Value),
                        XScale = float.Parse(element.Attribute("xscale").Value),
                        ZScale = float.Parse(element.Attribute("zscale").Value)
                    };

                    _playfieldMaps[info.Id] = info;
                    count++;
                }

                Chat.WriteLine($"[MapCoordinateLoader] Loaded {count} playfield coordinate mappings", ChatColor.Green);
                return true;
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[MapCoordinateLoader] Error loading coordinates: {ex.Message}", ChatColor.Red);
                return false;
            }
        }

        /// <summary>
        /// Transform game world coordinates to map pixel coordinates
        /// </summary>
        public MapPosition GameToMap(uint playfieldId, float gameX, float gameZ)
        {
            if (!_playfieldMaps.TryGetValue(playfieldId, out var mapInfo))
            {
                return new MapPosition
                {
                    Valid = false,
                    Message = $"No map data for playfield {playfieldId}"
                };
            }

            // Transform game coordinates using the map's reference point and scale
            // The MapCoordinates.xml contains the center/reference point (x, z)
            // and scale factors (xscale, zscale)

            // Calculate offset from map reference point
            float offsetX = (gameX - mapInfo.X) * mapInfo.XScale;
            float offsetZ = (gameZ - mapInfo.Z) * mapInfo.ZScale;

            return new MapPosition
            {
                Valid = true,
                PlayfieldId = playfieldId,
                PlayfieldName = mapInfo.Name,
                MapX = offsetX,
                MapZ = offsetZ,
                GameX = gameX,
                GameZ = gameZ,
                Message = "OK"
            };
        }

        /// <summary>
        /// Get map info for a playfield
        /// </summary>
        public PlayfieldMapInfo GetPlayfieldInfo(uint playfieldId)
        {
            _playfieldMaps.TryGetValue(playfieldId, out var info);
            return info;
        }

        /// <summary>
        /// Get all loaded playfield IDs
        /// </summary>
        public IEnumerable<uint> GetLoadedPlayfields()
        {
            return _playfieldMaps.Keys;
        }
    }

    /// <summary>
    /// Map information for a playfield
    /// </summary>
    public class PlayfieldMapInfo
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Z { get; set; }
        public float XScale { get; set; }
        public float ZScale { get; set; }
    }

    /// <summary>
    /// Result of coordinate transformation
    /// </summary>
    public class MapPosition
    {
        public bool Valid { get; set; }
        public uint PlayfieldId { get; set; }
        public string PlayfieldName { get; set; }
        public float MapX { get; set; }
        public float MapZ { get; set; }
        public float GameX { get; set; }
        public float GameZ { get; set; }
        public string Message { get; set; }
    }
}
