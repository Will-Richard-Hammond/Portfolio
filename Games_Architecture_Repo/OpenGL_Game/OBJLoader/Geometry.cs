using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.Managers;
using OpenTK.Mathematics;

namespace OpenGL_Game.OBJLoader
{
    public class Group
    {
        public List<float> vertices = new List<float>();
        public List<float> textureCoords = new List<float>();
        public List<float> normals = new List<float>();
        public PrimitiveType primitiveType;
        public int primitiveScale = 0;
        public int numberOfFaces = 0;
        public int texture = 0;
        // handles to vao and vbos
        public int vao_Handle;
        public int vbo_verts;
        public int vbo_texs;
        public int vbo_normals;
        public Vector3 diffuse;
    }
    /// <summary>
    /// This is the object that we use to store our geometry that we will use to render in the game
    /// </summary>
    public class Geometry
    {
        List<Group> groups = new List<Group>();

        string path;

        public Geometry()
        {
        }

        // Try to resolve a material texture name to an actual file on disk (case-insensitive)
        string ResolveTextureFile(string materialTexture)
        {
            if (string.IsNullOrEmpty(materialTexture))
                return null;

            // If the material gives an absolute or relative path, prefer it
            string direct = Path.Combine(path, materialTexture.Replace('/', '\\'));
            if (File.Exists(direct)) return direct;

            // Try just the filename in the model directory
            string filename = Path.GetFileName(materialTexture);
            if (!string.IsNullOrEmpty(filename))
            {
                string candidate = Path.Combine(path, filename);
                if (File.Exists(candidate)) return candidate;
            }

            // Try case-insensitive search in the directory
            try
            {
                var files = Directory.GetFiles(path);
                string baseName = Path.GetFileNameWithoutExtension(materialTexture);
                foreach (var f in files)
                {
                    if (string.Equals(Path.GetFileName(f), materialTexture, StringComparison.OrdinalIgnoreCase))
                        return f;
                    if (!string.IsNullOrEmpty(baseName) &&
                        Path.GetFileNameWithoutExtension(f).IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                        return f;
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"[ResolveTextureFile] directory search failed: {ex.Message}");
            }

            return null;
        }

        public void LoadObject(string filename)
        {
            try
            {
                // This OBJ parser library is developed by chrisjansson and available at https://github.com/chrisjansson/ObjLoader
                ObjLoader.Loader.Loaders.LoadResult obj = LoadOBJObject(filename);

                // This code assumes that all faces in all groups are defined as triangles or quads
                foreach (var group in obj.Groups)
                {
                    Group newGroup = new Group();
                    string resolvedTexturePath = null;

                    if (group.Material?.DiffuseTextureMap != null)
                    {
                        // material texture name from MTL
                        resolvedTexturePath = ResolveTextureFile(group.Material.DiffuseTextureMap);

                        if (resolvedTexturePath != null)
                        {
                            try
                            {
                                newGroup.texture = ResourceManager.LoadTexture(resolvedTexturePath);
                            }
                            catch (Exception ex)
                            {
                                // Debug.WriteLine($"[LoadObject] failed to load resolved texture '{resolvedTexturePath}': {ex.Message}");
                                newGroup.texture = 0;
                            }
                        }
                        else
                        {
                            // Debug.WriteLine($"[LoadObject] texture referenced in material not found: '{group.Material.DiffuseTextureMap}' (model path: '{path}')");
                            newGroup.texture = 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            newGroup.texture = ResourceManager.LoadTexture("Geometry\\Default\\default.png");
                        }
                        catch (Exception ex)
                        {
                            // Debug.WriteLine($"[LoadObject] failed to load default texture: {ex.Message}");
                            newGroup.texture = 0;
                        }
                    }

                    // Debug.WriteLine($"[LoadObject] group '{group?.Name}' material='{group?.Material?.DiffuseTextureMap}' resolvedTex={newGroup.texture}");

                    bool error = false;
                    string errorMessage = "";
                    bool primitiveSet = false;
                    foreach (var face in group.Faces)
                    {
                        ++newGroup.numberOfFaces;

                        if (face.Count == 3)
                        {
                            if(primitiveSet && newGroup.primitiveScale != 3)
                            {
                                error = true;
                                errorMessage = "The " + filename + " file has both triangular and quad faces and so will not be rendered correctly";
                            }
                            newGroup.primitiveType = PrimitiveType.Triangles;
                            newGroup.primitiveScale = 3;
                            primitiveSet = true;
                        }
                        else
                        {
                            error = true;
                            errorMessage = "The " + filename + " file does not have triangular faces and so will not be rendered correctly";
                        }

                        for (int i = 0; i < face.Count; ++i)
                        {
                            // obj indexing starts at 1, so we need to subtract 1
                            int v = face[i].VertexIndex - 1;
                            newGroup.vertices.Add(obj.Vertices[v].X);
                            newGroup.vertices.Add(obj.Vertices[v].Y);
                            newGroup.vertices.Add(obj.Vertices[v].Z);

                            if (obj.Textures.Count > 0)
                            {
                                // obj indexing starts at 1, so we need to subtract 1
                                int t = face[i].TextureIndex - 1;
                                newGroup.textureCoords.Add(obj.Textures[t].X);
                                // OpenGL tex coords start at top-left
                                newGroup.textureCoords.Add(1.0f-obj.Textures[t].Y);
                            }

                            if (obj.Normals.Count > 0)
                            {
                                // obj indexing starts at 1, so we need to subtract 1
                                int n = face[i].NormalIndex - 1;
                                newGroup.normals.Add(obj.Normals[n].X);
                                newGroup.normals.Add(obj.Normals[n].Y);
                                newGroup.normals.Add(obj.Normals[n].Z);
                            }
                        }
                    }

                    // Debug.WriteLine($"[LoadObject] group '{group?.Name}' verts={newGroup.vertices.Count/3} uvs={newGroup.textureCoords.Count/2} normals={newGroup.normals.Count/3}");

                    if (error)
                    {
                        MessageBox.Show(errorMessage, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Create the single VAO that will hold all information for this group
                    GL.GenVertexArrays(1, out newGroup.vao_Handle);
                    GL.BindVertexArray(newGroup.vao_Handle);

                    // Create the buffer for the vertices
                    GL.GenBuffers(1, out newGroup.vbo_verts);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newGroup.vbo_verts);
                    GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(newGroup.vertices.Count * 4), newGroup.vertices.ToArray<float>(), BufferUsageHint.StaticDraw);
                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * 4, 0);

                    // Tex Coords
                    if (obj.Textures.Count > 0)
                    {
                        // Create the buffer for the texture coords
                        GL.GenBuffers(1, out newGroup.vbo_texs);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newGroup.vbo_texs);
                        GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(newGroup.textureCoords.Count * 4), newGroup.textureCoords.ToArray<float>(), BufferUsageHint.StaticDraw);
                        GL.EnableVertexAttribArray(1);
                        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * 4, 0);
                    }

                    // Normals
                    if (obj.Normals.Count > 0)
                    {
                        // Create the buffer for the normals
                        GL.GenBuffers(1, out newGroup.vbo_normals);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newGroup.vbo_normals);
                        GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(newGroup.normals.Count * 4), newGroup.normals.ToArray<float>(), BufferUsageHint.StaticDraw);
                        GL.EnableVertexAttribArray(2);
                        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * 4, 0);
                    }

                    // Diffuse colour
                    newGroup.diffuse = new Vector3(group.Material.DiffuseColor.X, group.Material.DiffuseColor.Y, group.Material.DiffuseColor.Z); ;

                    groups.Add(newGroup);
                    GL.BindVertexArray(0);
                }

            }
            catch (Exception e)
            {
                // Debug.WriteLine(e.ToString());
            }
        }

        public ObjLoader.Loader.Loaders.LoadResult LoadOBJObject(string filename)
        {
            var objLoaderFactory = new ObjLoader.Loader.Loaders.ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            var fileStream = new FileStream(filename, FileMode.Open);
            path = fileStream.Name.Substring(0, fileStream.Name.LastIndexOf('\\') + 1);
            objLoader.SetPath(path);
            var obj = objLoader.Load(fileStream);
            fileStream.Close();
            return obj;
        }

        // Render this object
        public void Render(int uniform_diffuse)
        {
            foreach (var group in groups)
            {
                if (group.texture > 0)
                {
                    // Debug.WriteLine($"[Render] group vao={group.vao_Handle} tex={group.texture} faces={group.numberOfFaces}");
                    GL.Uniform3(uniform_diffuse, group.diffuse);
                    GL.BindVertexArray(group.vao_Handle);
                    GL.BindTexture(TextureTarget.Texture2D, group.texture);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, group.numberOfFaces * 3);
                }
            }
            GL.BindVertexArray(0);
        }

        // Overload: allow caller to supply an override texture. Render groups regardless of their own group.texture
        // as long as there is an effective texture to bind (either override or group's texture).
        public void Render(int uniform_diffuse, int texture)
        {
            foreach (var group in groups)
            {
                int effectiveTexture = (texture != 0) ? texture : group.texture;
                // Debug.WriteLine($"[Render] group vao={group.vao_Handle} effectiveTex={effectiveTexture} faces={group.numberOfFaces}");
                if (effectiveTexture > 0)
                {
                    GL.Uniform3(uniform_diffuse, group.diffuse);
                    GL.BindVertexArray(group.vao_Handle);
                    GL.BindTexture(TextureTarget.Texture2D, effectiveTexture);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, group.numberOfFaces * 3);
                }
            }
            GL.BindVertexArray(0);
        }

        public void RemoveGeometry()
        {
            foreach (var group in groups)
            {
                GL.DeleteBuffer(group.vbo_normals);
                GL.DeleteBuffer(group.vbo_texs);
                GL.DeleteBuffer(group.vbo_verts);
                GL.DeleteVertexArray(group.vao_Handle);
            }
        }
    }
}
