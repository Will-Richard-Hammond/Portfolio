using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainQuest4_DragonDrop
{
    /// <summary>
    /// A Rock is a circular static obstacle that dragons must navigate around.
    /// Implements ICollidable so it can be passed to steering behaviours that
    /// need to reason about collidable objects in the world.
    /// </summary>
    public class Rock : ICollidable
    {
        public Circle Circle { get; }

        // ICollidable — expose the circle as the collision shape
        public Shape Shapes => Circle;

        public Rock(Vector2 position, float radius)
        {
            Circle = new Circle(position, radius);
        }

        public bool CollidesWith(ICollidable other)
        {
            if (other?.Shapes == null) return false;
            return Shapes.Intersects(other.Shapes);
        }

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
        {
            if (other?.Shapes == null) return false;
            return Shapes.Intersects(other.Shapes, ref collisionNormal);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            int diameter = (int)(Circle.Radius * 2);
            Rectangle dest = new Rectangle(
                (int)(Circle.Centre.X - Circle.Radius),
                (int)(Circle.Centre.Y - Circle.Radius),
                diameter,
                diameter);

            spriteBatch.Draw(texture, dest, Color.White);
        }
    }
}
