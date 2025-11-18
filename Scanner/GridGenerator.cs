using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Pathfinding;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Generates search patterns (lawnmower grids) within a closed polygon boundary.
    /// </summary>
    public static class GridGenerator
    {
        /// <summary>
        /// Generates a lawnmower pattern that fills a closed polygon.
        /// </summary>
        /// <param name="boundary">Closed polygon boundary (first and last points should be near each other)</param>
        /// <param name="spacing">Distance between parallel scan lines (default 40m)</param>
        /// <returns>List of waypoints following a lawnmower pattern</returns>
        public static List<Vector3> GenerateLawnmowerPattern(List<Vector3> boundary, float spacing = 40f)
        {
            if (boundary == null || boundary.Count < 3)
                throw new ArgumentException("Boundary must have at least 3 points");

            // Calculate bounding box
            float minX = boundary.Min(p => p.X);
            float maxX = boundary.Max(p => p.X);
            float minZ = boundary.Min(p => p.Z);
            float maxZ = boundary.Max(p => p.Z);

            // Use average Y height
            float avgY = boundary.Average(p => p.Y);

            var waypoints = new List<Vector3>();

            // Generate horizontal scan lines
            bool leftToRight = true;
            for (float z = minZ; z <= maxZ; z += spacing)
            {
                if (leftToRight)
                {
                    // Scan from left to right
                    for (float x = minX; x <= maxX; x += spacing / 2) // Half spacing for smoother coverage
                    {
                        var point = new Vector3(x, avgY, z);
                        if (IsPointInPolygon(point, boundary))
                        {
                            waypoints.Add(point);
                        }
                    }
                }
                else
                {
                    // Scan from right to left
                    for (float x = maxX; x >= minX; x -= spacing / 2)
                    {
                        var point = new Vector3(x, avgY, z);
                        if (IsPointInPolygon(point, boundary))
                        {
                            waypoints.Add(point);
                        }
                    }
                }

                leftToRight = !leftToRight;
            }

            return waypoints;
        }

        /// <summary>
        /// Determines if a path forms a closed loop.
        /// </summary>
        public static bool IsClosedLoop(List<Vector3> path, float tolerance = 5f)
        {
            if (path == null || path.Count < 3)
                return false;

            var first = path[0];
            var last = path[path.Count - 1];

            float distance = Vector3.Distance(first, last);
            return distance <= tolerance;
        }

        /// <summary>
        /// Point-in-polygon test using ray casting algorithm.
        /// </summary>
        private static bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i].Z < point.Z && polygon[j].Z >= point.Z ||
                     polygon[j].Z < point.Z && polygon[i].Z >= point.Z) &&
                    (polygon[i].X <= point.X || polygon[j].X <= point.X))
                {
                    if (polygon[i].X + (point.Z - polygon[i].Z) / (polygon[j].Z - polygon[i].Z) * (polygon[j].X - polygon[i].X) < point.X)
                    {
                        inside = !inside;
                    }
                }

                j = i;
            }

            return inside;
        }

        /// <summary>
        /// Generates a spiral pattern within a closed polygon.
        /// </summary>
        public static List<Vector3> GenerateSpiralPattern(List<Vector3> boundary, float spacing = 40f)
        {
            // For now, use lawnmower (spiral is more complex)
            // TODO: Implement true spiral if lawnmower isn't sufficient
            return GenerateLawnmowerPattern(boundary, spacing);
        }
    }
}
