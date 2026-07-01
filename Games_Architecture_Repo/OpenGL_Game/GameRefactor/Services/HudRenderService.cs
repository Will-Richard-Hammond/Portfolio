using System.Collections.Generic;
using System.Linq;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.GameRefactor.Services
{
    class HudRenderService
    {
        // Number of health pips to display per drone (must match BaseDroneHealth in GameSettings).
        const int MaxDroneHealth = 3;

        public void Render(
            Vector2i viewportSize,
            bool birdseyeEnabled,
            int playerLives,
            int weaponDamage,
            IEnumerable<(string name, int health, bool isDisabled)> droneStates,
            bool dronesMovementEnabled,
            bool playerWallCollisionEnabled,
            int powerUpsLeft,
            float dronesDisabledTimer,
            bool paused)
        {
            const float hudTop  = 60f;
            const float hudStep = 35f;

            GUI.DrawText(birdseyeEnabled ? "Birdseye: ON (B to toggle)" : "Birdseye: OFF (B to toggle)",
                30, hudTop, 20, 255, 255, 0);
            GUI.DrawText($"Lives: {new string('\u2665', playerLives)}",
                30, hudTop + hudStep, 28, 255, 80, 80);
            GUI.DrawText($"Weapon Level: {weaponDamage}",
                30, hudTop + hudStep * 2, 24, 255, 255, 255);

            // ?? Drone status list ?????????????????????????????????????????
            GUI.DrawText("DRONES:", 30, hudTop + hudStep * 3, 22, 200, 200, 255);

            float droneListY = hudTop + hudStep * 4;
            foreach (var (name, health, isDisabled) in droneStates)
            {
                if (isDisabled)
                {
                    // Disabled: red, health bar fully empty, [DISABLED] tag
                    GUI.DrawText($"  {name}  [------]  DISABLED",
                        30, droneListY, 22, 200, 60, 60);
                }
                else
                {
                    // Health pip bar: filled ?, empty ?
                    int filled = System.Math.Clamp(health, 0, MaxDroneHealth);
                    string pips = new string('\u2588', filled)
                                + new string('\u2591', MaxDroneHealth - filled);

                    // Colour: green at full health, yellow when damaged
                    bool damaged = health < MaxDroneHealth;
                    byte r = damaged ? (byte)255 : (byte)80;
                    byte g = damaged ? (byte)220 : (byte)255;
                    byte b = 80;

                    GUI.DrawText($"  {name}  [{pips}]  HP:{health}",
                        30, droneListY, 22, r, g, b);
                }
                droneListY += hudStep * 0.85f;
            }
            // ?????????????????????????????????????????????????????????????

            // Shift the remaining rows below the drone list.
            float afterDrones = droneListY + hudStep * 0.3f;

            GUI.DrawText($"Drone Move: {(dronesMovementEnabled ? "ON" : "OFF")} (M)",
                30, afterDrones, 24, 255, 255, 255);
            GUI.DrawText($"Wall Collide: {(playerWallCollisionEnabled ? "ON" : "OFF")} (C)",
                30, afterDrones + hudStep, 24, 255, 255, 255);
            GUI.DrawText($"PowerUps Left: {powerUpsLeft}",
                30, afterDrones + hudStep * 2, 24, 255, 255, 255);

            if (dronesDisabledTimer > 0f)
                GUI.DrawText($"EMP Active: {dronesDisabledTimer:0.0}s",
                    30, afterDrones + hudStep * 3, 24, 0, 255, 255);

            if (paused)
            {
                GUI.DrawText("PAUSED",
                    viewportSize.X * 0.5f - 70, viewportSize.Y * 0.5f, 42, 255, 255, 0);
                GUI.DrawText("P = Resume   Esc = Main Menu",
                    viewportSize.X * 0.5f - 150, viewportSize.Y * 0.5f + 40, 24, 255, 255, 255);
            }

            GUI.Render();
        }
    }
}
