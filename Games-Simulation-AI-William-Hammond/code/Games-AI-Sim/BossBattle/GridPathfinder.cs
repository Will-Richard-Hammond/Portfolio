using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace BossBattle
{
    /// <summary>
    /// Small grid-based A* pathfinder for the temple prototype.
    /// The FSM still decides the goal. This class simply finds safe waypoints
    /// around solid wall rectangles so thieves do not steer directly into walls.
    /// </summary>
    public sealed class GridPathfinder
    {
        private readonly int _worldWidth;
        private readonly int _worldHeight;
        private readonly int _cellSize;
        private readonly int _cols;
        private readonly int _rows;
        private readonly bool[,] _blocked;

        public GridPathfinder(int worldWidth, int worldHeight, int cellSize,
                              IEnumerable<Rectangle> walls, float agentRadius)
        {
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _cellSize = cellSize;
            _cols = Math.Max(1, (int)MathF.Ceiling(worldWidth / (float)cellSize));
            _rows = Math.Max(1, (int)MathF.Ceiling(worldHeight / (float)cellSize));
            _blocked = new bool[_cols, _rows];

            int inflate = (int)MathF.Ceiling(agentRadius + 2f);
            var inflatedWalls = new List<Rectangle>();

            foreach (Rectangle wall in walls)
            {
                inflatedWalls.Add(new Rectangle(
                    wall.X - inflate,
                    wall.Y - inflate,
                    wall.Width + inflate * 2,
                    wall.Height + inflate * 2));
            }

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    Vector2 centre = CellToWorld(new Point(x, y));
                    foreach (Rectangle wall in inflatedWalls)
                    {
                        if (wall.Contains((int)centre.X, (int)centre.Y))
                        {
                            _blocked[x, y] = true;
                            break;
                        }
                    }
                }
            }
        }

        public List<Vector2> FindPath(Vector2 startWorld, Vector2 goalWorld)
        {
            if (!TryFindNearestWalkableCell(WorldToCell(startWorld), out Point start))
                return new List<Vector2>();

            Point requestedGoal = WorldToCell(goalWorld);
            if (!TryFindNearestWalkableCell(requestedGoal, out Point goal))
                return new List<Vector2>();

            // If the requested goal is inside/too close to a wall, use the centre
            // of the nearest walkable cell instead. Otherwise the final waypoint
            // can pull thieves back into the wall after a valid path is found.
            Vector2 resolvedGoalWorld = goal == requestedGoal && IsWalkable(requestedGoal)
                ? goalWorld
                : CellToWorld(goal);

            if (start == goal)
                return new List<Vector2> { startWorld, resolvedGoalWorld };

            float[,] gScore = new float[_cols, _rows];
            float[,] fScore = new float[_cols, _rows];
            bool[,] closed = new bool[_cols, _rows];
            bool[,] inOpen = new bool[_cols, _rows];
            Point[,] parent = new Point[_cols, _rows];

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    gScore[x, y] = float.PositiveInfinity;
                    fScore[x, y] = float.PositiveInfinity;
                    parent[x, y] = new Point(-1, -1);
                }
            }

            var open = new List<Point> { start };
            inOpen[start.X, start.Y] = true;
            gScore[start.X, start.Y] = 0f;
            fScore[start.X, start.Y] = Heuristic(start, goal);

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = fScore[open[0].X, open[0].Y];

                for (int i = 1; i < open.Count; i++)
                {
                    float score = fScore[open[i].X, open[i].Y];
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                Point current = open[bestIndex];
                open.RemoveAt(bestIndex);
                inOpen[current.X, current.Y] = false;

                if (current == goal)
                    return ReconstructPath(parent, current, startWorld, resolvedGoalWorld);

                closed[current.X, current.Y] = true;

                foreach (Point neighbour in GetNeighbours(current))
                {
                    if (closed[neighbour.X, neighbour.Y]) continue;

                    float moveCost = current.X != neighbour.X && current.Y != neighbour.Y
                        ? 1.4142f
                        : 1f;

                    float tentative = gScore[current.X, current.Y] + moveCost;
                    if (tentative >= gScore[neighbour.X, neighbour.Y]) continue;

                    parent[neighbour.X, neighbour.Y] = current;
                    gScore[neighbour.X, neighbour.Y] = tentative;
                    fScore[neighbour.X, neighbour.Y] = tentative + Heuristic(neighbour, goal);

                    if (!inOpen[neighbour.X, neighbour.Y])
                    {
                        open.Add(neighbour);
                        inOpen[neighbour.X, neighbour.Y] = true;
                    }
                }
            }

            return new List<Vector2>();
        }

        private List<Vector2> ReconstructPath(Point[,] parent, Point current,
                                              Vector2 originalStart, Vector2 exactGoal)
        {
            var cells = new List<Point> { current };

            while (parent[current.X, current.Y].X >= 0)
            {
                current = parent[current.X, current.Y];
                cells.Add(current);
            }

            cells.Reverse();

            var path = new List<Vector2> { originalStart };
            for (int i = 1; i < cells.Count; i++)
                path.Add(CellToWorld(cells[i]));

            if (path.Count == 0 || Vector2.DistanceSquared(path[path.Count - 1], exactGoal) > 1f)
                path.Add(exactGoal);

            return path;
        }

        private IEnumerable<Point> GetNeighbours(Point cell)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Point next = new Point(cell.X + dx, cell.Y + dy);
                    if (!IsWalkable(next)) continue;

                    // Prevent diagonal corner-cutting through two blocked cells.
                    if (dx != 0 && dy != 0)
                    {
                        if (!IsWalkable(new Point(cell.X + dx, cell.Y)) ||
                            !IsWalkable(new Point(cell.X, cell.Y + dy)))
                            continue;
                    }

                    yield return next;
                }
            }
        }

        private bool TryFindNearestWalkableCell(Point start, out Point result)
        {
            start = ClampCell(start);

            if (IsWalkable(start))
            {
                result = start;
                return true;
            }

            var queue = new Queue<Point>();
            bool[,] visited = new bool[_cols, _rows];

            queue.Enqueue(start);
            visited[start.X, start.Y] = true;

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        Point next = new Point(current.X + dx, current.Y + dy);
                        if (!IsInBounds(next)) continue;
                        if (visited[next.X, next.Y]) continue;

                        if (IsWalkable(next))
                        {
                            result = next;
                            return true;
                        }

                        visited[next.X, next.Y] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            result = start;
            return false;
        }

        private Point WorldToCell(Vector2 world)
        {
            return ClampCell(new Point(
                (int)(MathHelper.Clamp(world.X, 0f, _worldWidth - 1f) / _cellSize),
                (int)(MathHelper.Clamp(world.Y, 0f, _worldHeight - 1f) / _cellSize)));
        }

        private Vector2 CellToWorld(Point cell)
        {
            return new Vector2(
                MathHelper.Clamp(cell.X * _cellSize + _cellSize * 0.5f, 0f, _worldWidth),
                MathHelper.Clamp(cell.Y * _cellSize + _cellSize * 0.5f, 0f, _worldHeight));
        }

        private Point ClampCell(Point cell)
        {
            return new Point(
                Math.Clamp(cell.X, 0, _cols - 1),
                Math.Clamp(cell.Y, 0, _rows - 1));
        }

        private bool IsWalkable(Point cell)
        {
            return IsInBounds(cell) && !_blocked[cell.X, cell.Y];
        }

        private bool IsInBounds(Point cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X < _cols && cell.Y < _rows;
        }

        private static float Heuristic(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
