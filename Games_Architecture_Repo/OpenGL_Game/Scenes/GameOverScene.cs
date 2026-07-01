using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.GameRefactor.Managers;
using OpenGL_Game.GameRefactor.Services;
using OpenGL_Game.Managers;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace OpenGL_Game.Scenes
{
    class GameOverScene : Scene
    {
        // ── constants ─────────────────────────────────────────────────────
        const int    MaxInitials    = 3;
        const int    MaxLeaderboard = 10;
        const string ServerOffline  = "(server offline)";

        // ── audio ─────────────────────────────────────────────────────────
        readonly AudioService audioService = new();

        // ── network ───────────────────────────────────────────────────────
        readonly HighscoreClient hsClient = new();

        // ── state ─────────────────────────────────────────────────────────
        enum Phase { EnterInitials, Submitting, ShowBoard }
        Phase phase = Phase.EnterInitials;

        string initials      = "";
        bool   cursorVisible = true;
        float  cursorTimer   = 0f;

        List<HighscoreEntry> leaderboard    = new();
        string               statusMessage  = "";
        bool                 serverAvailable = true;

        readonly int    score;
        readonly string resultMessage;

        // ── layout helpers ────────────────────────────────────────────────
        float CX => sceneManager.Size.X * 0.5f;

        public GameOverScene(SceneManager sceneManager) : base(sceneManager)
        {
            score         = GameSessionState.LastScore;
            resultMessage = GameSessionState.LastGameResultMessage;

            sceneManager.Title = "Game Over";
            sceneManager.renderer         = Render;
            sceneManager.updater          = Update;
            sceneManager.keyboardDownDelegate += Keyboard_KeyDown;
            sceneManager.mouseDelegate        += Mouse_ButtonPressed;

            // TextInput (GLFW char callback) is intentionally NOT used here.
            // It does not fire reliably after transitioning from a
            // CursorState.Grabbed scene. Letter keys are captured directly
            // inside Keyboard_KeyDown instead.

            GL.ClearColor(0.07f, 0.05f, 0.12f, 1.0f);

            audioService.Register("gameover", "Audio/gameover.wav");
            audioService.Play("gameover");

            // Pre-fetch the leaderboard in the background.
            // The resulting "Client disconnected" in the server log is expected –
            // GetScoresAsync opens its own connection and closes it when done.
            _ = FetchLeaderboardAsync();
        }

        // ── update ────────────────────────────────────────────────────────

        public override void Update(FrameEventArgs e)
        {
            if (phase != Phase.EnterInitials) return;

            cursorTimer += (float)e.Time;
            if (cursorTimer >= 0.5f)
            {
                cursorTimer   = 0f;
                cursorVisible = !cursorVisible;
            }
        }

        // ── input ─────────────────────────────────────────────────────────

        void Keyboard_KeyDown(KeyboardKeyEventArgs e)
        {
            if (phase == Phase.EnterInitials)
            {
                switch (e.Key)
                {
                    case Keys.Backspace when initials.Length > 0:
                        initials = initials[..^1];
                        break;

                    case Keys.Enter when initials.Length > 0:
                        _ = SubmitScoreAsync();
                        break;

                    case Keys.Escape:
                        sceneManager.StartMenu();
                        break;

                    default:
                        // Capture A–Z directly from key events.
                        // Keys.A.ToString() == "A", Keys.Z.ToString() == "Z", etc.
                        // Multi-character names (F1, Space, LeftShift …) are skipped.
                        if (initials.Length < MaxInitials)
                        {
                            string keyName = e.Key.ToString();
                            if (keyName.Length == 1 && char.IsAsciiLetter(keyName[0]))
                                initials += keyName; // enum names are already upper-case
                        }
                        break;
                }
                return;
            }

            if (phase == Phase.ShowBoard)
                sceneManager.StartMenu();
        }

        void Mouse_ButtonPressed(MouseButtonEventArgs e)
        {
            if (phase == Phase.ShowBoard)
                sceneManager.StartMenu();
        }

        // ── async server calls ────────────────────────────────────────────

        async Task FetchLeaderboardAsync()
        {
            var list = await hsClient.GetScoresAsync();
            leaderboard     = list;
            serverAvailable = true;
        }

        async Task SubmitScoreAsync()
        {
            phase         = Phase.Submitting;
            statusMessage = "Submitting…";

            var (ok, updated) = await hsClient.SubmitAndRefreshAsync(initials, score);

            if (ok)
            {
                leaderboard     = updated;
                serverAvailable = true;
                statusMessage   = "Saved!";
            }
            else
            {
                serverAvailable = false;
                statusMessage   = ServerOffline;
            }

            phase = Phase.ShowBoard;
        }

        // ── render ────────────────────────────────────────────────────────

        public override void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Size.X, sceneManager.Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            switch (phase)
            {
                case Phase.EnterInitials: RenderEnterInitials(); break;
                case Phase.Submitting:    RenderSubmitting();    break;
                case Phase.ShowBoard:     RenderLeaderboard();   break;
            }

            GUI.Render();
        }

        void RenderEnterInitials()
        {
            DrawCentred("GAME OVER", CX, 110, 80, 220, 40, 40);
            DrawCentred(resultMessage, CX, 170, 22, 200, 200, 200);
            DrawCentred($"Your score:  {score}", CX, 210, 28, 255, 220, 60);

            DrawCentred("Enter your initials (up to 3 letters)", CX, 280, 22, 180, 180, 255);

            string display = initials + (cursorVisible ? "_" : " ");
            DrawCentred(display, CX, 340, 64, 255, 255, 255);

            DrawCentred("ENTER = confirm   BACKSPACE = delete   ESC = menu", CX, 410, 18, 140, 140, 140);

            if (leaderboard.Count > 0)
                RenderBoardPreview(460f);
        }

        void RenderSubmitting()
        {
            DrawCentred("GAME OVER", CX, 110, 80, 220, 40, 40);
            DrawCentred(statusMessage, CX, 320, 32, 180, 180, 255);
        }

        void RenderLeaderboard()
        {
            DrawCentred("GAME OVER", CX, 90, 64, 220, 40, 40);
            DrawCentred(resultMessage, CX, 145, 22, 200, 200, 200);
            DrawCentred($"Your score:  {score}   ({initials})", CX, 178, 26, 255, 220, 60);

            if (!serverAvailable)
                DrawCentred(ServerOffline, CX, 210, 20, 200, 80, 80);

            float tableTop  = 220f;
            float rowHeight = 30f;

            DrawCentred("── HIGHSCORES ──", CX, tableTop, 22, 180, 180, 255);
            tableTop += 34f;

            if (leaderboard.Count == 0)
            {
                DrawCentred("No scores yet", CX, tableTop, 20, 140, 140, 140);
            }
            else
            {
                for (int i = 0; i < Math.Min(leaderboard.Count, MaxLeaderboard); i++)
                {
                    var  entry    = leaderboard[i];
                    bool isPlayer = entry.Name.Equals(initials, StringComparison.OrdinalIgnoreCase)
                                    && entry.Score == score;

                    byte r = isPlayer ? (byte)255 : (byte)220;
                    byte g = isPlayer ? (byte)220 : (byte)220;
                    byte b = isPlayer ? (byte)60  : (byte)220;

                    string line = $"{i + 1,2}.  {entry.Name.PadRight(4)}  {entry.Score,6}";
                    GUI.DrawText(line, CX - 90, tableTop + i * rowHeight, 24, r, g, b);
                }
            }

            float footerY = tableTop + MaxLeaderboard * rowHeight + 20f;
            DrawCentred("Any key or click = Main Menu", CX, footerY, 18, 120, 120, 120);
        }

        void RenderBoardPreview(float startY)
        {
            GUI.DrawText("Current top scores:", 30, startY, 18, 140, 140, 180);
            float y   = startY + 24f;
            int   max = Math.Min(leaderboard.Count, 5);
            for (int i = 0; i < max; i++)
            {
                var e = leaderboard[i];
                GUI.DrawText($"  {i + 1}. {e.Name.PadRight(4)} {e.Score}", 30, y, 18, 180, 180, 180);
                y += 22f;
            }
        }

        // ── helpers ───────────────────────────────────────────────────────

        static void DrawCentred(string text, float cx, float y, float size, byte r, byte g, byte b)
        {
            float approxOffset = size * 0.3f * text.Length * 0.5f;
            GUI.DrawText(text, cx - approxOffset, y, size, r, g, b);
        }

        // ── lifecycle ─────────────────────────────────────────────────────

        public override void Close()
        {
            sceneManager.keyboardDownDelegate -= Keyboard_KeyDown;
            sceneManager.mouseDelegate        -= Mouse_ButtonPressed;
            audioService.Close();
        }
    }
}
