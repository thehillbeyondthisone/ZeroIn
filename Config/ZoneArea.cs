using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;

namespace ZeroIn.Config
{
    /// <summary>
    /// Represents a zone area defined by four corner coordinates
    /// </summary>
    public class ZoneArea
    {
        public string Name { get; set; }
        public Vector3 Corner1 { get; set; }
        public Vector3 Corner2 { get; set; }
        public Vector3 Corner3 { get; set; }
        public Vector3 Corner4 { get; set; }

        public ZoneArea()
        {
            Name = "Unnamed Area";
            Corner1 = Vector3.Zero;
            Corner2 = Vector3.Zero;
            Corner3 = Vector3.Zero;
            Corner4 = Vector3.Zero;
        }

        public ZoneArea(string name, Vector3 c1, Vector3 c2, Vector3 c3, Vector3 c4)
        {
            Name = name;
            Corner1 = c1;
            Corner2 = c2;
            Corner3 = c3;
            Corner4 = c4;
        }

        /// <summary>
        /// Gets the approximate center of the area
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3(
                (Corner1.X + Corner2.X + Corner3.X + Corner4.X) / 4f,
                (Corner1.Y + Corner2.Y + Corner3.Y + Corner4.Y) / 4f,
                (Corner1.Z + Corner2.Z + Corner3.Z + Corner4.Z) / 4f
            );
        }

        /// <summary>
        /// Gets the bounding box min/max coordinates
        /// </summary>
        public (Vector3 min, Vector3 max) GetBounds()
        {
            float minX = Math.Min(Math.Min(Corner1.X, Corner2.X), Math.Min(Corner3.X, Corner4.X));
            float maxX = Math.Max(Math.Max(Corner1.X, Corner2.X), Math.Max(Corner3.X, Corner4.X));
            float minY = Math.Min(Math.Min(Corner1.Y, Corner2.Y), Math.Min(Corner3.Y, Corner4.Y));
            float maxY = Math.Max(Math.Max(Corner1.Y, Corner2.Y), Math.Max(Corner3.Y, Corner4.Y));
            float minZ = Math.Min(Math.Min(Corner1.Z, Corner2.Z), Math.Min(Corner3.Z, Corner4.Z));
            float maxZ = Math.Max(Math.Max(Corner1.Z, Corner2.Z), Math.Max(Corner3.Z, Corner4.Z));

            return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        /// <summary>
        /// Validates that all corners have been set
        /// </summary>
        public bool IsValid()
        {
            return Corner1 != Vector3.Zero &&
                   Corner2 != Vector3.Zero &&
                   Corner3 != Vector3.Zero &&
                   Corner4 != Vector3.Zero;
        }
    }
}
