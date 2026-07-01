using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Entities;

namespace OpenGL_Game.GameRefactor.Services
{
    /// <summary>
    /// Scene-facing audio service for the refactored game.
    /// Holds a named dictionary of AudioComponent instances so the scene can trigger
    /// sounds by name without knowing anything about OpenAL source IDs.
    ///
    /// Usage:
    ///   var audio = new AudioService();
    ///   audio.Register("fire", "Audio/buzz.wav");
    ///   audio.Play("fire");          // plays immediately
    ///   audio.Restart("fire");       // restarts from the beginning even if already playing
    ///   audio.Close();               // call from RefactorGameScene.Close()
    /// </summary>
    class AudioService
    {
        readonly Dictionary<string, AudioComponent> sounds = new(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------ //
        // Registration
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Load and register a named sound from a file path.
        /// Calling Register with the same name twice replaces the previous entry.
        /// </summary>
        public void Register(string name, string filePath, bool loop = false, float gain = 1.0f, float pitch = 1.0f, float referenceDistance = 1.0f)
        {
            if (sounds.TryGetValue(name, out var existing))
            {
                existing.Close();
                sounds.Remove(name);
            }

            sounds[name] = new AudioComponent(filePath, loop, gain, pitch, referenceDistance);
        }

        // ------------------------------------------------------------------ //
        // Playback
        // ------------------------------------------------------------------ //

        /// <summary>Play from the current position (no-op if already playing).</summary>
        public void Play(string name)
        {
            if (sounds.TryGetValue(name, out var audio))
                audio.Play();
        }

        /// <summary>Stop and rewind to start, then play. Use for fire-and-forget one-shots.</summary>
        public void Restart(string name)
        {
            if (sounds.TryGetValue(name, out var audio))
                audio.Restart();
        }

        /// <summary>Stop and rewind without playing.</summary>
        public void Stop(string name)
        {
            if (sounds.TryGetValue(name, out var audio))
                audio.Stop();
        }

        /// <summary>Set a world-space 3-D emitter position for a named sound.</summary>
        public void SetPosition(string name, Vector3 position)
        {
            if (sounds.TryGetValue(name, out var audio))
                audio.SetPosition(position);
        }

        // ------------------------------------------------------------------ //
        // Lifecycle
        // ------------------------------------------------------------------ //

        /// <summary>Release all OpenAL sources. Call from RefactorGameScene.Close().</summary>
        public void Close()
        {
            foreach (var kv in sounds)
                kv.Value.Close();
            sounds.Clear();
        }
    }
}
