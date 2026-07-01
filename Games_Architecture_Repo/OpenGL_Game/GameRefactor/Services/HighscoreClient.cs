using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGL_Game.GameRefactor.Services
{
    /// <summary>One highscore entry; mirrors the server-side record.</summary>
    record HighscoreEntry(string Name, int Score, DateTime Date);

    /// <summary>
    /// Thin async wrapper around the TCP highscore server.
    /// Every method catches all exceptions and degrades gracefully so the
    /// game never crashes when the server is unavailable.
    /// Protocol (line-delimited, UTF-8 no-BOM):
    ///   ? GET_SCORES          ? JSON array of HighscoreEntry
    ///   ? SUBMIT name|score   ? "OK" or "ERR ..."
    /// </summary>
    class HighscoreClient
    {
        const string Host      = "127.0.0.1";
        const int    Port      = 8080;
        const int    TimeoutMs = 2000;

        static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        // ------------------------------------------------------------------ //

        /// <summary>Fetch the current leaderboard from the server.</summary>
        public async Task<List<HighscoreEntry>> GetScoresAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeoutMs);
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(Host, Port, cts.Token);
                using var ns     = tcp.GetStream();
                using var reader = new StreamReader(ns, Encoding.UTF8);
                using var writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };

                await writer.WriteLineAsync("GET_SCORES");
                var line = await reader.ReadLineAsync(cts.Token);
                return ParseList(line);
            }
            catch
            {
                return new List<HighscoreEntry>();
            }
        }

        /// <summary>
        /// Submit a score and fetch the refreshed leaderboard in one connection.
        /// Returns (ok, updatedScores); ok is false when the server is unreachable.
        /// </summary>
        public async Task<(bool ok, List<HighscoreEntry> scores)> SubmitAndRefreshAsync(
            string initials, int score)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeoutMs * 2);
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(Host, Port, cts.Token);
                using var ns     = tcp.GetStream();
                using var reader = new StreamReader(ns, Encoding.UTF8);
                using var writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };

                // 1) Submit
                await writer.WriteLineAsync($"SUBMIT {initials}|{score}");
                var resp = await reader.ReadLineAsync(cts.Token);
                bool ok  = resp?.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase) == true;

                // 2) Refresh scores on the same connection
                await writer.WriteLineAsync("GET_SCORES");
                var json = await reader.ReadLineAsync(cts.Token);
                return (ok, ParseList(json));
            }
            catch
            {
                return (false, new List<HighscoreEntry>());
            }
        }

        // ------------------------------------------------------------------ //

        static List<HighscoreEntry> ParseList(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new List<HighscoreEntry>();
                return JsonSerializer.Deserialize<List<HighscoreEntry>>(json, JsonOpts)
                       ?? new List<HighscoreEntry>();
            }
            catch
            {
                return new List<HighscoreEntry>();
            }
        }
    }
}
