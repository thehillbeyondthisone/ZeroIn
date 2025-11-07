using AOSharp.Common.GameData;
using AOSharp.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroIn.Config;

namespace ZeroIn.GridPattern
{
    /// <summary>
    /// Generates a grid/lawnmower search pattern for a zone area
    /// </summary>
    public class GridGenerator
    {
        private ZoneArea _area;
        private float _spacing;

        public GridGenerator(ZoneArea area, float spacing)
        {
            _area = area;
            _spacing = spacing;
        }

        /// <summary>
        /// Generates a lawnmower pattern across the zone area
        /// </summary>
        public List<GridWaypoint> GeneratePattern()
        {
            if (_area == null || !_area.IsValid())
            {
                Console.WriteLine("[ZeroIn] Cannot generate pattern: Invalid area");
                return new List<GridWaypoint>();
            }

            var waypoints = new List<GridWaypoint>();
            var bounds = _area.GetBounds();

            float minX = bounds.min.X;
            float maxX = bounds.max.X;
            float minY = bounds.min.Y;
            float maxY = bounds.max.Y;
            float avgZ = (bounds.min.Z + bounds.max.Z) / 2f; // Use average Z height

            // Calculate number of rows and columns
            float width = maxX - minX;
            float height = maxY - minY;
            int numRows = (int)Math.Ceiling(height / _spacing) + 1;
            int numCols = (int)Math.Ceiling(width / _spacing) + 1;

            Console.WriteLine($"[ZeroIn] Generating grid pattern:");
            Console.WriteLine($"  Area: {_area.Name}");
            Console.WriteLine($"  Bounds: ({minX:F1}, {minY:F1}) to ({maxX:F1}, {maxY:F1})");
            Console.WriteLine($"  Size: {width:F1} x {height:F1}");
            Console.WriteLine($"  Grid: {numRows} rows x {numCols} columns");
            Console.WriteLine($"  Spacing: {_spacing}m");
            Console.WriteLine($"  Total waypoints: {numRows * numCols}");

            // Generate lawnmower pattern (back and forth)
            for (int row = 0; row < numRows; row++)
            {
                float y = minY + (row * _spacing);

                // Alternate direction for each row (lawnmower pattern)
                if (row % 2 == 0)
                {
                    // Left to right
                    for (int col = 0; col < numCols; col++)
                    {
                        float x = minX + (col * _spacing);
                        waypoints.Add(new GridWaypoint(new Vector3(x, y, avgZ), row, col));
                    }
                }
                else
                {
                    // Right to left
                    for (int col = numCols - 1; col >= 0; col--)
                    {
                        float x = minX + (col * _spacing);
                        waypoints.Add(new GridWaypoint(new Vector3(x, y, avgZ), row, col));
                    }
                }
            }

            return waypoints;
        }

        /// <summary>
        /// Converts grid waypoints to an AOSharp SPath
        /// </summary>
        public SPath GenerateSPath(List<GridWaypoint> waypoints)
        {
            var path = new SPath();

            foreach (var waypoint in waypoints)
            {
                path.Waypoints.Add(waypoint.Position);
            }

            path.IsLooping = false; // Don't loop by default
            path.Lock(); // Lock the path to prevent modification

            return path;
        }

        /// <summary>
        /// Estimates the total distance of the pattern
        /// </summary>
        public float EstimateDistance(List<GridWaypoint> waypoints)
        {
            float totalDistance = 0f;

            for (int i = 1; i < waypoints.Count; i++)
            {
                totalDistance += Vector3.Distance(waypoints[i - 1].Position, waypoints[i].Position);
            }

            return totalDistance;
        }

        /// <summary>
        /// Estimates the time to complete the pattern (in seconds)
        /// </summary>
        public float EstimateTime(List<GridWaypoint> waypoints, float speed = 7f)
        {
            float distance = EstimateDistance(waypoints);
            return distance / speed; // Assuming average run speed of 7 m/s
        }
    }
}
