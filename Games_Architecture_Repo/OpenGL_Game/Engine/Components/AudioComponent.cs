using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Engine.Components
{
    /// <summary>
    /// Engine-owned audio source component. Wraps an OpenAL source and a
    /// ResourceManager-cached buffer so the source can be played on demand.
    /// Lifetime is managed by AudioService.Close().
    /// </summary>
    class AudioComponent : IEngineComponent
    {
        readonly int sourceId;
        bool isValid;

        public EngineComponentType ComponentType => EngineComponentType.Audio;

        public AudioComponent(string filePath, bool loop = false, float gain = 1.0f, float pitch = 1.0f, float referenceDistance = 1.0f)
        {
            int bufferId = ResourceManager.LoadAudio(filePath);
            sourceId = AL.GenSource();
            AL.Source(sourceId, ALSourcei.Buffer, bufferId);
            AL.Source(sourceId, ALSourceb.Looping, loop);
            AL.Source(sourceId, ALSourcef.Gain, gain);
            AL.Source(sourceId, ALSourcef.Pitch, pitch);
            AL.Source(sourceId, ALSourcef.ReferenceDistance, referenceDistance);
            // Non-positional by default — sounds play at the listener's position.
            // Set to false so gain is not distance-attenuated unless a position is set.
            AL.Source(sourceId, ALSourceb.SourceRelative, true);
            isValid = true;
        }

        /// <summary>Plays from the current playback position.</summary>
        public void Play()
        {
            if (!isValid) return;
            AL.SourcePlay(sourceId);
        }

        /// <summary>Stops playback and rewinds to the beginning.</summary>
        public void Stop()
        {
            if (!isValid) return;
            AL.SourceStop(sourceId);
            AL.SourceRewind(sourceId);
        }

        /// <summary>
        /// Restarts the sound from the beginning even if already playing.
        /// Useful for fire-and-forget one-shots.
        /// </summary>
        public void Restart()
        {
            if (!isValid) return;
            AL.SourceStop(sourceId);
            AL.SourceRewind(sourceId);
            AL.SourcePlay(sourceId);
        }

        /// <summary>Set a 3-D world-space emitter position for spatial audio.</summary>
        public void SetPosition(Vector3 position)
        {
            if (!isValid) return;
            // Switch to world-space positioning when an explicit position is set.
            AL.Source(sourceId, ALSourceb.SourceRelative, false);
            AL.Source(sourceId, ALSource3f.Position, position.X, position.Y, position.Z);
        }

        /// <summary>Free the OpenAL source. Buffer is owned by ResourceManager.</summary>
        public void Close()
        {
            if (!isValid) return;
            try { AL.SourceStop(sourceId); } catch { }
            try { AL.DeleteSource(sourceId); } catch { }
            isValid = false;
        }
    }
}
