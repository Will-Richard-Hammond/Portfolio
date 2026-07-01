// [REFACTOR] Superseded by EngineWorldCollisionService.
// EngineWorldCollisionService.ResolvePlayerMovement() performs per-axis swept AABB resolution
// against all EngineEntity wall/floor entities using AabbCollisionComponent + TransformComponent.
// This class is retained so the legacy SystemManager list still compiles.

using System;
using System.Collections.Generic;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemCollisionPointInAABB : System
    {
        public string Name
        {
            get { return "SystemCollisionPointInAABB"; }
        }

        // [REFACTOR] OnAction body superseded by EngineWorldCollisionService.ResolvePlayerMovement().
        // The engine collision service resolves the player AABB against all wall entities each frame.
        public override void OnAction(List<Entity> entities)
        {
            /*
            var camera = GameScene.gameInstance?.camera;
            if (camera == null) return;
            Vector3 point = camera.cameraPosition;

            foreach (var entity in entities)
            {
                ComponentPosition pos = null;
                ComponentCollisionAABB aabb = null;
                foreach (var c in entity.Components)
                {
                    if (c is ComponentPosition p) pos = p;
                    else if (c is ComponentCollisionAABB a) aabb = a;
                }
                if (pos == null || aabb == null) continue;

                Vector3 worldMin = pos.Position + aabb.LocalMin;
                Vector3 worldMax = pos.Position + aabb.LocalMax;

                bool inside =
                    point.X >= worldMin.X && point.X <= worldMax.X &&
                    point.Y >= worldMin.Y && point.Y <= worldMax.Y &&
                    point.Z >= worldMin.Z && point.Z <= worldMax.Z;
            }
            */
        }

        public override void OnAction(Entity entity) { }
    }
}

