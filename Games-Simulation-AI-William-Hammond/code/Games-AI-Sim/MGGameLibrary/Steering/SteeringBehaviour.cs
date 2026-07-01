using Microsoft.Xna.Framework;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Base class for all steering behaviours. Subclasses implement
    /// CalculateSteeringForce to produce a force that drives an Agent.
    /// </summary>
    public abstract class SteeringBehaviour
    {
        public abstract Vector2 CalculateSteeringForce(Agent agent);
    }
}
