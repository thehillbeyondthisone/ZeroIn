using AOSharp.Common.GameData;

namespace ZeroIn.GridPattern
{
    /// <summary>
    /// Represents a waypoint in the grid scan pattern
    /// </summary>
    public class GridWaypoint
    {
        public Vector3 Position { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public bool Visited { get; set; }

        public GridWaypoint(Vector3 position, int row, int column)
        {
            Position = position;
            RowNumber = row;
            ColumnNumber = column;
            Visited = false;
        }

        public override string ToString()
        {
            return $"Waypoint[{RowNumber},{ColumnNumber}] at ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1})";
        }
    }
}
