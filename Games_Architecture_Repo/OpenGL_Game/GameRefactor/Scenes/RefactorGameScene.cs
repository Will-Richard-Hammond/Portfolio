using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGL_Game.Engine.Camera;
using OpenGL_Game.Engine.Config;
using OpenGL_Game.Engine.Input;
using OpenGL_Game.Engine.Loop;
using OpenGL_Game.Engine.Managers;
using OpenGL_Game.Engine.Systems;
using OpenGL_Game.GameRefactor.Config;
using OpenGL_Game.GameRefactor.Factories;
using OpenGL_Game.GameRefactor.Managers;
using OpenGL_Game.GameRefactor.Services;
using OpenGL_Game.Managers;
using OpenGL_Game.Scenes;
using OpenGL_Game.Utilities;
using OpenGL_Game.GameRefactor.Utilities;
// (fixed - was not caching geometry through path rather than allocating new locations in ram for each object)i believe there is a memory leak in the program unsure where it is coming from but i think it is in the game loop or the render loop
// (fixed) walls are not generated correctly and the player can walk through them, this is likely due to the maze generation logic not being implemented or not being called correctly
// the refactor is incomplete
namespace OpenGL_Game.GameRefactor.Scenes
{
    class RefactorGameScene : Scene
    {
        const int MazeGridSize = 20;
        const float MazeCellSize = 1.0f;
        const string PlayerEntityName = "PlayerEngine";
        static readonly Vector3 MazeOrigin = new(0f, -1.5f, 0f);
        static readonly Vector3 PlayerOffset = new(0f, -1f, 0f);
        static readonly Vector3 BirdseyePosition = new(20f, 20f, 20f);
        static readonly Vector3 BirdseyeTarget = Vector3.Zero;

        readonly EngineEntityManager engineEntityManager = new();
        readonly EngineSystemManager engineUpdateSystemManager = new();
        readonly EngineSystemManager engineRenderSystemManager = new();
        readonly GameInputManager inputManager = new();
        readonly SceneInputBridge inputBridge;
        readonly EngineGameLoop engineLoop;
        readonly DroneService droneService;
        readonly PowerUpService powerUpService = new();
        readonly HudRenderService hudRenderService = new();
        readonly EngineEntityQueryService entityQueryService;
        readonly EngineWorldCollisionService worldCollisionService;
        readonly AudioService audioService = new();
        readonly bool[,] mazeWalkable = new bool[MazeGridSize, MazeGridSize];
        readonly List<DroneState> drones = new();
        readonly List<PowerUpState> powerUps = new();
        readonly GameSettings settings;
        readonly RefactorEntityFactory entityFactory;

        EngineCamera camera;
        Vector3 cameraStartPosition;
        Vector3 savedCameraPosition;
        Vector3 savedCameraDirection;
        Vector3 savedCameraUp;
        bool birdseyeEnabled;
        bool paused;
        int playerLives;
        int weaponDamage = 1;
        float dronesDisabledTimer;
        bool dronesMovementEnabled = true;
        bool playerWallCollisionEnabled = true;

        public RefactorGameScene(SceneManager sceneManager) : base(sceneManager)
        {
            settings = LoadSettings();
            playerLives = settings.MaxLives;
            inputBridge = new SceneInputBridge(sceneManager, inputManager);
            engineLoop = new EngineGameLoop(sceneManager, new RefactorLoopAdapter(this, sceneManager));
            droneService = new DroneService(mazeWalkable, MazeGridSize, MazeCellSize, MazeOrigin);
            entityFactory = new RefactorEntityFactory(engineEntityManager);
            entityQueryService = new EngineEntityQueryService(engineEntityManager);
            worldCollisionService = new EngineWorldCollisionService(engineEntityManager);

            sceneManager.Title = "Game (Refactor)";
            sceneManager.renderer = Render;
            sceneManager.updater = Update;
            sceneManager.keyboardDownDelegate += Keyboard_KeyDown;
            sceneManager.mouseUpDelegate += Mouse_ButtonUp;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(0.08f, 0.08f, 0.12f, 1.0f);

            camera = new EngineCamera(new Vector3(0.0f, 0.5f, 8.0f), new Vector3(0.0f, 0.5f, 7.0f),
                (float)sceneManager.Size.X / sceneManager.Size.Y, 0.1f, 200f, settings.NormalFovDegrees);
            camera.Up = Vector3.UnitY;
            NormalizeCameraOrientation();
            camera.UpdateView();
            cameraStartPosition = camera.Position;
            sceneManager.CursorState = CursorState.Grabbed;

            inputBridge.Attach();
            BuildMazeWalkableMap();
            CreateEntities();
            CreateSystems();
            RegisterSounds();
        }

        void RegisterSounds()
        {
            audioService.Register("fire", "Audio/buzz.wav");
            audioService.Register("explosion", "Audio/explosion.wav");
            audioService.Register("pickup", "Audio/pickup.wav");
            audioService.Register("gameover", "Audio/gameover.wav");
            audioService.Register("ambient", "Audio/whitenoise.wav", loop: true, gain: 0.35f);
            audioService.Play("ambient");

            // Register a looping 3D beep for every power-up that starts uncollected.
            foreach (var powerUp in powerUps)
                audioService.Register(PowerUpBeepKey(powerUp.EntityName), "Audio/beeping.wav",
                    loop: true, gain: 0.8f, referenceDistance: 4.0f);

            // Position each beep at its power-up's world position and start playing.
            foreach (var powerUp in powerUps)
            {
                audioService.SetPosition(PowerUpBeepKey(powerUp.EntityName), powerUp.StartPosition);
                audioService.Play(PowerUpBeepKey(powerUp.EntityName));
            }
        }

        static string PowerUpBeepKey(string entityName) => $"beep_{entityName}";

        GameSettings LoadSettings()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "GameRefactor", "Config", "gamesettings.json");
            return File.Exists(path) ? SettingsLoader.Load<GameSettings>(path) ?? new GameSettings() : new GameSettings();
        }

        void BuildMazeWalkableMap()
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

        void CarveWalkableRect(int minX, int minZ, int maxX, int maxZ)
        {
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                    if (x >= 0 && x < MazeGridSize && z >= 0 && z < MazeGridSize)
                        mazeWalkable[x, z] = true;
        }

        void CreateEntities()
        {
            entityFactory.CreateRenderable("Skybox", camera.Position, "Geometry/Skybox/skybox.obj", new Vector3(100f, 100f, 100f), "Skybox", gameplayTag: "Skybox");

            var playerEngineEntity = new Engine.Entities.EngineEntity(PlayerEntityName);
            playerEngineEntity.AddComponent(new Engine.Components.TransformComponent(camera.Position + PlayerOffset));
            playerEngineEntity.AddComponent(new Engine.Components.VelocityComponent(Vector3.Zero));
            playerEngineEntity.AddComponent(new Engine.Components.AabbCollisionComponent(new Vector3(-0.30f, -0.5f, -0.30f), new Vector3(0.30f, 0.5f, 0.30f)));
            engineEntityManager.AddEntity(playerEngineEntity);

            float floorScaleX = 20.0f;
            float floorScaleY = 0.5f;
            float floorScaleZ = 20.0f;
            entityFactory.CreateRenderable(
                "Floor",
                new Vector3(0.0f, -1.5f, 0.0f),
                "Geometry/Cube/cube.obj",
                new Vector3(floorScaleX, floorScaleY, floorScaleZ),
                "Default",
                gameplayTag: "Floor",
                aabb: (new Vector3(-floorScaleX / 2f, -floorScaleY / 2f, -floorScaleZ / 2f), new Vector3(floorScaleX / 2f, floorScaleY / 2f, floorScaleZ / 2f)));

            // Build maze walls from the walkable map — every non-walkable cell becomes a solid wall block.
            EngineMazeBuilder.BuildMaze(mazeWalkable, MazeGridSize, MazeCellSize, MazeOrigin, entityFactory);

            CreateDrone("Drone_1", MazeCellToWorld(2, 2, 0.5f));
            CreateDrone("Drone_2", MazeCellToWorld(17, 17, 0.5f));
            CreatePowerUp("PowerUp_Health", PowerUpType.Health, MazeCellToWorld(2, 17, 0.25f), new Vector3(0.35f));
            CreatePowerUp("PowerUp_Weapon", PowerUpType.WeaponUpgrade, MazeCellToWorld(17, 2, 0.25f), new Vector3(0.35f));
            CreatePowerUp("PowerUp_EMP", PowerUpType.DisableDrones, MazeCellToWorld(9, 9, 0.25f), new Vector3(0.35f));
        }

        void CreateDrone(string name, Vector3 startPosition)
        {
            entityFactory.CreateRenderable(
                name,
                startPosition,
                "Geometry/Intergalactic_Spaceship/Intergalactic_Spaceship.obj",
                new Vector3(0.5f, 0.5f, 0.5f),
                "Default",
                gameplayTag: "Drone",
                aabb: (new Vector3(-0.25f, -0.25f, -0.25f), new Vector3(0.25f, 0.25f, 0.25f)));
            drones.Add(new DroneState(name, startPosition, settings.BaseDroneHealth));
        }

        void CreatePowerUp(string name, PowerUpType type, Vector3 position, Vector3 scale)
        {
            entityFactory.CreateRenderable(
                name,
                position,
                "Geometry/Cube/cube.obj",
                scale,
                "Default",
                gameplayTag: "PowerUp",
                aabb: (new Vector3(-scale.X / 2f, -scale.Y / 2f, -scale.Z / 2f), new Vector3(scale.X / 2f, scale.Y / 2f, scale.Z / 2f)));
            powerUps.Add(new PowerUpState(name, type, position));
        }

        void CreateSystems()
        {
            engineRenderSystemManager.AddSystem(new RenderSystem(() => (camera.View, camera.Projection)));
            engineUpdateSystemManager.AddSystem(new MovementSystem());
        }

        public override void Update(FrameEventArgs e)
        {
            float dt = (float)e.Time;
            if (paused)
            {
                camera.UpdateView();
                inputManager.ClearFrameRequests();
                return;
            }

            UpdateGameplay(dt);
            FinalizeFrame(dt);
        }

        void UpdateGameplay(float dt)
        {
            // HandleInputRequests must run before the birdseyeEnabled guard
            // so that birdseye can be toggled off and pause/fire still work
            HandleInputRequests();

            if (birdseyeEnabled)
                return;

            Vector3 previousCameraPosition = camera.Position;
            UpdateMouseLook();
            UpdateMovement(dt);
            if (playerWallCollisionEnabled)
                ResolvePlayerMovement(previousCameraPosition, camera.Position);
            UpdateDroneDisableTimer(dt);
            UpdateDrones(dt);
            UpdatePowerUps();
            CheckGameState();
        }

        void FinalizeFrame(float dt)
        {
            NormalizeCameraOrientation();
            camera.UpdateView();
            engineUpdateSystemManager.UpdateAll(engineEntityManager, dt);
            SyncSceneState();
            inputManager.ClearFrameRequests();
        }

        void NormalizeCameraOrientation()
        {
            camera.Direction = Vector3.Normalize(new Vector3(camera.Direction.X, 0f,
                camera.Direction.Z == 0f && camera.Direction.X == 0f ? -1f : camera.Direction.Z));
            camera.Up = Vector3.UnitY;
        }

        void UpdateMovement(float dt)
        {
            Vector3 move = Vector3.Zero;
            Vector3 flatForward = GetFlatForward();
            Vector3 flatRight = GetFlatRight();

            if (inputManager.IsKeyDown(Keys.W) || inputManager.IsKeyDown(Keys.Up)) move += flatForward * settings.MoveSpeed * dt;
            if (inputManager.IsKeyDown(Keys.S) || inputManager.IsKeyDown(Keys.Down)) move -= flatForward * settings.MoveSpeed * dt;
            if (inputManager.IsKeyDown(Keys.A) || inputManager.IsKeyDown(Keys.Left)) move -= flatRight * settings.MoveSpeed * dt;
            if (inputManager.IsKeyDown(Keys.D) || inputManager.IsKeyDown(Keys.Right)) move += flatRight * settings.MoveSpeed * dt;

            camera.Position += new Vector3(move.X, 0f, move.Z);
            SyncPlayerVelocity(move, dt);
        }

        void SyncPlayerVelocity(Vector3 move, float dt)
        {
            var velocity = entityQueryService.Find(PlayerEntityName)?.GetComponent<Engine.Components.VelocityComponent>();
            if (velocity != null)
                velocity.Velocity = dt > 0f ? move / dt : Vector3.Zero;
        }

        void HandleInputRequests()
        {
            if (inputManager.ToggleBirdseyeRequested)
                ToggleBirdseyeView();
            if (inputManager.TogglePauseRequested)
            {
                paused = !paused;
                sceneManager.CursorState = paused ? CursorState.Normal : CursorState.Grabbed;
                GUI.AddMessage(paused ? "Paused" : "Resumed", 1.0f);
            }
            if (inputManager.ToggleDroneMovementRequested)
            {
                dronesMovementEnabled = !dronesMovementEnabled;
                GUI.AddMessage(dronesMovementEnabled ? "Drone movement enabled" : "Drone movement disabled", 1.5f);
            }
            if (inputManager.ToggleWallCollisionRequested)
            {
                playerWallCollisionEnabled = !playerWallCollisionEnabled;
                GUI.AddMessage(playerWallCollisionEnabled ? "Wall collision enabled" : "Wall collision disabled", 1.5f);
            }
            if (inputManager.FireRequested && !birdseyeEnabled)
                HandlePlayerFire();
        }

        void UpdateDroneDisableTimer(float dt)
        {
            if (dronesDisabledTimer <= 0f) return;
            dronesDisabledTimer -= dt;
            if (dronesDisabledTimer <= 0f)
            {
                dronesDisabledTimer = 0f;
                GUI.AddMessage("Drones reactivated", 1.5f);
            }
        }

        void UpdateDrones(float dt)
        {
            // Snapshot positions BEFORE the service moves them so we can compute
            // a movement delta and rotate each drone to face its travel direction.
            var prePositions = new Dictionary<string, Vector3>(drones.Count);
            foreach (var drone in drones)
            {
                if (drone.IsDisabled) continue;
                var pos = entityQueryService.GetPosition(drone.EntityName);
                if (pos.HasValue) prePositions[drone.EntityName] = pos.Value;
            }

            droneService.UpdateDrones(
                drones,
                camera.Position + PlayerOffset,
                settings.DroneTouchDistance,
                settings.DroneSpeed,
                dronesMovementEnabled,
                dronesDisabledTimer,
                dt,
                drone => drone.IsDisabled,
                drone => drone.EntityName,
                drone => drone.CurrentTargetCell,
                (drone, cell) => drone.CurrentTargetCell = cell,
                entityQueryService.GetPosition,
                entityQueryService.SetPosition,
                _ => OnPlayerCaught());

            // After movement: update each drone's rotation to face its movement vector.
            foreach (var drone in drones)
            {
                if (drone.IsDisabled) continue;
                if (!prePositions.TryGetValue(drone.EntityName, out Vector3 before)) continue;

                var afterPos = entityQueryService.GetPosition(drone.EntityName);
                if (!afterPos.HasValue) continue;

                Vector3 delta = afterPos.Value - before;
                delta.Y = 0f;
                if (delta.LengthSquared < 1e-6f) continue; // not moving — keep last rotation

                // Rotate the forward axis (-Z) to the movement direction.
                Vector3 forward = Vector3.Normalize(delta);
                float yaw = MathF.Atan2(forward.X, forward.Z);
                Quaternion rotation = Quaternion.FromAxisAngle(Vector3.UnitY, yaw);

                var entity = entityQueryService.Find(drone.EntityName);
                var transform = entity?.GetComponent<Engine.Components.TransformComponent>();
                if (transform != null)
                    transform.Rotation = rotation;
            }
        }

        Vector3 MazeCellToWorld(int x, int z, float y)
        {
            float half = (MazeGridSize / 2f) - 0.5f;
            float worldX = MazeOrigin.X + (x - half) * MazeCellSize;
            float worldZ = MazeOrigin.Z + (z - half) * MazeCellSize;
            return new Vector3(worldX, y, worldZ);
        }

        void UpdatePowerUps()
        {
            powerUpService.UpdatePowerUps(
                powerUps,
                camera.Position + PlayerOffset,
                powerUp => powerUp.Collected,
                powerUp =>
                {
                    powerUp.Collected = true;
                    audioService.Stop(PowerUpBeepKey(powerUp.EntityName));
                },
                powerUp => powerUp.EntityName,
                powerUp => powerUp.Type,
                entityQueryService.GetPosition,
                entityQueryService.SetPosition,
                type => ApplyPowerUp((PowerUpType)type));

            // Keep each uncollected beep anchored to its power-up's world position.
            foreach (var powerUp in powerUps)
            {
                if (powerUp.Collected) continue;
                var pos = entityQueryService.GetPosition(powerUp.EntityName);
                if (pos.HasValue)
                    audioService.SetPosition(PowerUpBeepKey(powerUp.EntityName), pos.Value);
            }
        }

        void CheckGameState()
        {
            if (drones.All(d => d.IsDisabled))
            {
                GameSessionState.LastGameResultMessage = "Victory - All drones disabled";
                GameSessionState.LastScore = ComputeScore();
                sceneManager.StartGameOver();
            }
        }

        int ComputeScore()
        {
            int dronesDisabled = drones.Count(d => d.IsDisabled);
            int livesLost      = settings.MaxLives - playerLives;
            return Math.Max(0, dronesDisabled * 100 - livesLost * 50);
        }

        void UpdateMouseLook()
        {
            float deltaX = inputManager.ConsumeMouseDeltaX();
            if (Math.Abs(deltaX) < 0.001f) return;
            camera.RotateY(deltaX * settings.MouseSensitivity);
            NormalizeCameraOrientation();
        }

        Vector3 GetFlatForward()
        {
            Vector3 forward = camera.Direction;
            forward.Y = 0f;
            if (forward.LengthSquared <= 0f) return -Vector3.UnitZ;
            return Vector3.Normalize(forward);
        }

        Vector3 GetFlatRight()
        {
            Vector3 forward = GetFlatForward();
            Vector3 right = Vector3.Cross(forward, Vector3.UnitY);
            if (right.LengthSquared <= 0f) return Vector3.UnitX;
            return Vector3.Normalize(right);
        }

        void ResolvePlayerMovement(Vector3 previousCameraPosition, Vector3 desiredCameraPosition)
        {
            Vector3 previousPlayerPosition = previousCameraPosition + PlayerOffset;
            Vector3 desiredPlayerPosition = desiredCameraPosition + PlayerOffset;
            Vector3 resolvedPlayerPosition = worldCollisionService.ResolvePlayerMovement(
                PlayerEntityName,
                previousPlayerPosition,
                desiredPlayerPosition);

            entityQueryService.SetPosition(PlayerEntityName, resolvedPlayerPosition);
            camera.Position = resolvedPlayerPosition - PlayerOffset;
            camera.Position = new Vector3(camera.Position.X, previousCameraPosition.Y, camera.Position.Z);
        }

        void ApplyPowerUp(PowerUpType type)
        {
            audioService.Restart("pickup");
            switch (type)
            {
                case PowerUpType.Health:
                    if (playerLives < settings.MaxLives) playerLives++;
                    GUI.AddMessage("Health collected", 1.5f);
                    break;
                case PowerUpType.WeaponUpgrade:
                    weaponDamage++;
                    GUI.AddMessage("Weapon upgraded", 1.5f);
                    break;
                case PowerUpType.DisableDrones:
                    dronesDisabledTimer = settings.DroneDisableDuration;
                    GUI.AddMessage("EMP activated", 1.5f);
                    break;
            }
        }

        void HandlePlayerFire()
        {
            GUI.AddMessage("Fire", 0.25f);
            audioService.Restart("fire");
            Vector3 origin = camera.Position;
            Vector3 direction = GetFlatForward();
            DroneState bestHit = null;
            float bestDistance = float.MaxValue;

            foreach (var drone in drones)
            {
                if (drone.IsDisabled) continue;
                var dronePos = entityQueryService.GetPosition(drone.EntityName);
                if (!dronePos.HasValue) continue;

                Vector3 toDrone = dronePos.Value - origin;
                float forwardDistance = Vector3.Dot(toDrone, direction);
                if (forwardDistance < 0f || forwardDistance > settings.WeaponRange) continue;

                Vector3 closestPoint = origin + direction * forwardDistance;
                float distanceFromRay = (dronePos.Value - closestPoint).Length;
                if (distanceFromRay > settings.WeaponHitRadius) continue;

                if (forwardDistance < bestDistance)
                {
                    bestDistance = forwardDistance;
                    bestHit = drone;
                }
            }

            if (bestHit == null) return;

            bestHit.Health -= weaponDamage;
            GUI.AddMessage($"{bestHit.EntityName} hit", 1.0f);
            if (bestHit.Health <= 0)
            {
                bestHit.IsDisabled = true;
                bestHit.CurrentTargetCell = null;
                entityQueryService.SetPosition(bestHit.EntityName, new Vector3(1000f, -1000f, 1000f));
                audioService.Restart("explosion");
                GUI.AddMessage($"{bestHit.EntityName} disabled", 2.0f);
            }
        }

        void OnPlayerCaught()
        {
            playerLives--;
            audioService.Restart("gameover");
            GUI.AddMessage($"Drone hit! Lives: {playerLives}", 2.0f);
            if (playerLives <= 0)
            {
                GameSessionState.LastGameResultMessage = "Defeat - You lost all 3 lives";
                GameSessionState.LastScore = ComputeScore();
                sceneManager.StartGameOver();
                return;
            }
            ResetPositionsAfterLifeLoss();
        }

        void ResetPositionsAfterLifeLoss()
        {
            ResetPlayerPosition();
            dronesDisabledTimer = 0f;

            foreach (var drone in drones)
            {
                drone.Health = settings.BaseDroneHealth;
                drone.CurrentTargetCell = null;
                drone.IsDisabled = false;
                entityQueryService.SetPosition(drone.EntityName, drone.StartPosition);
            }
        }

        void ToggleBirdseyeView()
        {
            birdseyeEnabled = !birdseyeEnabled;
            if (birdseyeEnabled)
            {
                savedCameraPosition = camera.Position;
                savedCameraDirection = camera.Direction;
                savedCameraUp = camera.Up;
                sceneManager.CursorState = CursorState.Normal;
                camera.Position = BirdseyePosition;
                camera.Direction = Vector3.Normalize(BirdseyeTarget - BirdseyePosition);
                camera.Up = Vector3.UnitZ;
                camera.SetPerspective(settings.BirdseyeFovDegrees, (float)sceneManager.Size.X / sceneManager.Size.Y, 0.1f, 200f);
            }
            else
            {
                camera.Position = savedCameraPosition;
                camera.Direction = savedCameraDirection;
                camera.Up = savedCameraUp;
                sceneManager.CursorState = CursorState.Grabbed;
                camera.SetPerspective(settings.NormalFovDegrees, (float)sceneManager.Size.X / sceneManager.Size.Y, 0.1f, 200f);
            }
            camera.UpdateView();
        }

        void ResetPlayerPosition()
        {
            camera.Position = cameraStartPosition;
            camera.Direction = new Vector3(0f, 0f, -1f);
            camera.Up = Vector3.UnitY;
            camera.UpdateView();
            entityQueryService.SetPosition(PlayerEntityName, camera.Position + PlayerOffset);
        }

        void SyncSceneState()
        {
            entityQueryService.SetPosition("Skybox", camera.Position);
            entityQueryService.SetPosition(PlayerEntityName, camera.Position + PlayerOffset);

            SyncAudioListener();
        }

        void SyncAudioListener()
        {
            AL.Listener(ALListener3f.Position, camera.Position.X, camera.Position.Y, camera.Position.Z);
            AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);
            float[] orientation = {camera.Direction.X, camera.Direction.Y, camera.Direction.Z, camera.Up.X, camera.Up.Y, camera.Up.Z};
            AL.Listener(ALListenerfv.Orientation, orientation);
        }

        public override void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Size.X, sceneManager.Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // RenderSystem must run here, after GL.Clear and before SwapBuffers,
            // not inside Update() where GL.Clear() would wipe its output.
            engineRenderSystemManager.UpdateAll(engineEntityManager, 0f);

            hudRenderService.Render(
                sceneManager.Size,
                birdseyeEnabled,
                playerLives,
                weaponDamage,
                drones.Select(d => (d.EntityName, d.Health, d.IsDisabled)),
                dronesMovementEnabled,
                playerWallCollisionEnabled,
                powerUps.Count(p => !p.Collected),
                dronesDisabledTimer,
                paused);
        }

        public override void Close()
        {
            engineLoop.Close();
            inputBridge.Detach();
            sceneManager.keyboardDownDelegate -= Keyboard_KeyDown;
            sceneManager.mouseUpDelegate -= Mouse_ButtonUp;
            sceneManager.CursorState = CursorState.Normal;
            audioService.Close();
            ResourceManager.RemoveAllAssets();
        }

        void Keyboard_KeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
                sceneManager.StartMenu();
        }

        void Mouse_ButtonUp(MouseButtonEventArgs e)
        {
        }

        sealed class RefactorLoopAdapter : EngineScene
        {
            readonly RefactorGameScene owner;

            public RefactorLoopAdapter(RefactorGameScene owner, SceneManager sceneManager) : base(sceneManager)
            {
                this.owner = owner;
            }

            public override void Render(FrameEventArgs e) => owner.Render(e);
            public override void Update(FrameEventArgs e) => owner.Update(e);
            public override void Close() { }
        }

        sealed class DroneState
        {
            public string EntityName { get; }
            public Vector3 StartPosition { get; }
            public int Health { get; set; }
            public bool IsDisabled { get; set; }
            public (int x, int z)? CurrentTargetCell { get; set; }

            public DroneState(string entityName, Vector3 startPosition, int health)
            {
                EntityName = entityName;
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

        sealed class PowerUpState
        {
            public string EntityName { get; }
            public PowerUpType Type { get; }
            public Vector3 StartPosition { get; }
            public bool Collected { get; set; }

            public PowerUpState(string entityName, PowerUpType type, Vector3 startPosition)
            {
                EntityName = entityName;
                Type = type;
                StartPosition = startPosition;
            }
        }
    }
}
