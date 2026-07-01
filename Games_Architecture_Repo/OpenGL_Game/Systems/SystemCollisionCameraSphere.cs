// [REFACTOR] Not used in the refactored scene. This system accesses GameScene.gameInstance
// directly and is never registered in any SystemManager in the refactored path.
// Camera-sphere collision is replaced by DroneService distance checks and
// EngineWorldCollisionService AABB resolution in RefactorGameScene.
// This class is retained so legacy code still compiles.

using System;
using System.Collections.Generic;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemCollisionCameraSphere : System
    {
        public string Name
        {
            get { return "SystemCollisionCameraSphere"; }
        }

        // [REFACTOR] OnAction body not used in refactored path — drone proximity is handled
        // by DroneService.UpdateDrones() and player wall collision by EngineWorldCollisionService.
        public override void OnAction(List<Entity> entities)
        {
            /*
            var scene = GameScene.gameInstance;
            if (scene == null) return;

            var player = scene.playerEntity;
            if (player == null) return;

            ComponentPosition playerPos = null;
            ComponentCollisionSphere playerSphere = null;
            foreach (var c in player.Components)
            {
                if (c is ComponentPosition p) playerPos = p;
                else if (c is ComponentCollisionSphere s) playerSphere = s;
            }
            if (playerPos == null || playerSphere == null) return;

            Vector3 pPos = playerPos.Position;
            float pRadius = playerSphere.Radius;

            foreach (var entity in entities)
            {
                if (entity == player) continue;

                ComponentPosition pos = null;
                ComponentCollisionSphere sphere = null;
                foreach (var c in entity.Components)
                {
                    if (c is ComponentPosition p) pos = p;
                    else if (c is ComponentCollisionSphere s) sphere = s;
                }
                if (pos == null || sphere == null) continue;

                float combined = pRadius + sphere.Radius;
                float distSq = (pos.Position - pPos).LengthSquared;
                if (distSq <= combined * combined)
                {
                    if (entity.Name == "IGSS")
                    {
                        scene.camera.cameraPosition = scene.cameraStartPosition;
                        scene.camera.UpdateView();
                        var plPosComp = player.Components.Find(c => c is ComponentPosition) as ComponentPosition;
                        if (plPosComp != null) plPosComp.Position = scene.camera.cameraPosition;
                    }
                }
            }
            */
        }

        public override void OnAction(Entity entity) { }
    }
}
