using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MGGameLibrary.Shapes
{
    public class Triangle : Shape
    {
        public float Size { get; set; }
        public Triangle(Vector2 position, float size) : base(position)
        {
            Size = size;
        }
        public override bool IsInside(Point point)
        {
            Vector2 p1 = Position;
            Vector2 p2 = new Vector2(Position.X - Size / 2, Position.Y + Size);
            Vector2 p3 = new Vector2(Position.X + Size / 2, Position.Y + Size);

            Vector2 pt = new Vector2(point.X, point.Y);

            // Compute barycentric coordinates
            float denom = (float)((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
            float w1 = ((p2.Y - p3.Y) * (point.X - p3.X) + (p3.X - p2.X) * (point.Y - p3.Y)) / denom;
            float w2 = ((p3.Y - p1.Y) * (point.X - p3.X) + (p1.X - p3.X) * (point.Y - p3.Y)) / denom;
            float w3 = 1 - w1 - w2;

            return w1 >= 0 && w2 >= 0 && w3 >= 0;
        }

        public override bool Intersects(Shape other)
        {
            if (other is Circle c)
                return IntersectsCircle(c);

            if (other is Triangle t)
            {
                // Simple AABB overlap for triangle bounds
                GetBounds(out float minAx, out float maxAx, out float minAy, out float maxAy);
                t.GetBounds(out float minBx, out float maxBx, out float minBy, out float maxBy);

                bool overlap = !(maxAx < minBx || maxBx < minAx || maxAy < minBy || maxBy < minAy);
                return overlap;
            }

            return other.Intersects(this);
        }

        public override bool Intersects(Shape other, ref Vector2 collisionNormal)
        {
            if (other is Circle c)
                return IntersectsCircle(c, ref collisionNormal);

            if (other is Triangle t)
            {
                GetBounds(out float minAx, out float maxAx, out float minAy, out float maxAy);
                t.GetBounds(out float minBx, out float maxBx, out float minBy, out float maxBy);

                float overlapX = Math.Min(maxAx, maxBx) - Math.Max(minAx, minBx);
                float overlapY = Math.Min(maxAy, maxBy) - Math.Max(minAy, minBy);

                bool intersects = overlapX > 0 && overlapY > 0;
                if (intersects)
                {
                    Vector2 centerA = new Vector2((minAx + maxAx) / 2f, (minAy + maxAy) / 2f);
                    Vector2 centerB = new Vector2((minBx + maxBx) / 2f, (minBy + maxBy) / 2f);
                    Vector2 delta = centerA - centerB;

                    if (overlapX < overlapY)
                        collisionNormal = new Vector2(delta.X < 0 ? -1f : 1f, 0f);
                    else
                        collisionNormal = new Vector2(0f, delta.Y < 0 ? -1f : 1f);
                }

                return intersects;
            }

            return other.Intersects(this, ref collisionNormal);
        }

        public override bool IntersectsCircle(Circle other)
        {
            // Check if circle center inside triangle
            if (IsInside(new Point((int)other.Centre.X, (int)other.Centre.Y)))
                return true;

            // Check distance to each edge
            Vector2 p1 = Position;
            Vector2 p2 = new Vector2(Position.X - Size / 2, Position.Y + Size);
            Vector2 p3 = new Vector2(Position.X + Size / 2, Position.Y + Size);

            float r2 = other.Radius * other.Radius;
            if (PointSegmentDistanceSquared(other.Centre, p1, p2) <= r2) return true;
            if (PointSegmentDistanceSquared(other.Centre, p2, p3) <= r2) return true;
            if (PointSegmentDistanceSquared(other.Centre, p3, p1) <= r2) return true;

            return false;
        }

        public override bool IntersectsCircle(Circle other, ref Vector2 collisionNormal)
        {
            // If center inside triangle, choose arbitrary normal
            Vector2 center = other.Centre;
            Vector2 p1 = Position;
            Vector2 p2 = new Vector2(Position.X - Size / 2, Position.Y + Size);
            Vector2 p3 = new Vector2(Position.X + Size / 2, Position.Y + Size);

            if (IsInside(new Point((int)center.X, (int)center.Y)))
            {
                collisionNormal = new Vector2(0f, -1f);
                return true;
            }

            // Find closest point on triangle edges
            float d1 = PointSegmentDistanceSquared(center, p1, p2);
            float d2 = PointSegmentDistanceSquared(center, p2, p3);
            float d3 = PointSegmentDistanceSquared(center, p3, p1);

            float minD = d1;
            Vector2 closest = ClosestPointOnSegment(center, p1, p2);
            if (d2 < minD) { minD = d2; closest = ClosestPointOnSegment(center, p2, p3); }
            if (d3 < minD) { minD = d3; closest = ClosestPointOnSegment(center, p3, p1); }

            bool collision = minD <= other.Radius * other.Radius;
            if (collision)
            {
                Vector2 n = center - closest;
                if (n.LengthSquared() < 1e-8f)
                    collisionNormal = new Vector2(0f, -1f);
                else
                    collisionNormal = Vector2.Normalize(n);
            }

            return collision;
        }

        // Helper: bounding box of triangle
        private void GetBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            Vector2 p1 = Position;
            Vector2 p2 = new Vector2(Position.X - Size / 2, Position.Y + Size);
            Vector2 p3 = new Vector2(Position.X + Size / 2, Position.Y + Size);

            minX = Math.Min(p1.X, Math.Min(p2.X, p3.X));
            maxX = Math.Max(p1.X, Math.Max(p2.X, p3.X));
            minY = Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
            maxY = Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));
        }

        private static float PointSegmentDistanceSquared(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float abLen2 = ab.LengthSquared();
            if (abLen2 == 0f) return Vector2.DistanceSquared(p, a);
            float t = Vector2.Dot(p - a, ab) / abLen2;
            t = Math.Clamp(t, 0f, 1f);
            Vector2 proj = a + ab * t;
            return Vector2.DistanceSquared(p, proj);
        }

        private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float abLen2 = ab.LengthSquared();
            if (abLen2 == 0f) return a;
            float t = Vector2.Dot(p - a, ab) / abLen2;
            t = Math.Clamp(t, 0f, 1f);
            return a + ab * t;
        }
    }
}
