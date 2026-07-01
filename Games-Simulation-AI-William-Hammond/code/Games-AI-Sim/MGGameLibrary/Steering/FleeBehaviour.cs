using Microsoft.Xna.Framework;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Steers an Agent away from a target position.
    /// The opposite of SeekBehaviour — desired velocity points FROM target TO agent.
    /// </summary>
    public class FleeBehaviour : SteeringBehaviour
    {
        public ITargetable TargetPosition { get; set; }

        public FleeBehaviour(ITargetable target)
        {
            TargetPosition = target;
        }

        public override Vector2 CalculateSteeringForce(Agent agent)
        {
            Vector2 desiredVelocity = agent.Position - TargetPosition.TargetPosition;
            desiredVelocity.Normalize();
            desiredVelocity *= agent.MaxSpeed;
            return desiredVelocity - agent.Velocity;
        }
    }
}
