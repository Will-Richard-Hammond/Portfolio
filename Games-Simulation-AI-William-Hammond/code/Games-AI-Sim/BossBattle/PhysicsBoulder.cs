using MGGameLibrary.Abstract;
using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace BossBattle
{
    public class PhysicsBoulder : PhysicsObject, ICollidable
    {
        public const float Radius = 24f;
        public const float Mass = 12f;
        private const float LinearDamping = 0.975f;

        private readonly Circle _circle;
        public Circle Circle => _circle;
        public Shape Shapes => _circle;

        private readonly List<Rectangle> _walls;

        private Texture2D _texture;

        public PhysicsBoulder(Vector2 position, IEnumerable<Rectangle> walls, Game game)
            : base(Mass, position, game)
        {
            _circle = new Circle(position, Radius);
            _walls  = new List<Rectangle>(walls);
        }

        public bool CollidesWith(ICollidable other)
        {
            if (other?.Shapes == null) return false;
            return _circle.Intersects(other.Shapes);
        }

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
        {
            if (other?.Shapes == null) return false;
            return _circle.Intersects(other.Shapes, ref collisionNormal);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            _circle.Position = Position;

            Velocity *= LinearDamping;

            if (Velocity.LengthSquared() < 0.25f)
                Velocity = Vector2.Zero;

            foreach (Rectangle wall in _walls)
            {
                Vector2 normal = Vector2.Zero;
                if (CircleIntersectsWall(_circle, wall, ref normal))
                {
                    RevertToPreviousPosition();
                    _circle.Position = Position;

                    float vIntoWall = Vector2.Dot(Velocity, -normal);
                    if (vIntoWall > 0f)
                        Velocity += normal * (vIntoWall * 2f);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);

            float shadowScale = (Radius * 2.2f) / _texture.Width;
            spriteBatch.Draw(_texture, Position + new Vector2(3f, 5f), null,
                new Color(20, 15, 10) * 0.45f,
                0f, origin, shadowScale, SpriteEffects.None, 0f);

            float bodyScale = (Radius * 2f) / _texture.Width;
            spriteBatch.Draw(_texture, Position, null,
                new Color(115, 100, 80),
                0f, origin, bodyScale, SpriteEffects.None, 0f);

            float litScale = (Radius * 1.4f) / _texture.Width;
            spriteBatch.Draw(_texture,
                Position - new Vector2(Radius * 0.20f, Radius * 0.22f), null,
                new Color(160, 145, 115) * 0.70f,
                0f, origin, litScale, SpriteEffects.None, 0f);
        }

        private static bool CircleIntersectsWall(Circle circle, Rectangle wall,
                                                  ref Vector2 normal)
        {
            float closestX = MathHelper.Clamp(circle.Centre.X, wall.Left,  wall.Right);
            float closestY = MathHelper.Clamp(circle.Centre.Y, wall.Top,   wall.Bottom);

            float dx = circle.Centre.X - closestX;
            float dy = circle.Centre.Y - closestY;
            float distSq = dx * dx + dy * dy;

            if (distSq > circle.Radius * circle.Radius) return false;

            if (distSq < 1e-8f)
                normal = new Vector2(0f, -1f);
            else
                normal = Vector2.Normalize(new Vector2(dx, dy));

            return true;
        }

        private void EnsureTexture(GraphicsDevice device)
        {
            if (_texture != null) return;

            const int diameter = 64;
            _texture = new Texture2D(device, diameter, diameter);
            Color[] data = new Color[diameter * diameter];

            float r = diameter / 2f;
            Vector2 c = new Vector2(r, r);

            for (int py = 0; py < diameter; py++)
                for (int px = 0; px < diameter; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), c);
                    float a = MathHelper.Clamp(r - dist, 0f, 1f);
                    data[py * diameter + px] = new Color(a, a, a, a);
                }

            _texture.SetData(data);
        }
    }
}
