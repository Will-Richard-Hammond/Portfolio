using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HighscoreServer
{
    public record HighscoreEntry(string Name, int Score, DateTime Date);

    class HighscoreManager
    {
        readonly string filePath;
        readonly object sync = new();
        List<HighscoreEntry> scores;
        readonly int maxEntries;
        readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = false };

        public HighscoreManager(string filePath, int maxEntries = 10)
        {
            this.filePath = filePath;
            this.maxEntries = maxEntries;
            scores = Load();
        }

        List<HighscoreEntry> Load()
        {
            try
            {
                if (!File.Exists(filePath)) return new List<HighscoreEntry>();
                var json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<HighscoreEntry>>(json, jsonOptions);
                var result = list ?? new List<HighscoreEntry>();
                result.Sort((a, b) => b.Score.CompareTo(a.Score));
                if (result.Count > maxEntries) result.RemoveRange(maxEntries, result.Count - maxEntries);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to load highscores ({ex.Message}), starting fresh.");
                return new List<HighscoreEntry>();
            }
        }

        void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var json = JsonSerializer.Serialize(scores, jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving highscores: {ex.Message}");
            }
        }

        public List<HighscoreEntry> GetTop()
        {
            lock (sync)
            {
                return new List<HighscoreEntry>(scores);
            }
        }

        public void Add(string name, int score)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "ANON";
            name = name.Trim();
            if (name.Length > 32) name = name.Substring(0, 32);

            lock (sync)
            {
                scores.Add(new HighscoreEntry(name, score, DateTime.UtcNow));
                scores.Sort((a, b) => b.Score.CompareTo(a.Score));
                if (scores.Count > maxEntries) scores.RemoveRange(maxEntries, scores.Count - maxEntries);
                Save();
            }
        }
    }

    class Server
    {
        static async Task Main(string[] args)
        {
            int port = 8080;
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start listener: {ex.Message}");
                return;
            }
            Console.WriteLine($"Server listening on {IPAddress.Loopback}:{port}");

            var hsFile = Path.Combine(AppContext.BaseDirectory, "highscores.json");
            var hsm = new HighscoreManager(hsFile, maxEntries: 10);
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    _ = HandleClientAsync(client, hsm, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Listener error: {ex.Message}");
            }
            finally
            {
                listener.Stop();
                Console.WriteLine("Server stopped.");
            }
        }

        static async Task HandleClientAsync(TcpClient client, HighscoreManager hsm, CancellationToken token)
        {
            var endpoint = client.Client?.RemoteEndPoint;
            Console.WriteLine($"Client connected: {endpoint}");

            //Use UTF8 WITHOUT BOM to avoid sending a BOM into the stream
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, utf8NoBom))
            using (var writer = new StreamWriter(stream, utf8NoBom) { AutoFlush = true })
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        string? raw = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (raw == null) break;

                        //Normalize — remove BOM and trim whitespace so command detection works
                        string line = raw.Trim();
                        if (line.Length > 0 && line[0] == '\uFEFF') line = line.Substring(1);
                        Console.WriteLine($"Received from {endpoint}: '{line}'");

                        if (line.StartsWith("GET_SCORES", StringComparison.OrdinalIgnoreCase))
                        {
                            var list = hsm.GetTop();
                            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = false });
                            Console.WriteLine($"Sending GET_SCORES response to {endpoint}: {json}");
                            await writer.WriteLineAsync(json).ConfigureAwait(false);
                        }
                        else if (line.StartsWith("SUBMIT ", StringComparison.OrdinalIgnoreCase))
                        {
                            var payload = line.Substring(7);
                            Console.WriteLine($"Submit payload from {endpoint}: '{payload}'");
                            var parts = payload.Split('|', 2);
                            if (parts.Length == 2 && int.TryParse(parts[1], out int score))
                            {
                                var name = parts[0].Trim();
                                if (string.IsNullOrEmpty(name)) name = "ANON";
                                hsm.Add(name, score);
                                var updated = hsm.GetTop();
                                Console.WriteLine($"Highscores after add: {JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = false })}");
                                await writer.WriteLineAsync("OK").ConfigureAwait(false);
                            }
                            else
                            {
                                Console.WriteLine($"Invalid submit format from {endpoint}: '{payload}'");
                                await writer.WriteLineAsync("ERR invalid submit format").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            //fallback echo
                            Console.WriteLine($"Echoing string: {line}");
                            await writer.WriteLineAsync("Echoing string: " + line).ConfigureAwait(false);
                        }
                    }
                }
                catch (IOException) { /*network error*/ }
                catch (Exception ex) { Console.WriteLine($"Client handler error: {ex.Message}"); }
                finally { Console.WriteLine($"Client disconnected: {endpoint}"); }
            }
        }
    }
}