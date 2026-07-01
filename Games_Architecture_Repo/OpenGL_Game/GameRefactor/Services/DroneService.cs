using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OpenGL_Game.GameRefactor.Services
{
    class DroneService
    {
        readonly bool[,] mazeWalkable;
        readonly int mazeGridSize;
        readonly float mazeCellSize;
        readonly Vector3 mazeOrigin;

        public DroneService(bool[,] mazeWalkable, int mazeGridSize, float mazeCellSize, Vector3 mazeOrigin)
        {
            this.mazeWalkable = mazeWalkable;
            this.mazeGridSize = mazeGridSize;
            this.mazeCellSize = mazeCellSize;
            this.mazeOrigin = mazeOrigin;
        }

        public void UpdateDrones<TDroneState>(
            IEnumerable<TDroneState> drones,
            Vector3 playerPosition,
            float droneTouchDistance,
            float droneSpeed,
            bool dronesMovementEnabled,
            float dronesDisabledTimer,
            float dt,
            Func<TDroneState, bool> isDisabled,
            Func<TDroneState, string> getEntityName,
            Func<TDroneState, (int x, int z)?> getCurrentTargetCell,
            Action<TDroneState, (int x, int z)?> setCurrentTargetCell,
            Func<string, Vector3?> getPosition,
            Action<string, Vector3> setPosition,
            Action<TDroneState> onPlayerCaught)
        {
            var playerCell = TryGetMazeCell(playerPosition);

            foreach (var drone in drones)
            {
                if (isDisabled(drone))
                    continue;

                string entityName = getEntityName(drone);
                var dronePos = getPosition(entityName);
                if (!dronePos.HasValue)
                    continue;

                if (dronesDisabledTimer <= 0f && dronesMovementEnabled && playerCell.HasValue)
                    dronePos = UpdateDronePathMovement(drone, dronePos.Value, playerCell.Value, droneSpeed, dt, getCurrentTargetCell, setCurrentTargetCell, entityName, setPosition);

                Vector3 horizontalDelta = dronePos.Value - playerPosition;
                horizontalDelta.Y = 0f;
                if (horizontalDelta.Length <= droneTouchDistance)
                {
                    onPlayerCaught(drone);
                    break;
                }
            }
        }

        Vector3? UpdateDronePathMovement<TDroneState>(
            TDroneState drone,
            Vector3 dronePosition,
            (int x, int z) playerCell,
            float droneSpeed,
            float dt,
            Func<TDroneState, (int x, int z)?> getCurrentTargetCell,
            Action<TDroneState, (int x, int z)?> setCurrentTargetCell,
            string entityName,
            Action<string, Vector3> setPosition)
        {
            var droneCell = TryGetMazeCell(dronePosition);
            if (!droneCell.HasValue)
                return dronePosition;

            var currentTargetCell = getCurrentTargetCell(drone);
            if (!currentTargetCell.HasValue || currentTargetCell.Value == droneCell.Value)
            {
                var nextCell = FindNextCellTowardsTarget(droneCell.Value, playerCell);
                currentTargetCell = nextCell ?? droneCell.Value;
                setCurrentTargetCell(drone, currentTargetCell);
            }

            Vector3 targetWorld = MazeCellToWorld(currentTargetCell.Value.x, currentTargetCell.Value.z, dronePosition.Y);
            Vector3 toTarget = targetWorld - dronePosition;
            toTarget.Y = 0f;

            if (toTarget.LengthSquared <= 0.01f)
            {
                var snapped = new Vector3(targetWorld.X, dronePosition.Y, targetWorld.Z);
                setPosition(entityName, snapped);
                setCurrentTargetCell(drone, null);
                return snapped;
            }

            Vector3 step = Vector3.Normalize(toTarget) * droneSpeed * dt;
            if (step.LengthSquared > toTarget.LengthSquared)
                step = toTarget;

            var updated = dronePosition + new Vector3(step.X, 0f, step.Z);
            setPosition(entityName, updated);
            return updated;
        }

        (int x, int z)? FindNextCellTowardsTarget((int x, int z) start, (int x, int z) target)
        {
            if (start == target)
                return start;

            var queue = new Queue<(int x, int z)>();
            var visited = new bool[mazeGridSize, mazeGridSize];
            var previous = new (int x, int z)?[mazeGridSize, mazeGridSize];
            queue.Enqueue(start);
            visited[start.x, start.z] = true;

            int[] dx = [1, -1, 0, 0];
            int[] dz = [0, 0, 1, -1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                    break;

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.x + dx[i];
                    int nz = current.z + dz[i];
                    if (nx < 0 || nx >= mazeGridSize || nz < 0 || nz >= mazeGridSize)
                        continue;
                    if (!mazeWalkable[nx, nz] || visited[nx, nz])
                        continue;

                    visited[nx, nz] = true;
                    previous[nx, nz] = current;
                    queue.Enqueue((nx, nz));
                }
            }

            if (!visited[target.x, target.z])
                return null;

            var step = target;
            while (previous[step.x, step.z].HasValue && previous[step.x, step.z]!.Value != start)
                step = previous[step.x, step.z]!.Value;

            return step;
        }

        (int x, int z)? TryGetMazeCell(Vector3 worldPosition)
        {
            float half = (mazeGridSize / 2f) - 0.5f;
            int x = (int)MathF.Round(((worldPosition.X - mazeOrigin.X) / mazeCellSize) + half);
            int z = (int)MathF.Round(((worldPosition.Z - mazeOrigin.Z) / mazeCellSize) + half);
            if (x < 0 || x >= mazeGridSize || z < 0 || z >= mazeGridSize)
                return null;
            if (!mazeWalkable[x, z])
                return null;
            return (x, z);
        }

        Vector3 MazeCellToWorld(int x, int z, float y)
        {
            float half = (mazeGridSize / 2f) - 0.5f;
            float worldX = mazeOrigin.X + (x - half) * mazeCellSize;
            float worldZ = mazeOrigin.Z + (z - half) * mazeCellSize;
            return new Vector3(worldX, y, worldZ);
        }
    }
}
