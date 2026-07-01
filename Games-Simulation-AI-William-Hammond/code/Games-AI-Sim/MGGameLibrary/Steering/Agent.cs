using MGGameLibrary.Abstract;
using Microsoft.Xna.Framework;
using System;

namespace MGGameLibrary.Steering
{
    /// <summary>
    /// An Agent is a game entity that can perceive its environment, make decisions,
    /// and act autonomously. It owns a PhysicsObject for position/movement and
    /// a Heading (in radians from "up") to track which direction it faces.
    /// Implements ITargetable so other agents can seek this one.
    /// </summary>
    public class Agent : GameComponent, ITargetable
    {
        private PhysicsObject _physicsObject;
        private SteeringBehaviour _behaviour;

        public float Heading { get; set; }
        public float MaxSpeed { get; set; }

        public Vector2 Position
        {
            get { return _physicsObject.Position; }
            set { _physicsObject.Position = value; }
        }

        public Vector2 Velocity => _physicsObject.Velocity;

        // ITargetable — any other agent can seek this agent's position
        public Vector2 TargetPosition => Position;

        public Agent(Vector2 position, float heading, Game game, SteeringBehaviour behaviour, float maxSpeed = 150f) : base(game)
        {
            _physicsObject = new PhysicsObject(1f, position, game);
            Heading = heading;
            _behaviour = behaviour;
            MaxSpeed = maxSpeed;
        }

        public override void Update(GameTime gameTime)
        {
            // Calculate steering force and apply to physics object
            Vector2 steeringForce = _behaviour.CalculateSteeringForce(this);
            _physicsObject.ApplyForce(steeringForce);
            _physicsObject.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            // Align heading to velocity so the sprite faces its direction of travel
            Vector2 velocity = _physicsObject.Velocity;
            if (velocity.LengthSquared() > 0)
            {
                velocity = Vector2.Normalize(velocity);
                Heading = MathF.Atan2(velocity.Y, velocity.X) + MathF.PI / 2;
            }

            base.Update(gameTime);
        }
    }
}
