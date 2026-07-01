using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MGGameLibrary.Shapes
{
    public class Square : Shape
    {
        public float Size { get; set; }
        public Square(Vector2 position, float size) : base(position)
        {
            Size = size;
        }
        public override bool IsInside(Point point)
        {
            return point.X >= Position.X && point.X <= Position.X + Size &&
                   point.Y >= Position.Y && point.Y <= Position.Y + Size;
        }

        public override bool Intersects(Shape other)
        {
            if (other is Circle c)
                return IntersectsCircle(c);

            if (other is Square s)
            {
                // Axis-aligned rectangle overlap
                float leftA = Position.X;
                float rightA = Position.X + Size;
                float topA = Position.Y;
                float bottomA = Position.Y + Size;

                float leftB = s.Position.X;
                float rightB = s.Position.X + s.Size;
                float topB = s.Position.Y;
                float bottomB = s.Position.Y + s.Size;

                bool overlap = !(rightA < leftB || rightB < leftA || bottomA < topB || bottomB < topA);
                return overlap;
            }

            // Fallback to allow other shapes to handle the intersection
            return other.Intersects(this);
        }

        public override bool Intersects(Shape other, ref Vector2 collisionNormal)
        {
            if (other is Circle c)
                return IntersectsCircle(c, ref collisionNormal);

            if (other is Square s)
            {
                // Determine overlap on each axis
                float leftA = Position.X;
                float rightA = Position.X + Size;
                float topA = Position.Y;
                float bottomA = Position.Y + Size;

                float leftB = s.Position.X;
                float rightB = s.Position.X + s.Size;
                float topB = s.Position.Y;
                float bottomB = s.Position.Y + s.Size;

                float overlapX = Math.Min(rightA, rightB) - Math.Max(leftA, leftB);
                float overlapY = Math.Min(bottomA, bottomB) - Math.Max(topA, topB);

                bool intersects = overlapX > 0 && overlapY > 0;

                if (intersects)
                {
                    // Choose the axis of minimum penetration as collision normal
                    Vector2 centerA = new Vector2((leftA + rightA) / 2f, (topA + bottomA) / 2f);
                    Vector2 centerB = new Vector2((leftB + rightB) / 2f, (topB + bottomB) / 2f);
                    Vector2 delta = centerA - centerB;

                    if (overlapX < overlapY)
                    {
                        collisionNormal = new Vector2(delta.X < 0 ? -1f : 1f, 0f);
                    }
                    else
                    {
                        collisionNormal = new Vector2(0f, delta.Y < 0 ? -1f : 1f);
                    }
                }

                return intersects;
            }

            // Fallback
            return other.Intersects(this, ref collisionNormal);
        }

        public override bool IntersectsCircle(Circle other)
        {
            // Clamp circle center to the rectangle to find closest point
            float left = Position.X;
            float right = Position.X + Size;
            float top = Position.Y;
            float bottom = Position.Y + Size;

            float closestX = Math.Clamp(other.Centre.X, left, right);
            float closestY = Math.Clamp(other.Centre.Y, top, bottom);

            float dx = other.Centre.X - closestX;
            float dy = other.Centre.Y - closestY;

            return (dx * dx + dy * dy) <= (other.Radius * other.Radius);
        }

        public override bool IntersectsCircle(Circle other, ref Vector2 collisionNormal)
        {
            float left = Position.X;
            float right = Position.X + Size;
            float top = Position.Y;
            float bottom = Position.Y + Size;

            float closestX = Math.Clamp(other.Centre.X, left, right);
            float closestY = Math.Clamp(other.Centre.Y, top, bottom);

            Vector2 closestPoint = new Vector2(closestX, closestY);
            Vector2 n = other.Centre - closestPoint; // from rect to circle center

            float distSq = n.LengthSquared();
            bool collision = distSq <= other.Radius * other.Radius;

            if (collision)
            {
                if (distSq < 1e-8f)
                {
                    // Circle center is exactly on the rectangle (or inside); pick an arbitrary normal
                    collisionNormal = new Vector2(0f, -1f);
                }
                else
                {
                    collisionNormal = Vector2.Normalize(n);
                }
            }

            return collision;
        }
    }
}
