using Microsoft.Xna.Framework;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Anything that can be sought or fled from by a steering behaviour.
    /// </summary>
    public interface ITargetable
    {
        Vector2 TargetPosition { get; }
    }
}
