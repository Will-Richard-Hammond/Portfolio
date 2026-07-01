using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGGameLibrary.Abstract
{
    public class PhysicsObject : GameComponent
    {
        protected float _mass;
        protected Vector2 _position;
        protected Vector2 _previousPosition;
        protected Vector2 _velocity;
        protected Vector2 _force;

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector2 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        public PhysicsObject(float mass, Vector2 position, Game game) : base(game)
        {
            _mass = mass <= 0f ? 0.0001f : mass;
            _position = position;
            _previousPosition = position;
            _velocity = Vector2.Zero;
            _force = Vector2.Zero;
        }

        public void ApplyForce(Vector2 force) => _force += force;
        public void ApplyGravity()
        {
            const float GRAVITY = 250f; // pixels per second squared
            _force += new Vector2(0, GRAVITY * _mass);
        }
        public void ApplyImpulse(Vector2 changeInVelocity, float deltaTime)
        {
            // F = m * dv / dt
            Vector2 force = _mass * changeInVelocity / deltaTime;
            ApplyForce(force);
        }
        public virtual void Update(float deltaTime)
        {
            _previousPosition = _position;
            Vector2 acceleration = _force / _mass;
            _velocity += acceleration * deltaTime;
            _position += _velocity * deltaTime;
            _force = Vector2.Zero;
        }

        public void RevertToPreviousPosition() => _position = _previousPosition;
    }
}
