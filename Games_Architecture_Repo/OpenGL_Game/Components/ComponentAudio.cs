using System;
using OpenTK.Mathematics;
using OpenTK.Audio.OpenAL;

namespace OpenGL_Game.Components
{
    class ComponentAudio : IComponent, ICloseableComponent
    {
        int sourceId;
        int bufferId;
        bool isValid = false;

        public ComponentAudio(string filename, bool loop = false)
        {
            bufferId = Managers.ResourceManager.LoadAudio(filename);
            sourceId = AL.GenSource();
            AL.Source(sourceId, ALSourcei.Buffer, bufferId);
            AL.Source(sourceId, ALSourceb.Looping, loop);
            // reasonable defaults
            AL.Source(sourceId, ALSourcef.Gain, 1.0f);
            AL.Source(sourceId, ALSourcef.Pitch, 1.0f);
            isValid = true;
        }

        public ComponentTypes ComponentType
        {
            get { return ComponentTypes.COMPONENT_AUDIO; }
        }

        public void SetPosition(Vector3 emitterPosition)
        {
            if (!isValid) return;
            AL.Source(sourceId, ALSource3f.Position, emitterPosition.X, emitterPosition.Y, emitterPosition.Z);
        }

        public void SetVelocity(Vector3 velocity)
        {
            if (!isValid) return;
            AL.Source(sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
        }

        public void Play()
        {
            if (!isValid) return;
            AL.SourcePlay(sourceId);
        }

        public void Stop()
        {
            if (!isValid) return;
            AL.SourceStop(sourceId);
            AL.SourceRewind(sourceId);
        }

        public void Close()
        {
            if (!isValid) return;
            try
            {
                AL.SourceStop(sourceId);
            }
            catch { }
            try
            {
                AL.DeleteSource(sourceId);
            }
            catch { }
            isValid = false;
        }
    }
}