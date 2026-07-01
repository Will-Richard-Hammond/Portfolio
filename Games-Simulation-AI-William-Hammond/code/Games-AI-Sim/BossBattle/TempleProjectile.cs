using MGGameLibrary.Abstract;
using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BossBattle
{
    public class TempleProjectile : PhysicsObject, ICollidable
    {
        public const float Radius = 6f;
        public const float Mass = 0.5f;

        private readonly Circle _circle;
        public Circle Circle => _circle;
        public Shape Shapes => _circle;

        public bool IsActive { get; private set; } = true;

        private readonly int _screenW;
        private readonly int _screenH;

        private Texture2D _texture;

        public TempleProjectile(Vector2 position, Vector2 initialVelocity,
                                int screenW, int screenH, Game game)
            : base(Mass, position, game)
        {
            _circle  = new Circle(position, Radius);
            Velocity = initialVelocity;
            _screenW = screenW;
            _screenH = screenH;
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
            if (!IsActive) return;

            base.Update(deltaTime);
            _circle.Position = Position;

            if (Position.X < -Radius  || Position.X > _screenW + Radius ||
                Position.Y < -Radius  || Position.Y > _screenH + Radius)
            {
                IsActive = false;
            }
        }

        public void Deactivate() => IsActive = false;

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);

            float glowScale = (Radius * 3.5f) / _texture.Width;
            spriteBatch.Draw(_texture, Position, null,
                new Color(255, 140, 30) * 0.28f,
                0f, origin, glowScale, SpriteEffects.None, 0f);

            float bodyScale = (Radius * 2f) / _texture.Width;
            spriteBatch.Draw(_texture, Position, null,
                new Color(255, 210, 70),
                0f, origin, bodyScale, SpriteEffects.None, 0f);

            float coreScale = (Radius * 0.9f) / _texture.Width;
            spriteBatch.Draw(_texture, Position, null,
                new Color(255, 248, 210) * 0.92f,
                0f, origin, coreScale, SpriteEffects.None, 0f);
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
