using Microsoft.Xna.Framework;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Minimal ITargetable wrapper around a fixed Vector2 position.
    /// Used to specify waypoints for path-following behaviours.
    /// </summary>
    public class SimpleTargetable : ITargetable
    {
        public Vector2 TargetPosition { get; }
        public SimpleTargetable(Vector2 pos) => TargetPosition = pos;
    }
}
