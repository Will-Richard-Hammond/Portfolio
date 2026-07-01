using OpenTK.Graphics.OpenGL;
using OpenGL_Game.Components;
using OpenGL_Game.Systems;
using OpenGL_Game.Managers;
using OpenGL_Game.Objects;
using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Audio.OpenAL;
using OpenGL_Game.Utilities;
using OpenTK.Windowing.Common.Input;

namespace OpenGL_Game.Scenes
{
    class GameScene : Scene
    {
        public static float dt = 0;
        public static string LastGameResultMessage { get; private set; } = "Game Over";

        const int MazeGridSize = 20;
        const float MazeCellSize = 1.0f;
        static readonly Vector3 MazeOrigin = new Vector3(0f, -1.5f, 0f);

        EntityManager entityManager;
        SystemManager systemManager;
        public Camera camera;
        public static GameScene gameInstance;

        public Entity playerEntity;
        public Vector3 cameraStartPosition;
        public float cameraCollisionRadius = 0.5f;
        public Entity skyboxEntity;

        readonly Vector3 playerOffset = new Vector3(0f, -1f, 0f);

        bool[] keysPressed = new bool[512];
        bool[,] mazeWalkable = new bool[MazeGridSize, MazeGridSize];

        bool birdseyeEnabled = false;
        bool paused = false;
        Vector3 savedCameraPosition;
        Vector3 savedCameraDirection;
        Vector3 savedCameraUp;
        readonly Vector3 birdseyePosition = new Vector3(20f, 20f, 20f);
        readonly Vector3 birdseyeTarget = new Vector3(0f, 0f, 0f);
        readonly float normalFovDegrees = 103f;
        readonly float birdseyeFovDegrees = 103f;

        const float MouseSensitivity = 0.0025f;
        const float MoveSpeed = 3.0f;
        const float MouseJitterThreshold = 0.001f;
        const int MaxLives = 3;
        const float DroneSpeed = 1.4f;
        const float DroneTouchDistance = 0.75f;
        const float DroneDisableDuration = 5f;
        const float WeaponRange = 8f;
        const float WeaponHitRadius = 0.65f;
        const int BaseDroneHealth = 3;

        bool fireRequested = false;
        float accumulatedMouseDeltaX = 0f;
        int playerLives = MaxLives;
        int weaponDamage = 1;
        float dronesDisabledTimer = 0f;
        bool dronesMovementEnabled = true;
        bool playerWallCollisionEnabled = true;

        readonly List<DroneState> drones = new();
        readonly List<PowerUpState> powerUps = new();

        public GameScene(SceneManager sceneManager) : base(sceneManager)
        {
            gameInstance = this;
            entityManager = new EntityManager();
            systemManager = new SystemManager();

            sceneManager.Title = "Game";
            sceneManager.renderer = Render;
            sceneManager.updater = Update;
            sceneManager.keyboardDownDelegate += Keyboard_KeyDown;
            sceneManager.keyboardUpDelegate += Keyboard_KeyUp;
            sceneManager.mouseDelegate += Mouse_ButtonDown;
            sceneManager.mouseUpDelegate += Mouse_ButtonUp;
            sceneManager.mouseMoveDelegate += Mouse_Move;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(0.08f, 0.08f, 0.12f, 1.0f);

            camera = new Camera(new Vector3(0.0f, 0.5f, 8.0f),
                                new Vector3(0.0f, 0.5f, 7.0f),
                                (float)(sceneManager.Size.X) / (float)(sceneManager.Size.Y),
                                0.1f, 200f);

            camera.projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(normalFovDegrees),
                (float)sceneManager.Size.X / sceneManager.Size.Y,
                0.1f,
                200f);

            camera.cameraUp = Vector3.UnitY;
            camera.cameraDirection.Y = 0f;
            if (camera.cameraDirection.LengthSquared > 0f)
                camera.cameraDirection = Vector3.Normalize(camera.cameraDirection);

            cameraStartPosition = camera.cameraPosition;
            sceneManager.CursorState = CursorState.Grabbed;

            BuildMazeWalkableMap();
            CreateEntities();
            CreateSystems();
        }

        private void BuildMazeWalkableMap()
        {
            for (int z = 0; z < MazeGridSize; z++)
                for (int x = 0; x < MazeGridSize; x++)
                    mazeWalkable[x, z] = false;

            CarveWalkableRect(1, 1, 4, 4);
            CarveWalkableRect(15, 1, 18, 4);
            CarveWalkableRect(15, 15, 18, 18);
            CarveWalkableRect(1, 15, 4, 18);
            CarveWalkableRect(8, 8, 11, 11);
            CarveWalkableRect(5, 2, 14, 3);
            CarveWalkableRect(5, 16, 14, 17);
            CarveWalkableRect(2, 5, 3, 14);
            CarveWalkableRect(16, 5, 17, 14);
            CarveWalkableRect(9, 4, 10, 7);
            CarveWalkableRect(9, 12, 10, 15);
            CarveWalkableRect(4, 9, 7, 10);
            CarveWalkableRect(12, 9, 15, 10);
            CarveWalkableRect(5, 4, 7, 7);
            CarveWalkableRect(12, 4, 14, 7);
            CarveWalkableRect(5, 12, 7, 15);
            CarveWalkableRect(12, 12, 14, 15);
        }

        private void CarveWalkableRect(int minX, int minZ, int maxX, int maxZ)
        {
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                    if (x >= 0 && x < MazeGridSize && z >= 0 && z < MazeGridSize)
                        mazeWalkable[x, z] = true;
        }

        private void CreateEntities()
        {
            Entity newEntity;

            skyboxEntity = new Entity("Skybox");
            skyboxEntity.AddComponent(new ComponentPosition(camera.cameraPosition));
            skyboxEntity.AddComponent(new ComponentGeometry("Geometry/Skybox/skybox.obj"));
            skyboxEntity.AddComponent(new ComponentScale(100.0f, 100.0f, 100.0f));
            skyboxEntity.AddComponent(new ComponentShaderSkybox(null));
            entityManager.AddEntity(skyboxEntity);

            FixedMazeBuilder.BuildFixedMaze(entityManager, MazeOrigin, "Geometry/Cube/cube.obj");

            playerEntity = new Entity("Player");
            playerEntity.AddComponent(new ComponentPosition(camera.cameraPosition + playerOffset));
            playerEntity.AddComponent(new ComponentCollisionAABB(new Vector3(-0.30f, -0.5f, -0.30f), new Vector3(0.30f, 0.5f, 0.30f)));
            entityManager.AddEntity(playerEntity);

            newEntity = new Entity("Floor");
            newEntity.AddComponent(new ComponentPosition(0.0f, -1.5f, 0.0f));
            newEntity.AddComponent(new ComponentGeometry("Geometry/Cube/cube.obj"));
            float floorScaleX = 20.0f;
            float floorScaleY = 0.5f;
            float floorScaleZ = 20.0f;
            newEntity.AddComponent(new ComponentScale(floorScaleX, floorScaleY, floorScaleZ));
            newEntity.AddComponent(new ComponentCollisionAABB(
                new Vector3(-floorScaleX / 2f, -floorScaleY / 2f, -floorScaleZ / 2f),
                new Vector3(floorScaleX / 2f, floorScaleY / 2f, floorScaleZ / 2f)));
            entityManager.AddEntity(newEntity);

            CreateDrone("Drone_1", MazeCellToWorld(2, 2, 0.5f));
            CreateDrone("Drone_2", MazeCellToWorld(17, 17, 0.5f));

            CreatePowerUp("PowerUp_Health", PowerUpType.Health, MazeCellToWorld(2, 17, 0.25f), new Vector3(0.35f, 0.35f, 0.35f));
            CreatePowerUp("PowerUp_Weapon", PowerUpType.WeaponUpgrade, MazeCellToWorld(17, 2, 0.25f), new Vector3(0.35f, 0.35f, 0.35f));
            CreatePowerUp("PowerUp_EMP", PowerUpType.DisableDrones, MazeCellToWorld(9, 9, 0.25f), new Vector3(0.35f, 0.35f, 0.35f));
        }

        private void CreateDrone(string name, Vector3 startPosition)
        {
            var drone = new Entity(name);
            drone.AddComponent(new ComponentPosition(startPosition));
            drone.AddComponent(new ComponentGeometry("Geometry/Cube/cube.obj"));
            drone.AddComponent(new ComponentScale(0.5f, 0.5f, 0.5f));
            drone.AddComponent(new ComponentCollisionAABB(new Vector3(-0.25f, -0.25f, -0.25f), new Vector3(0.25f, 0.25f, 0.25f)));
            drone.AddComponent(new ComponentShaderDefault());
            entityManager.AddEntity(drone);
            drones.Add(new DroneState(drone, startPosition, BaseDroneHealth));
        }

        private void CreatePowerUp(string name, PowerUpType type, Vector3 position, Vector3 scale)
        {
            var powerUp = new Entity(name);
            powerUp.AddComponent(new ComponentPosition(position));
            powerUp.AddComponent(new ComponentGeometry("Geometry/Cube/cube.obj"));
            powerUp.AddComponent(new ComponentScale(scale));
            powerUp.AddComponent(new ComponentCollisionAABB(new Vector3(-scale.X / 2f, -scale.Y / 2f, -scale.Z / 2f), new Vector3(scale.X / 2f, scale.Y / 2f, scale.Z / 2f)));
            powerUp.AddComponent(new ComponentShaderDefault());
            entityManager.AddEntity(powerUp);
            powerUps.Add(new PowerUpState(powerUp, type, position));
        }

        private void CreateSystems()
        {
            systemManager.AddSystem(new SystemRender(() => (camera.view, camera.projection)));
            systemManager.AddSystem(new SystemPhysics());
            systemManager.AddSystem(new SystemAudio());
            systemManager.AddSystem(new SystemCollisionSphereSphere());
            systemManager.AddSystem(new SystemCollisionPointInAABB());
            systemManager.AddSystem(new SystemCollisionLineLine());
        }

        public override void Update(FrameEventArgs e)
        {
            dt = (float)e.Time;
            if (paused)
            {
                camera.UpdateView();
                return;
            }

            Vector3 previousCameraPosition = camera.cameraPosition;

            if (!birdseyeEnabled)
            {
                UpdateMouseLook();

                Vector3 move = Vector3.Zero;
                Vector3 flatForward = GetFlatForward();
                Vector3 flatRight = GetFlatRight();

                if (keysPressed[(char)Keys.W] || keysPressed[(char)Keys.Up]) move += flatForward * MoveSpeed * dt;
                if (keysPressed[(char)Keys.S] || keysPressed[(char)Keys.Down]) move -= flatForward * MoveSpeed * dt;
                if (keysPressed[(char)Keys.A] || keysPressed[(char)Keys.Left]) move -= flatRight * MoveSpeed * dt;
                if (keysPressed[(char)Keys.D] || keysPressed[(char)Keys.Right]) move += flatRight * MoveSpeed * dt;

                move.Y = 0f;
                camera.cameraPosition += move;

                if (playerWallCollisionEnabled)
                    ResolvePlayerMovement(previousCameraPosition, camera.cameraPosition);

                if (fireRequested)
                {
                    HandlePlayerFire();
                    fireRequested = false;
                }

                UpdateDroneDisableTimer();
                UpdateDrones();
                UpdatePowerUps();
                CheckGameState();
            }

            camera.cameraDirection.Y = 0f;
            if (camera.cameraDirection.LengthSquared > 0f)
                camera.cameraDirection = Vector3.Normalize(camera.cameraDirection);
            camera.cameraUp = Vector3.UnitY;
            camera.UpdateView();

            var skyPos = skyboxEntity?.Components.Find(c => c is ComponentPosition) as ComponentPosition;
            if (skyPos != null)
                skyPos.Position = camera.cameraPosition;

            var playerPos = playerEntity?.Components.Find(c => c is ComponentPosition) as ComponentPosition;
            if (playerPos != null)
                playerPos.Position = camera.cameraPosition + playerOffset;

            AL.Listener(ALListener3f.Position, camera.cameraPosition.X, camera.cameraPosition.Y, camera.cameraPosition.Z);
            AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);
            float[] orientation = new float[] {
                camera.cameraDirection.X, camera.cameraDirection.Y, camera.cameraDirection.Z,
                camera.cameraUp.X, camera.cameraUp.Y, camera.cameraUp.Z
            };
            AL.Listener(ALListenerfv.Orientation, orientation);
        }

        private void UpdateDroneDisableTimer()
        {
            if (dronesDisabledTimer <= 0f) return;
            dronesDisabledTimer -= dt;
            if (dronesDisabledTimer <= 0f)
            {
                dronesDisabledTimer = 0f;
                GUI.AddMessage("Drones reactivated", 1.5f);
            }
        }

        private void UpdateDrones()
        {
            Vector3 playerPosition = camera.cameraPosition + playerOffset;
            var playerCell = TryGetMazeCell(playerPosition);

            foreach (var drone in drones)
            {
                if (drone.IsDisabled) continue;
                var dronePos = GetEntityPosition(drone.Entity);
                if (dronePos == null) continue;

                if (dronesDisabledTimer <= 0f && dronesMovementEnabled && playerCell.HasValue)
                    UpdateDronePathMovement(drone, dronePos, playerCell.Value);

                Vector3 horizontalDelta = dronePos.Position - playerPosition;
                horizontalDelta.Y = 0f;
                if (horizontalDelta.Length <= DroneTouchDistance)
                {
                    OnPlayerCaught();
                    return;
                }
            }
        }

        private void UpdateDronePathMovement(DroneState drone, ComponentPosition dronePos, (int x, int z) playerCell)
        {
            var droneCell = TryGetMazeCell(dronePos.Position);
            if (!droneCell.HasValue) return;

            if (!drone.CurrentTargetCell.HasValue || drone.CurrentTargetCell.Value == droneCell.Value)
            {
                var nextCell = FindNextCellTowardsTarget(droneCell.Value, playerCell);
                drone.CurrentTargetCell = nextCell ?? droneCell.Value;
            }

            Vector3 targetWorld = MazeCellToWorld(drone.CurrentTargetCell.Value.x, drone.CurrentTargetCell.Value.z, dronePos.Position.Y);
            Vector3 toTarget = targetWorld - dronePos.Position;
            toTarget.Y = 0f;

            if (toTarget.LengthSquared <= 0.01f)
            {
                dronePos.Position = new Vector3(targetWorld.X, dronePos.Position.Y, targetWorld.Z);
                drone.CurrentTargetCell = null;
                return;
            }

            Vector3 step = Vector3.Normalize(toTarget) * DroneSpeed * dt;
            if (step.LengthSquared > toTarget.LengthSquared)
                step = toTarget;

            dronePos.Position += new Vector3(step.X, 0f, step.Z);
        }

        private (int x, int z)? FindNextCellTowardsTarget((int x, int z) start, (int x, int z) target)
        {
            if (start == target) return start;

            var queue = new Queue<(int x, int z)>();
            var visited = new bool[MazeGridSize, MazeGridSize];
            var previous = new (int x, int z)?[MazeGridSize, MazeGridSize];
            queue.Enqueue(start);
            visited[start.x, start.z] = true;

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target) break;

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.x + dx[i];
                    int nz = current.z + dz[i];
                    if (nx < 0 || nx >= MazeGridSize || nz < 0 || nz >= MazeGridSize) continue;
                    if (!mazeWalkable[nx, nz] || visited[nx, nz]) continue;

                    visited[nx, nz] = true;
                    previous[nx, nz] = current;
                    queue.Enqueue((nx, nz));
                }
            }

            if (!visited[target.x, target.z]) return null;

            var step = target;
            while (previous[step.x, step.z].HasValue && previous[step.x, step.z].Value != start)
                step = previous[step.x, step.z].Value;

            return step;
        }

        private (int x, int z)? TryGetMazeCell(Vector3 worldPosition)
        {
            float half = (MazeGridSize / 2f) - 0.5f;
            int x = (int)MathF.Round(((worldPosition.X - MazeOrigin.X) / MazeCellSize) + half);
            int z = (int)MathF.Round(((worldPosition.Z - MazeOrigin.Z) / MazeCellSize) + half);
            if (x < 0 || x >= MazeGridSize || z < 0 || z >= MazeGridSize) return null;
            if (!mazeWalkable[x, z]) return null;
            return (x, z);
        }

        private Vector3 MazeCellToWorld(int x, int z, float y)
        {
            float half = (MazeGridSize / 2f) - 0.5f;
            float worldX = MazeOrigin.X + (x - half) * MazeCellSize;
            float worldZ = MazeOrigin.Z + (z - half) * MazeCellSize;
            return new Vector3(worldX, y, worldZ);
        }

        private void UpdatePowerUps()
        {
            Vector3 playerPosition = camera.cameraPosition + playerOffset;
            foreach (var powerUp in powerUps)
            {
                if (powerUp.Collected) continue;
                var powerPos = GetEntityPosition(powerUp.Entity);
                if (powerPos == null) continue;
                if ((powerPos.Position - playerPosition).Length <= 0.8f)
                {
                    powerUp.Collected = true;
                    powerPos.Position = new Vector3(1000f, -1000f, 1000f);
                    ApplyPowerUp(powerUp.Type);
                }
            }
        }

        private void ApplyPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Health:
                    if (playerLives < MaxLives) playerLives++;
                    GUI.AddMessage("Health collected", 1.5f);
                    break;
                case PowerUpType.WeaponUpgrade:
                    weaponDamage++;
                    GUI.AddMessage("Weapon upgraded", 1.5f);
                    break;
                case PowerUpType.DisableDrones:
                    dronesDisabledTimer = DroneDisableDuration;
                    GUI.AddMessage("EMP activated", 1.5f);
                    break;
            }
        }

        private void HandlePlayerFire()
        {
            GUI.AddMessage("Fire", 0.25f);
            Vector3 origin = camera.cameraPosition;
            Vector3 direction = GetFlatForward();
            DroneState bestHit = null;
            float bestDistance = float.MaxValue;

            foreach (var drone in drones)
            {
                if (drone.IsDisabled) continue;
                var dronePos = GetEntityPosition(drone.Entity);
                if (dronePos == null) continue;

                Vector3 toDrone = dronePos.Position - origin;
                float forwardDistance = Vector3.Dot(toDrone, direction);
                if (forwardDistance < 0f || forwardDistance > WeaponRange) continue;

                Vector3 closestPoint = origin + direction * forwardDistance;
                float distanceFromRay = (dronePos.Position - closestPoint).Length;
                if (distanceFromRay > WeaponHitRadius) continue;

                if (forwardDistance < bestDistance)
                {
                    bestDistance = forwardDistance;
                    bestHit = drone;
                }
            }

            if (bestHit == null) return;

            bestHit.Health -= weaponDamage;
            GUI.AddMessage($"{bestHit.Entity.Name} hit", 1.0f);
            if (bestHit.Health <= 0)
            {
                bestHit.IsDisabled = true;
                bestHit.CurrentTargetCell = null;
                var dronePos = GetEntityPosition(bestHit.Entity);
                if (dronePos != null)
                    dronePos.Position = new Vector3(1000f, -1000f, 1000f);
                GUI.AddMessage($"{bestHit.Entity.Name} disabled", 2.0f);
            }
        }

        private void OnPlayerCaught()
        {
            playerLives--;
            GUI.AddMessage($"Drone hit! Lives: {playerLives}", 2.0f);
            if (playerLives <= 0)
            {
                LastGameResultMessage = "Defeat - You lost all 3 lives";
                sceneManager.StartGameOver();
                return;
            }
            ResetPositionsAfterLifeLoss();
        }

        private void ResetPositionsAfterLifeLoss()
        {
            ResetPlayerPosition();
            accumulatedMouseDeltaX = 0f;
            dronesDisabledTimer = 0f;

            foreach (var drone in drones)
            {
                drone.Health = BaseDroneHealth;
                drone.CurrentTargetCell = null;
                drone.IsDisabled = false;

                var dronePos = GetEntityPosition(drone.Entity);
                if (dronePos != null)
                    dronePos.Position = drone.StartPosition;
            }
        }

        private void CheckGameState()
        {
            if (drones.All(d => d.IsDisabled))
            {
                LastGameResultMessage = "Victory - All drones disabled";
                sceneManager.StartGameOver();
            }
        }

        private ComponentPosition GetEntityPosition(Entity entity)
        {
            return entity.Components.Find(c => c is ComponentPosition) as ComponentPosition;
        }

        private void UpdateMouseLook()
        {
            float deltaX = accumulatedMouseDeltaX;
            accumulatedMouseDeltaX = 0f;
            if (Math.Abs(deltaX) < MouseJitterThreshold) return;
            camera.RotateY(deltaX * MouseSensitivity);
            camera.cameraDirection.Y = 0f;
            if (camera.cameraDirection.LengthSquared > 0f)
                camera.cameraDirection = Vector3.Normalize(camera.cameraDirection);
        }

        private void Mouse_Move(MouseMoveEventArgs e)
        {
            if (birdseyeEnabled) return;
            if (sceneManager.CursorState != CursorState.Grabbed) return;
            accumulatedMouseDeltaX += e.Delta.X;
        }

        private Vector3 GetFlatForward()
        {
            Vector3 forward = camera.cameraDirection;
            forward.Y = 0f;
            if (forward.LengthSquared <= 0f) return -Vector3.UnitZ;
            return Vector3.Normalize(forward);
        }

        private Vector3 GetFlatRight()
        {
            Vector3 forward = GetFlatForward();
            Vector3 right = Vector3.Cross(forward, Vector3.UnitY);
            if (right.LengthSquared <= 0f) return Vector3.UnitX;
            return Vector3.Normalize(right);
        }

        private void ResolvePlayerMovement(Vector3 previousCameraPosition, Vector3 desiredCameraPosition)
        {
            if (playerEntity == null) return;
            var playerPos = playerEntity.Components.Find(c => c is ComponentPosition) as ComponentPosition;
            var playerAabb = playerEntity.Components.Find(c => c is ComponentCollisionAABB) as ComponentCollisionAABB;
            if (playerPos == null || playerAabb == null) return;

            Vector3 previousPlayerPosition = previousCameraPosition + playerOffset;
            Vector3 desiredPlayerPosition = desiredCameraPosition + playerOffset;
            Vector3 resolvedPlayerPosition = previousPlayerPosition;

            resolvedPlayerPosition.X = desiredPlayerPosition.X;
            resolvedPlayerPosition = ResolvePlayerAxis(resolvedPlayerPosition, previousPlayerPosition, playerAabb, true);
            Vector3 afterX = resolvedPlayerPosition;
            resolvedPlayerPosition.Z = desiredPlayerPosition.Z;
            resolvedPlayerPosition = ResolvePlayerAxis(resolvedPlayerPosition, afterX, playerAabb, false);
            resolvedPlayerPosition.Y = previousPlayerPosition.Y;

            playerPos.Position = resolvedPlayerPosition;
            camera.cameraPosition = resolvedPlayerPosition - playerOffset;
            camera.cameraPosition.Y = previousCameraPosition.Y;
        }

        private Vector3 ResolvePlayerAxis(Vector3 desiredPlayerPosition, Vector3 previousPlayerPosition, ComponentCollisionAABB playerAabb, bool resolveX)
        {
            const float skin = 0.001f;
            foreach (var entity in entityManager.Entities())
            {
                if (entity == playerEntity || entity.Name == "Floor" || entity.Name == "Skybox") continue;
                if (entity.Name.StartsWith("Drone_") || entity.Name.StartsWith("PowerUp_")) continue;

                var wallPos = entity.Components.Find(c => c is ComponentPosition) as ComponentPosition;
                var wallAabb = entity.Components.Find(c => c is ComponentCollisionAABB) as ComponentCollisionAABB;
                if (wallPos == null || wallAabb == null) continue;
                if (!IntersectsAabb(desiredPlayerPosition, playerAabb, wallPos.Position, wallAabb)) continue;

                Vector3 wallMin = wallPos.Position + wallAabb.LocalMin;
                Vector3 wallMax = wallPos.Position + wallAabb.LocalMax;
                if (resolveX)
                {
                    if (desiredPlayerPosition.X > previousPlayerPosition.X)
                        desiredPlayerPosition.X = wallMin.X - playerAabb.LocalMax.X - skin;
                    else if (desiredPlayerPosition.X < previousPlayerPosition.X)
                        desiredPlayerPosition.X = wallMax.X - playerAabb.LocalMin.X + skin;
                }
                else
                {
                    if (desiredPlayerPosition.Z > previousPlayerPosition.Z)
                        desiredPlayerPosition.Z = wallMin.Z - playerAabb.LocalMax.Z - skin;
                    else if (desiredPlayerPosition.Z < previousPlayerPosition.Z)
                        desiredPlayerPosition.Z = wallMax.Z - playerAabb.LocalMin.Z + skin;
                }
            }
            return desiredPlayerPosition;
        }

        private static bool IntersectsAabb(Vector3 playerPosition, ComponentCollisionAABB playerAabb, Vector3 wallPosition, ComponentCollisionAABB wallAabb)
        {
            Vector3 playerMin = playerPosition + playerAabb.LocalMin;
            Vector3 playerMax = playerPosition + playerAabb.LocalMax;
            Vector3 wallMin = wallPosition + wallAabb.LocalMin;
            Vector3 wallMax = wallPosition + wallAabb.LocalMax;
            return playerMax.X > wallMin.X && playerMin.X < wallMax.X &&
                   playerMax.Y > wallMin.Y && playerMin.Y < wallMax.Y &&
                   playerMax.Z > wallMin.Z && playerMin.Z < wallMax.Z;
        }

        public override void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Size.X, sceneManager.Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            systemManager.ActionSystems(entityManager);

            const float hudTop = 60f;
            const float hudStep = 35f;

            GUI.DrawText(birdseyeEnabled ? "Birdseye: ON (B to toggle)" : "Birdseye: OFF (B to toggle)", 30, hudTop, 20, 255, 255, 0);
            GUI.DrawText($"Lives: {new string('\u2665', playerLives)}", 30, hudTop + hudStep, 28, 255, 80, 80);
            GUI.DrawText($"Weapon Level: {weaponDamage}", 30, hudTop + hudStep * 2, 24, 255, 255, 255);
            GUI.DrawText($"Drones: {string.Join(" ", drones.Select(d => d.IsDisabled ? "[X]" : $"[{d.Health}]"))}", 30, hudTop + hudStep * 3, 24, 255, 255, 255);
            GUI.DrawText($"Drone Move: {(dronesMovementEnabled ? "ON" : "OFF")} (M)", 30, hudTop + hudStep * 4, 24, 255, 255, 255);
            GUI.DrawText($"Wall Collide: {(playerWallCollisionEnabled ? "ON" : "OFF")} (C)", 30, hudTop + hudStep * 5, 24, 255, 255, 255);
            GUI.DrawText($"PowerUps Left: {powerUps.Count(p => !p.Collected)}", 30, hudTop + hudStep * 6, 24, 255, 255, 255);
            if (dronesDisabledTimer > 0f)
                GUI.DrawText($"EMP Active: {dronesDisabledTimer:0.0}s", 30, hudTop + hudStep * 7, 24, 0, 255, 255);
            if (paused)
            {
                GUI.DrawText("PAUSED", sceneManager.Size.X * 0.5f - 70, sceneManager.Size.Y * 0.5f, 42, 255, 255, 0);
                GUI.DrawText("P = Resume   Esc = Main Menu", sceneManager.Size.X * 0.5f - 150, sceneManager.Size.Y * 0.5f + 40, 24, 255, 255, 255);
            }
            GUI.Render();
        }

        public override void Close()
        {
            sceneManager.keyboardDownDelegate -= Keyboard_KeyDown;
            sceneManager.keyboardUpDelegate -= Keyboard_KeyUp;
            sceneManager.mouseDelegate -= Mouse_ButtonDown;
            sceneManager.mouseUpDelegate -= Mouse_ButtonUp;
            sceneManager.mouseMoveDelegate -= Mouse_Move;
            sceneManager.CursorState = CursorState.Normal;
            entityManager.CloseAllComponents();
            ResourceManager.RemoveAllAssets();
        }

        public void Keyboard_KeyDown(KeyboardKeyEventArgs e)
        {
            keysPressed[(char)e.Key] = true;
            if (e.Key == Keys.B)
                ToggleBirdseyeView();
            else if (e.Key == Keys.M)
            {
                dronesMovementEnabled = !dronesMovementEnabled;
                GUI.AddMessage(dronesMovementEnabled ? "Drone movement enabled" : "Drone movement disabled", 1.5f);
            }
            else if (e.Key == Keys.C)
            {
                playerWallCollisionEnabled = !playerWallCollisionEnabled;
                GUI.AddMessage(playerWallCollisionEnabled ? "Wall collision enabled" : "Wall collision disabled", 1.5f);
            }
            else if (e.Key == Keys.P)
            {
                paused = !paused;
                sceneManager.CursorState = paused ? CursorState.Normal : CursorState.Grabbed;
                GUI.AddMessage(paused ? "Paused" : "Resumed", 1.0f);
            }
            else if (e.Key == Keys.Escape)
                sceneManager.StartMenu();
        }

        public void Keyboard_KeyUp(KeyboardKeyEventArgs e) => keysPressed[(char)e.Key] = false;

        private void Mouse_ButtonDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left && !birdseyeEnabled)
                fireRequested = true;
        }

        private void Mouse_ButtonUp(MouseButtonEventArgs e)
        {
        }

        private void ToggleBirdseyeView()
        {
            birdseyeEnabled = !birdseyeEnabled;
            if (birdseyeEnabled)
            {
                savedCameraPosition = camera.cameraPosition;
                savedCameraDirection = camera.cameraDirection;
                savedCameraUp = camera.cameraUp;
                sceneManager.CursorState = CursorState.Normal;
                camera.cameraPosition = birdseyePosition;
                camera.cameraDirection = Vector3.Normalize(birdseyeTarget - birdseyePosition);
                camera.cameraUp = Vector3.UnitZ;
                camera.projection = Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(birdseyeFovDegrees),
                    (float)sceneManager.Size.X / sceneManager.Size.Y,
                    0.1f,
                    200f);
            }
            else
            {
                camera.cameraPosition = savedCameraPosition;
                camera.cameraDirection = savedCameraDirection;
                camera.cameraUp = savedCameraUp;
                sceneManager.CursorState = CursorState.Grabbed;
                accumulatedMouseDeltaX = 0f;
                camera.projection = Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(normalFovDegrees),
                    (float)sceneManager.Size.X / sceneManager.Size.Y,
                    0.1f,
                    200f);
            }
            camera.UpdateView();
        }

        public void ResetPlayerPosition()
        {
            camera.cameraPosition = cameraStartPosition;
            camera.cameraDirection = new Vector3(0f, 0f, -1f);
            camera.cameraUp = Vector3.UnitY;
            camera.UpdateView();
            var playerPos = playerEntity.Components.Find(c => c is ComponentPosition) as ComponentPosition;
            if (playerPos != null)
                playerPos.Position = camera.cameraPosition + playerOffset;
        }

        class DroneState
        {
            public Entity Entity { get; }
            public Vector3 StartPosition { get; }
            public int Health { get; set; }
            public bool IsDisabled { get; set; }
            public (int x, int z)? CurrentTargetCell { get; set; }

            public DroneState(Entity entity, Vector3 startPosition, int health)
            {
                Entity = entity;
                StartPosition = startPosition;
                Health = health;
            }
        }

        enum PowerUpType
        {
            Health,
            WeaponUpgrade,
            DisableDrones
        }

        class PowerUpState
        {
            public Entity Entity { get; }
            public PowerUpType Type { get; }
            public Vector3 StartPosition { get; }
            public bool Collected { get; set; }

            public PowerUpState(Entity entity, PowerUpType type, Vector3 startPosition)
            {
                Entity = entity;
                Type = type;
                StartPosition = startPosition;
            }
        }
    }
}