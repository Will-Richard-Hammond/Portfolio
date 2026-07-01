// [REFACTOR] Superseded by OpenGL_Game.GameRefactor.Utilities.EngineMazeBuilder.
// EngineMazeBuilder reads the same walkable map and creates EngineEntity wall blocks
// through RefactorEntityFactory, with proper AabbCollisionComponent for EngineWorldCollisionService.
// This class is retained only so the legacy GameScene path still compiles.

using System;
using OpenTK.Mathematics;
using OpenGL_Game.Components;
using OpenGL_Game.Managers;
using OpenGL_Game.Objects;

namespace OpenGL_Game.Utilities
{
    static class FixedMazeBuilder
    {
        const int GridSize = 20;
        const float CellSize = 1.0f;
        const float WallHeight = 1.0f;
        const float MouseSensitivity = 0.12f;

        public static void BuildFixedMaze(EntityManager entityManager, Vector3 origin, string wallGeometryPath)
        {
            // [REFACTOR] Body commented out — superseded by EngineMazeBuilder.BuildMaze().
            // The refactored path calls EngineMazeBuilder.BuildMaze(mazeWalkable, ...) in
            // RefactorGameScene.CreateEntities(), which creates EngineEntity wall blocks with
            // AabbCollisionComponent used by EngineWorldCollisionService for player collision.
            /*
            bool[,] walls = new bool[GridSize, GridSize];

            for (int z = 0; z < GridSize; z++)
                for (int x = 0; x < GridSize; x++)
                    walls[x, z] = true;

            CarveRect(walls, 1, 1, 4, 4);
            CarveRect(walls, 15, 1, 18, 4);
            CarveRect(walls, 15, 15, 18, 18);
            CarveRect(walls, 1, 15, 4, 18);
            CarveRect(walls, 8, 8, 11, 11);
            CarveRect(walls, 5, 2, 14, 3);
            CarveRect(walls, 5, 16, 14, 17);
            CarveRect(walls, 2, 5, 3, 14);
            CarveRect(walls, 16, 5, 17, 14);
            CarveRect(walls, 9, 4, 10, 7);
            CarveRect(walls, 9, 12, 10, 15);
            CarveRect(walls, 4, 9, 7, 10);
            CarveRect(walls, 12, 9, 15, 10);
            CarveRect(walls, 5, 4, 7, 7);
            CarveRect(walls, 12, 4, 14, 7);
            CarveRect(walls, 5, 12, 7, 15);
            CarveRect(walls, 12, 12, 14, 15);

            float half = (GridSize / 2f) - 0.5f;
            float wallY = origin.Y + 0.75f;

            for (int z = 0; z < GridSize; z++)
                for (int x = 0; x < GridSize; x++)
                {
                    if (!walls[x, z]) continue;
                    float worldX = origin.X + (x - half) * CellSize;
                    float worldZ = origin.Z + (z - half) * CellSize;
                    AddWallBlock(entityManager, $"MazeWall_{x}_{z}", new Vector3(worldX, wallY, worldZ), wallGeometryPath);
                }
            */
        }

        private static void CarveRect(bool[,] walls, int minX, int minZ, int maxX, int maxZ)
        {
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                    if (x >= 0 && x < GridSize && z >= 0 && z < GridSize)
                        walls[x, z] = false;
        }

        private static void AddWallBlock(EntityManager entityManager, string name, Vector3 position, string wallGeometryPath)
        {
            // [REFACTOR] Superseded by RefactorEntityFactory.CreateRenderable() with AabbCollisionComponent.
            /*
            var wall = new Entity(name);
            wall.AddComponent(new ComponentPosition(position));
            wall.AddComponent(new ComponentGeometry(wallGeometryPath));
            wall.AddComponent(new ComponentScale(CellSize, WallHeight, CellSize));
            wall.AddComponent(new ComponentCollisionAABB(
                new Vector3(-CellSize / 2f, -WallHeight / 2f, -CellSize / 2f),
                new Vector3(CellSize / 2f, WallHeight / 2f, CellSize / 2f)
            ));
            entityManager.AddEntity(wall);
            */
        }
    }
}