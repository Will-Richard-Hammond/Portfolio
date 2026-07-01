using MGGameLibrary.Shapes;
using MGGameLibrary.Steering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainQuest4_DragonDrop
{
    /// <summary>
    /// A Coin is a collectable game object placed at a fixed position in the world.
    /// Dragons will seek out and collect these coins.
    /// </summary>
    public class Coin : GameComponent, ITargetable
    {
        public Vector2 Position { get; set; }
        public bool IsCollected { get; private set; }

        // ITargetable — exposes the coin's world position to steering behaviours
        public Vector2 TargetPosition => Position;

        // Collision/hit-test shape — always in sync with Position
        public Circle Circle => new Circle(Position, COIN_SIZE / 2f);

        private const int COIN_SIZE = 32;

        public Coin(Vector2 position, Game game) : base(game)
        {
            Position = position;
            IsCollected = false;
        }

        /// <summary>Moves the coin centre to the given world position.</summary>
        public void MoveCoin(Vector2 newPosition)
        {
            Position = newPosition;
        }

        public void Collect()
        {
            IsCollected = true;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (IsCollected) return;

            // Isolate the first coin from the 4×4 sprite sheet
            Rectangle src = new Rectangle(0, 0, texture.Width / 4, texture.Height / 4);

            Rectangle dest = new Rectangle(
                (int)(Position.X - COIN_SIZE / 2),
                (int)(Position.Y - COIN_SIZE / 2),
                COIN_SIZE,
                COIN_SIZE);

            spriteBatch.Draw(texture, dest, src, Color.White);
        }
    }
}
