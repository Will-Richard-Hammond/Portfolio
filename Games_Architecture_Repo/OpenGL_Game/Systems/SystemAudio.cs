// [REFACTOR] Superseded by RefactorGameScene.SyncAudioListener().
// SyncAudioListener() directly sets AL.Listener position/velocity/orientation each frame
// from the EngineCamera, removing the need to scan entity components to locate audio sources.
// This class is retained so the legacy GameScene system list still compiles.

using System;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemAudio : System
    {
        const ComponentTypes MASK = (ComponentTypes.COMPONENT_POSITION | ComponentTypes.COMPONENT_AUDIO);

        public SystemAudio()
        {
        }

        public new string Name
        {
            get { return "SystemAudio"; }
        }

        // [REFACTOR] OnAction body superseded by RefactorGameScene.SyncAudioListener(),
        // which calls AL.Listener() directly using EngineCamera.Position/Direction/Up.
        public override void OnAction(Entity entity)
        {
            /*
            if ((entity.Mask & MASK) != MASK) return;

            ComponentPosition posComp = null;
            ComponentAudio audioComp = null;

            foreach (var component in entity.Components)
            {
                if (component is ComponentPosition p) posComp = p;
                else if (component is ComponentAudio a) audioComp = a;
            }

            if (posComp != null && audioComp != null)
            {
                Vector3 pos = posComp.Position;
                audioComp.SetPosition(pos);
            }
            */
        }
    }
}