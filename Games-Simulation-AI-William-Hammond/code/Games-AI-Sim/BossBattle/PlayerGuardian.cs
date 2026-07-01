using MGGameLibrary.Abstract;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace BossBattle
{
    public class PlayerGuardian : PhysicsObject
    {
        public Circle Shape { get; }
        public float Speed { get; set; }
        public Vector2 FacingDirection { get; private set; } = -Vector2.UnitY;
        public bool IsStunned => _stunTimer > 0f;
        public float StunTimeRemaining => _stunTimer;


        private readonly List<Rectangle> _walls;

        private readonly Color _bodyColour    = new Color(70, 130, 180);
        private readonly Color _outlineColour = new Color(225, 235, 245);
        private float _stunTimer;
        private Texture2D _circleTexture;

        public PlayerGuardian(Vector2 position, float radius, float speed,
                              IEnumerable<Rectangle> walls, Game game)
            : base(1f, position, game)
        {
            Shape  = new Circle(position, radius);
            Speed  = speed;
            _walls = new List<Rectangle>(walls);
        }

        public void Stun(float seconds)
        {
            if (seconds > _stunTimer)
                _stunTimer = seconds;

            Velocity = Vector2.Zero;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                if (_stunTimer < 0f)
                    _stunTimer = 0f;

                Velocity = Vector2.Zero;
                Shape.Position = Position;
                base.Update(gameTime);
                return;
            }

            KeyboardState ks = Keyboard.GetState();
            Vector2 direction = Vector2.Zero;
            if (ks.IsKeyDown(Keys.W)) direction.Y -= 1f;
            if (ks.IsKeyDown(Keys.S)) direction.Y += 1f;
            if (ks.IsKeyDown(Keys.A)) direction.X -= 1f;
            if (ks.IsKeyDown(Keys.D)) direction.X += 1f;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                FacingDirection = direction;
            }

            MoveWithWallSlide(direction, dt);

            base.Update(gameTime);
        }

        private void MoveWithWallSlide(Vector2 direction, float dt)
        {
            _previousPosition = Position;

            if (direction == Vector2.Zero)
            {
                Velocity = Vector2.Zero;
                Shape.Position = Position;
                return;
            }

            Vector2 desiredVelocity = direction * Speed;
            Velocity = desiredVelocity;

            Vector2 startPosition = Position;
            Vector2 desiredMove = desiredVelocity * dt;

            Position = startPosition + desiredMove;
            Shape.Position = Position;

            if (!CollidesWithAnyWall())
                return;

            Position = startPosition;
            Shape.Position = Position;

            bool blockedX = false;
            bool blockedY = false;

            if (desiredMove.X != 0f)
            {
                Position = new Vector2(startPosition.X + desiredMove.X, startPosition.Y);
                Shape.Position = Position;

                if (CollidesWithAnyWall())
                {
                    blockedX = true;
                    Position = startPosition;
                    Shape.Position = Position;
                }
            }

            Vector2 afterXPosition = Position;

            if (desiredMove.Y != 0f)
            {
                Position = new Vector2(Position.X, Position.Y + desiredMove.Y);
                Shape.Position = Position;

                if (CollidesWithAnyWall())
                {
                    blockedY = true;
                    Position = afterXPosition;
                    Shape.Position = Position;
                }
            }

            if (blockedX || blockedY)
            {
                Velocity = new Vector2(
                    blockedX ? 0f : desiredVelocity.X,
                    blockedY ? 0f : desiredVelocity.Y);
            }
        }

        private bool CollidesWithAnyWall()
        {
            foreach (Rectangle wall in _walls)
                if (CircleIntersectsRectangle(Shape, wall))
                    return true;
            return false;
        }

        private static bool CircleIntersectsRectangle(Circle circle, Rectangle rect)
        {
            float closestX = MathHelper.Clamp(circle.Centre.X, rect.Left, rect.Right);
            float closestY = MathHelper.Clamp(circle.Centre.Y, rect.Top,  rect.Bottom);

            float dx = circle.Centre.X - closestX;
            float dy = circle.Centre.Y - closestY;

            return (dx * dx + dy * dy) <= circle.Radius * circle.Radius;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new Vector2(_circleTexture.Width / 2f, _circleTexture.Height / 2f);

            float outlineScale = (Shape.Radius * 2f)       / _circleTexture.Width;
            float bodyScale    = (Shape.Radius * 2f - 6f)  / _circleTexture.Width;

            spriteBatch.Draw(_circleTexture, Position, null, _outlineColour,
                0f, origin, outlineScale, SpriteEffects.None, 0f);
            Color body = IsStunned ? new Color(95, 105, 125) : _bodyColour;

            spriteBatch.Draw(_circleTexture, Position, null, body,
                0f, origin, bodyScale, SpriteEffects.None, 0f);
        }

        private void EnsureTexture(GraphicsDevice device)
        {
            if (_circleTexture != null) return;

            const int diameter = 64;
            _circleTexture = new Texture2D(device, diameter, diameter);
            Color[] data = new Color[diameter * diameter];

            float r = diameter / 2f;
            Vector2 c = new Vector2(r, r);
            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = MathHelper.Clamp(r - dist, 0f, 1f);
                    data[y * diameter + x] = new Color(a, a, a, a);
                }

            _circleTexture.SetData(data);
        }
    }
}
