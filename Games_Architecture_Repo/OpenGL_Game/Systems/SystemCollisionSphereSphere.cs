// [REFACTOR] Superseded by EngineWorldCollisionService.
// Sphere-sphere detection has no direct engine equivalent yet; however the refactored
// gameplay collision (drone vs player proximity) is handled in DroneService.UpdateDrones()
// via straight distance checks, making this generalised sphere-sphere system redundant
// for the refactored path.
// This class is retained so the legacy SystemManager list still compiles.

using System;
using System.Collections.Generic;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemCollisionSphereSphere : System
    {
        public string Name
        {
            get { return "SystemCollisionSphereSphere"; }
        }

        // [REFACTOR] OnAction body superseded — drone/player proximity is now handled
        // inside DroneService.UpdateDrones() using a plain horizontal distance check.
        public override void OnAction(List<Entity> entities)
        {
            /*
            int n = entities.Count;
            for (int i = 0; i < n; ++i)
            {
                Entity a = entities[i];
                ComponentPosition posA = null;
                ComponentCollisionSphere sA = null;
                foreach (var c in a.Components)
                {
                    if (c is ComponentPosition p) posA = p;
                    else if (c is ComponentCollisionSphere s) sA = s;
                }
                if (posA == null || sA == null) continue;

                for (int j = i + 1; j < n; ++j)
                {
                    Entity b = entities[j];
                    ComponentPosition posB = null;
                    ComponentCollisionSphere sB = null;
                    foreach (var c in b.Components)
                    {
                        if (c is ComponentPosition p) posB = p;
                        else if (c is ComponentCollisionSphere s) sB = s;
                    }
                    if (posB == null || sB == null) continue;

                    float combined = sA.Radius + sB.Radius;
                    float distSq = (posA.Position - posB.Position).LengthSquared;
                    if (distSq <= combined * combined)
                    {
                        // collision response
                    }
                }
            }
            */
        }

        public override void OnAction(Entity entity) { }
    }
}
