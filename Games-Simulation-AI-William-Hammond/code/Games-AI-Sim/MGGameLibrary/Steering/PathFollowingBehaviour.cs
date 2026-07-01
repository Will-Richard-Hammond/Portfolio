using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// Steers an Agent along an ordered list of waypoints.
    /// Internally delegates to a SeekBehaviour, swapping its target each time
    /// the agent comes within the arrival threshold of the current waypoint.
    /// The path loops back to the first waypoint when the last is reached.
    /// </summary>
    public class PathFollowingBehaviour : SteeringBehaviour
    {
        private readonly List<ITargetable> _pathPoints;
        private int _currentTargetIndex = 0;
        private readonly float _arrivalThreshold;
        private readonly SeekBehaviour _seekBehaviour;

        public PathFollowingBehaviour(List<ITargetable> pathPoints, float arrivalThreshold = 30f)
        {
            _pathPoints = pathPoints;
            _arrivalThreshold = arrivalThreshold;
            _seekBehaviour = new SeekBehaviour(_pathPoints[0]);
        }

        public override Vector2 CalculateSteeringForce(Agent agent)
        {
            float distanceToTarget = Vector2.DistanceSquared(
                agent.Position,
                _pathPoints[_currentTargetIndex].TargetPosition);

            if (distanceToTarget < _arrivalThreshold * _arrivalThreshold)
            {
                _currentTargetIndex++;

                if (_currentTargetIndex >= _pathPoints.Count)
                    _currentTargetIndex = 0; // Loop back to start

                _seekBehaviour.TargetPosition = _pathPoints[_currentTargetIndex];
            }

            return _seekBehaviour.CalculateSteeringForce(agent);
        }
    }
}
