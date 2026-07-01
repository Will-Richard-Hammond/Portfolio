using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MGGameLibrary.Shapes
{
    public class Circle : Shape
    {
        public float Radius { get; set; }
        public Vector2 Centre => Position;

        public Circle(Vector2 position, float radius) : base(position)
        {
            Radius = radius;
        }

        public override bool IsInside(Point point)
        {
            float dx = point.X - Position.X;
            float dy = point.Y - Position.Y;
            return (dx * dx + dy * dy) <= (Radius * Radius);
        }

        public override bool Intersects(Shape other)
        {
            return other.IntersectsCircle(this);
        }

        public override bool Intersects(Shape other, ref Vector2 collisionNormal)
        {
            return other.IntersectsCircle(this, ref collisionNormal);
        }

        public override bool IntersectsCircle(Circle other)
        {
            return Shape.Intersects(this, other);
        }

        public override bool IntersectsCircle(Circle other, ref Vector2 collisionNormal)
        {
            return Shape.Intersects(this, other, ref collisionNormal);
        }
    }
}
