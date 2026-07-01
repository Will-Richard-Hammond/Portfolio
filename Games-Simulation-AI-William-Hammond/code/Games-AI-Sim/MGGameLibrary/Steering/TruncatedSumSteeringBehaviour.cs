using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Combines multiple steering behaviours by summing their forces,
    /// then truncating the result to a maximum force magnitude.
    /// This prevents runaway acceleration when many forces are active at once.
    /// </summary>
    public class TruncatedSumSteeringBehaviour : SteeringBehaviour
    {
        private readonly List<SteeringBehaviour> _behaviours;
        private readonly float _maxForce;

        public TruncatedSumSteeringBehaviour(List<SteeringBehaviour> behaviours, float maxForce)
        {
            _behaviours = behaviours;
            _maxForce = maxForce;
        }

        public override Vector2 CalculateSteeringForce(Agent agent)
        {
            Vector2 totalForce = Vector2.Zero;

            foreach (SteeringBehaviour behaviour in _behaviours)
                totalForce += behaviour.CalculateSteeringForce(agent);

            if (totalForce.LengthSquared() > _maxForce * _maxForce)
                totalForce = Vector2.Normalize(totalForce) * _maxForce;

            return totalForce;
        }
    }
}
