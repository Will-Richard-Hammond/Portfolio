using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MGGameLibrary.Shapes
{
    public abstract class Shape
    {
        protected Vector2 _position;
        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        protected Shape(Vector2 position)
        {
            Position = position;
        }

        public abstract bool IsInside(Point point);
        public abstract bool Intersects(Shape other);
        public abstract bool Intersects(Shape other, ref Vector2 collisionNormal);
        public abstract bool IntersectsCircle(Circle other);
        public abstract bool IntersectsCircle(Circle other, ref Vector2 collisionNormal);

        public static bool Intersects(Circle c1, Circle c2)
        {
            float distanceSquared = Vector2.DistanceSquared(c1.Centre, c2.Centre);
            float radiusSum = c1.Radius + c2.Radius;
            return distanceSquared <= radiusSum * radiusSum;
        }

        public static bool Intersects(Circle c1, Circle c2, ref Vector2 collisionNormal)
        {
            float distanceSquared = Vector2.DistanceSquared(c1.Centre, c2.Centre);
            float radiusSum = c1.Radius + c2.Radius;
            bool collision = distanceSquared <= radiusSum * radiusSum;

            if (collision)
            {
                Vector2 n = c1.Centre - c2.Centre;
                if (n.LengthSquared() < 1e-8f)
                    collisionNormal = new Vector2(0f, -1f);
                else
                    collisionNormal = Vector2.Normalize(n);
            }

            return collision;
        }

        /// <summary>
        /// Returns true if the line segment intersects the circle.
        /// Finds the closest point on the segment to the circle centre using
        /// a parametric projection, then compares its distance to the radius.
        /// </summary>
        public static bool Intersects(LineSegment line, Circle circle)
        {
            // Find the closest point on the line segment to the circle's center
            Vector2 lineDir = line.End - line.Start;
            Vector2 toCircle = circle.Centre - line.Start;

            float t = Vector2.Dot(toCircle, lineDir) / Vector2.Dot(lineDir, lineDir);
            t = MathHelper.Clamp(t, 0.0f, 1.0f);

            Vector2 closestPoint = line.Start + t * lineDir;
            // Check if the distance from the closest point to the circle's center is less than or equal to the radius
            float distanceSquared = Vector2.DistanceSquared(closestPoint, circle.Centre);
            return distanceSquared <= circle.Radius * circle.Radius;
        }
    }
}
