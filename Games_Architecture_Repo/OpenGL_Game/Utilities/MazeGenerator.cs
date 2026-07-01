using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenGL_Game.Components;
using OpenGL_Game.Managers;
using OpenGL_Game.Objects;

namespace OpenGL_Game.Utilities
{
    // [REFACTOR] Entire class body commented out — orphaned, superseded by EngineMazeBuilder.
    /*
    static class MazeGenerator
    {
        ...
    }
    */
    static class MazeGenerator
    {
        // [REFACTOR] GenerateMaze is orphaned — not called in legacy or refactored path.
        // Kept here as a stub so any future callers can re-enable it without a merge conflict.
        public static void GenerateMaze(
            EntityManager entityManager,
            Vector3 origin,
            int cols,
            int rows,
            float cellSize = 1f,
            float wallThickness = 0.2f,
            float wallHeight = 1f,
            string wallGeometryPath = "Geometry/Cube/cube.obj",
            float totalWidth = 0f,
            float totalDepth = 0f)
        {
            // [REFACTOR] Body intentionally empty — superseded by EngineMazeBuilder.BuildMaze().
        }
    }
}
