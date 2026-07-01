using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Steers an Agent away from collidable obstacles using whisker probes.
    /// Each whisker is a local-space vector that is rotated into world space
    /// using the agent's current heading. If a whisker intersects a collidable
    /// circle a repulsion force is added to push the agent away from it.
    /// </summary>
    public class AvoidCollidablesWithWhiskersBehaviour : SteeringBehaviour
    {
        private readonly List<ICollidable> _collidableList;

        // Whiskers expressed in local space (heading = 0 faces negative Y)
        private readonly List<Vector2> _whiskers;

        public AvoidCollidablesWithWhiskersBehaviour(
            List<ICollidable> collidables,
            List<Vector2> whiskers)
        {
            _collidableList = collidables;
            _whiskers = whiskers;
        }

        public override Vector2 CalculateSteeringForce(Agent agent)
        {
            Vector2 force = Vector2.Zero;

            foreach (ICollidable collidable in _collidableList)
            {
                if (collidable.Shapes is Circle circle) // yuck - should be using some form of double dispatch here
                {
                    foreach (Vector2 whisker in _whiskers)
                    {
                        // Rotate whisker from local space into world space using the agent's heading
                        Vector2 rotatedWhisker = Vector2.Transform(
                            whisker,
                            Matrix.CreateRotationZ(agent.Heading));

                        LineSegment probe = new LineSegment(
                            agent.Position,
                            agent.Position + rotatedWhisker);

                        if (Shape.Intersects(probe, circle))
                        {
                            // Flee from the circle centre.
                            // A more refined approach would project a point slightly outside
                            // the circle perimeter to produce a smoother steering arc.
                            Vector2 desiredVelocity = agent.Position - circle.Centre;
                            desiredVelocity.Normalize();
                            desiredVelocity *= agent.MaxSpeed;
                            force += desiredVelocity - agent.Velocity;
                        }
                    }
                }
            }

            return force;
        }
    }
}
