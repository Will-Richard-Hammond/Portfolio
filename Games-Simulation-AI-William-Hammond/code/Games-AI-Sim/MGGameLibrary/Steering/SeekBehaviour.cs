using Microsoft.Xna.Framework;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Steers an Agent towards a target position.
    /// Desired velocity = direction to target * MaxSpeed.
    /// Steering force = desired velocity - current velocity.
    /// </summary>
    public class SeekBehaviour : SteeringBehaviour
    {
        public ITargetable TargetPosition { get; set; }

        public SeekBehaviour(ITargetable target)
        {
            TargetPosition = target;
        }

        public override Vector2 CalculateSteeringForce(Agent agent)
        {
            Vector2 desiredVelocity = TargetPosition.TargetPosition - agent.Position;
            desiredVelocity.Normalize();
            desiredVelocity *= agent.MaxSpeed;
            return desiredVelocity - agent.Velocity;
        }
    }
}
