using OpenTK.Mathematics;
using OpenGL_Game.GameRefactor.Factories;

namespace OpenGL_Game.GameRefactor.Utilities
{
    /// <summary>
    /// Builds maze wall entities into the engine entity manager using the walkable map
    /// produced by RefactorGameScene.BuildMazeWalkableMap(). Every cell that is NOT
    /// walkable (false) becomes a solid wall block with an AABB collider.
    /// </summary>
    static class EngineMazeBuilder
    {
        const float WallHeight = 1.0f;
        const float WallHalfHeight = WallHeight / 2f;

        public static void BuildMaze(
            bool[,] mazeWalkable,
            int gridSize,
            float cellSize,
            Vector3 origin,
            RefactorEntityFactory factory)
        {
            float half = (gridSize / 2f) - 0.5f;

            // floor surface is at origin.Y (center of floor slab).
            // Floor entity is at origin.Y with scale.Y = 0.5 so its top surface is at origin.Y + 0.25.
            // Wall blocks sit on top of the floor: their center = floor top + WallHalfHeight.
            float wallY = origin.Y + 0.25f + WallHalfHeight;

            Vector3 halfExtents = new(cellSize / 2f, WallHalfHeight, cellSize / 2f);

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    if (mazeWalkable[x, z])
                        continue; // open space — no wall

                    float worldX = origin.X + (x - half) * cellSize;
                    float worldZ = origin.Z + (z - half) * cellSize;

                    factory.CreateRenderable(
                        $"MazeWall_{x}_{z}",
                        new Vector3(worldX, wallY, worldZ),
                        "Geometry/Cube/cube.obj",
                        new Vector3(cellSize, WallHeight, cellSize),
                        "Default",
                        gameplayTag: "Wall",
                        aabb: (-halfExtents, halfExtents));
                }
            }
        }
    }
}
