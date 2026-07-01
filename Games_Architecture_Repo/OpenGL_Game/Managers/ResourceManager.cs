using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.OBJLoader;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace OpenGL_Game.Managers
{
    static class ResourceManager
    {
        static Dictionary<string, Geometry> geometryDictionary = new Dictionary<string, Geometry>();
        static Dictionary<string, int> textureDictionary = new Dictionary<string, int>();
        static Dictionary<string, int> audioDictionary = new Dictionary<string, int>();

        public static void RemoveAllAssets()
        {
            foreach(var geometry in geometryDictionary)
            {
                geometry.Value.RemoveGeometry();
            }
            geometryDictionary.Clear();
            foreach(var texture in textureDictionary)
            {
                GL.DeleteTexture(texture.Value);
            }
            textureDictionary.Clear();

            foreach(var audio in audioDictionary)
            {
                int buffer = audio.Value;
                try
                {
                    AL.DeleteBuffer(buffer);
                }
                catch { }
            }
            audioDictionary.Clear();
        }

        public static Geometry LoadGeometry(string filename)
        {
            Geometry geometry;
            geometryDictionary.TryGetValue(filename, out geometry);
            if (geometry == null)
            {
                geometry = new Geometry();
                geometry.LoadObject(filename);
                geometryDictionary.Add(filename, geometry);
            }

            return geometry;
        }

        public static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(nameof(filename));

            int texture;
            textureDictionary.TryGetValue(filename, out texture);
            if (texture != 0)
            {
                // Debug.WriteLine($"[LoadTexture] cache hit: {filename} -> {texture}");
                return texture;
            }

            // Debug.WriteLine($"[LoadTexture] loading: {filename}");

            // Ensure file exists before attempting decode
            if (!File.Exists(filename))
            {
                // Debug.WriteLine($"[LoadTexture] file not found: {filename}");
                throw new FileNotFoundException("Texture file not found", filename);
            }

            // Decode image first (before creating GL resources)
            SKBitmap bmp = null;
            try
            {
                bmp = SKBitmap.Decode(filename);
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"[LoadTexture] Decode threw: {ex}");
                throw;
            }

            // Debug.WriteLine($"[LoadTexture] decode => bmp is null? {bmp == null}");
            if (bmp == null)
            {
                throw new InvalidDataException($"Failed to decode texture: {filename}");
            }

            // Debug.WriteLine($"[LoadTexture] bmp: {bmp.Width}x{bmp.Height}, ColorType={bmp.ColorType}");
            try
            {
                var sample = bmp.GetPixel(Math.Max(0, bmp.Width/2), Math.Max(0, bmp.Height/2));
                // Debug.WriteLine($"[LoadTexture] sample mid pixel: {sample}");
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"[LoadTexture] sample pixel read failed: {ex}");
            }

            // Convert to a safe upload format (RGBA8888) — some drivers/contexts reject BGRA on TexImage2D
            SKBitmap uploadBmp = bmp;
            SKBitmap converted = null;
            if (bmp.ColorType != SKColorType.Rgba8888)
            {
                converted = bmp.Copy(SKColorType.Rgba8888);
                if (converted == null)
                {
                    bmp.Dispose();
                    // Debug.WriteLine($"[LoadTexture] conversion to RGBA8888 failed: {filename}");
                    throw new InvalidDataException($"Failed to convert texture to RGBA8888: {filename}");
                }
                uploadBmp = converted;
                // Debug.WriteLine($"[LoadTexture] converted bitmap to RGBA8888 for upload");
            }

            try
            {
                texture = GL.GenTexture();
                // Debug.WriteLine($"[LoadTexture] GenTexture -> {texture}");

                GL.BindTexture(TextureTarget.Texture2D, texture);

                // Basic parameters — tune as needed
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                // Ensure proper alignment for arbitrary widths
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                // Upload pixel data using RGBA format (UnsignedByte)
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                              PixelInternalFormat.Rgba8,
                              uploadBmp.Width, uploadBmp.Height, 0,
                              OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                              PixelType.UnsignedByte,
                              uploadBmp.GetPixels());

                var err = GL.GetError();
                // Debug.WriteLine($"[LoadTexture] GL.GetError after TexImage2D: {err}, texId={texture}");

                GL.BindTexture(TextureTarget.Texture2D, 0);

                // Debug: log texture id and any GL error after upload
                try
                {
                    Debug.WriteLine($"[LoadTexture] Loaded texture '{filename}' -> id={texture}, GL.GetError()={(int)GL.GetError()}");
                }
                catch { }

                // Only add to cache on success
                textureDictionary.Add(filename, texture);
                // Debug.WriteLine($"[LoadTexture] cached: {filename} -> {texture}");
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"[LoadTexture] GL upload failed: {ex}");
                // Clean up GL texture if we created one but failed to upload
                try
                {
                    if (texture != 0)
                        GL.DeleteTexture(texture);
                }
                catch { }

                throw;
            }
            finally
            {
                if (converted != null) converted.Dispose();
                bmp.Dispose();
            }

            return texture;
        }

        public static int LoadAudio(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(nameof(filename));

            int bufferId;
            audioDictionary.TryGetValue(filename, out bufferId);
            if (bufferId == 0)
            {
                if (!File.Exists(filename))
                    throw new FileNotFoundException("Audio file not found", filename);

                bufferId = AL.GenBuffer();

                // Load WAV file
                using (FileStream fs = File.OpenRead(filename))
                {
                    var wave = LoadWave(fs);
                    ALFormat format;
                    if (wave.channels == 1 && wave.bits == 8) format = ALFormat.Mono8;
                    else if (wave.channels == 1 && wave.bits == 16) format = ALFormat.Mono16;
                    else if (wave.channels == 2 && wave.bits == 8) format = ALFormat.Stereo8;
                    else if (wave.channels == 2 && wave.bits == 16) format = ALFormat.Stereo16;
                    else throw new NotSupportedException("Unsupported WAV format");

                    // Copy managed byte[] to native memory because AL.BufferData expects a native pointer/IntPtr
                    IntPtr nativePtr = IntPtr.Zero;
                    try
                    {
                        nativePtr = Marshal.AllocHGlobal(wave.data.Length);
                        Marshal.Copy(wave.data, 0, nativePtr, wave.data.Length);
                        // Use IntPtr overload so we don't pass managed byte[] directly
                        AL.BufferData(bufferId, format, nativePtr, wave.data.Length, wave.sampleRate);
                    }
                    finally
                    {
                        if (nativePtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(nativePtr);
                    }
                }

                audioDictionary.Add(filename, bufferId);
            }

            return bufferId;
        }

        // Helper to load WAV PCM (supports 8/16 bit mono/stereo)
        static (byte[] data, int channels, int bits, int sampleRate) LoadWave(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // RIFF header
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                int riffChunkSize = reader.ReadInt32();
                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                // Read chunks until 'fmt ' and 'data' found
                int channels = 0;
                int sampleRate = 0;
                int bitsPerSample = 0;
                byte[] data = null;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    string chunkId = new string(reader.ReadChars(4));
                    int chunkSize = reader.ReadInt32();

                    if (chunkId == "fmt ")
                    {
                        int audioFormat = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
                        int byteRate = reader.ReadInt32();
                        int blockAlign = reader.ReadInt16();
                        bitsPerSample = reader.ReadInt16();

                        // If fmt chunk has extra bytes, skip them
                        int fmtExtra = chunkSize - 16;
                        if (fmtExtra > 0)
                            reader.ReadBytes(fmtExtra);
                    }
                    else if (chunkId == "data")
                    {
                        data = reader.ReadBytes(chunkSize);
                    }
                    else
                    {
                        // skip unknown chunk
                        reader.ReadBytes(chunkSize);
                    }

                    if (data != null && channels != 0 && sampleRate != 0 && bitsPerSample != 0)
                        break;
                }

                if (data == null)
                    throw new InvalidDataException("Wave file has no data chunk.");

                return (data, channels, bitsPerSample, sampleRate);
            }
        }
    }
}
