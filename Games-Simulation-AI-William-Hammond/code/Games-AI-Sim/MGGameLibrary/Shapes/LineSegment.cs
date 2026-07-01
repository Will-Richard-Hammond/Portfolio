using Microsoft.Xna.Framework;

namespace MGGameLibrary.Shapes
{
    /// <summary>
    /// A line segment defined by a start and end point.
    /// Position (inherited from Shape) is used as the Start.
    /// </summary>
    public class LineSegment : Shape
    {
        public Vector2 Start
        {
            get { return Position; }
            set { Position = value; }
        }

        public Vector2 End { get; set; }

        public LineSegment(Vector2 start, Vector2 end) : base(start)
        {
            End = end;
        }

        // ?? Shape abstract members ????????????????????????????????????????????
        // LineSegment does not have an interior, so IsInside is always false.
        public override bool IsInside(Point point) => false;

        // Double-dispatch intersect methods — full line-vs-shape support would
        // require additions to every other Shape subclass. For now these fall
        // back gracefully; use the static Shape.Intersects(LineSegment, Circle)
        // overload for the line-of-sight check.
        public override bool Intersects(Shape other) => false;
        public override bool Intersects(Shape other, ref Vector2 collisionNormal) => false;
        public override bool IntersectsCircle(Circle other)
            => Shape.Intersects(this, other);
        public override bool IntersectsCircle(Circle other, ref Vector2 collisionNormal)
            => Shape.Intersects(this, other);
    }
}
