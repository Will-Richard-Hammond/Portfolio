// [REFACTOR] Not used in the refactored scene. ComponentCollisionLine is a legacy
// component with no engine equivalent — line/line collision has no current gameplay use.
// The refactored path uses EngineWorldCollisionService (AABB) and DroneService (distance).
// This class is retained so the legacy EntityManager registry and SystemManager still compile.

using System;
using System.Collections.Generic;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemCollisionLineLine : System
    {
        public string Name => "SystemCollisionLineLine";

        struct LineEntry
        {
            public Entity Entity;
            public Vector2 A;
            public Vector2 B;
        }

        readonly List<LineEntry> _lines = new List<LineEntry>(64);

        // [REFACTOR] OnAction body has no refactored equivalent — no entities carry
        // ComponentCollisionLine in the refactored path; all wall collision is AABB-based.
        public override void OnAction(List<Entity> entities)
        {
            /*
            _lines.Clear();
            if (_lines.Capacity < entities.Count)
                _lines.Capacity = entities.Count;

            foreach (var entity in entities)
            {
                var pos = GetComponent(entity, ComponentTypes.COMPONENT_POSITION) as ComponentPosition;
                var lineComp = GetComponent(entity, ComponentTypes.COMPONENT_COLLISION_LINE) as ComponentCollisionLine;
                if (pos == null || lineComp == null) continue;

                Vector3 ws = pos.Position + lineComp.LocalStart;
                Vector3 we = pos.Position + lineComp.LocalEnd;
                _lines.Add(new LineEntry
                {
                    Entity = entity,
                    A = new Vector2(ws.X, ws.Z),
                    B = new Vector2(we.X, we.Z)
                });
            }

            int n = _lines.Count;
            if (n < 2) return;

            for (int i = 0; i < n; ++i)
            {
                var li = _lines[i];
                for (int j = i + 1; j < n; ++j)
                {
                    var lj = _lines[j];
                    if (SegmentsIntersect(li.A, li.B, lj.A, lj.B)) { }
                }
            }
            */
        }

        static float Orient(Vector2 a, Vector2 b, Vector2 c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        static bool OnSegment(Vector2 a, Vector2 b, Vector2 p) =>
            Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) &&
            Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float o1 = Orient(p1, p2, q1), o2 = Orient(p1, p2, q2);
            float o3 = Orient(q1, q2, p1), o4 = Orient(q1, q2, p2);
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, p2, q2)) return true;
            if (o3 == 0 && OnSegment(q1, q2, p1)) return true;
            if (o4 == 0 && OnSegment(q1, q2, p2)) return true;
            return (o1 > 0 && o2 < 0 || o1 < 0 && o2 > 0) &&
                   (o3 > 0 && o4 < 0 || o3 < 0 && o4 > 0);
        }

        public override void OnAction(Entity entity) { }
    }
}
