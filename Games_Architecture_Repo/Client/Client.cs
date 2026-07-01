using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

record HighscoreEntry(string Name, int Score, DateTime Date);

public class Client
{
    static readonly JsonSerializerOptions jsonOpts = new() { PropertyNameCaseInsensitive = true, WriteIndented = false };

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting client (highscore demo)...");
        int port = 8080;
        using TcpClient client = new TcpClient("localhost", port);
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        //1) Initialize: request highscores from server
        writer.WriteLine("GET_SCORES");
        string? json = reader.ReadLine();
        Console.WriteLine("DEBUG: raw GET_SCORES response: " + (json ?? "<null>"));
        var scores = ParseHighscores(json);
        PrintHighscores(scores);

        //2) Simulate gameplay loop on Game Over check/submit
        while (true)
        {
            Console.WriteLine();
            Console.Write("Play a round? (y/n): ");
            var yn = Console.ReadLine();
            if (!string.Equals(yn, "y", StringComparison.OrdinalIgnoreCase)) break;

            Console.Write("Enter your score (integer): ");
            if (!int.TryParse(Console.ReadLine(), out int score)) { Console.WriteLine("Invalid score."); continue; }

            //Game Over and check if score is a highscore
            if (IsHighscore(score, scores, maxEntries: 10))
            {
                Console.Write("New highscore! Enter your initials (max 32 chars): ");
                var name = Console.ReadLine() ?? "ANON";
                if (name.Length > 32) name = name.Substring(0, 32);

                //Submit to server("SUBMIT name|score")
                writer.WriteLine($"SUBMIT {name}|{score}");
                var resp = reader.ReadLine();
                Console.WriteLine("Server response: " + (resp ?? "<no response>"));

                //Refresh the local cached list
                writer.WriteLine("GET_SCORES");
                json = reader.ReadLine();
                Console.WriteLine("DEBUG: raw GET_SCORES response after submit: " + (json ?? "<null>"));
                scores = ParseHighscores(json);
                PrintHighscores(scores);
            }
            else
            {
                Console.WriteLine("Not a highscore. Try again.");
            }
        }

        Console.WriteLine("Client exiting.");
    }

    static List<HighscoreEntry> ParseHighscores(string? json)
    {
        try
        {
            if (string.IsNullOrEmpty(json)) return new List<HighscoreEntry>();
            var list = JsonSerializer.Deserialize<List<HighscoreEntry>>(json, jsonOpts);
            return list ?? new List<HighscoreEntry>();
        }
        catch
        {
            return new List<HighscoreEntry>();
        }
    }

    static void PrintHighscores(List<HighscoreEntry> list)
    {
        Console.WriteLine();
        Console.WriteLine("Highscores:");
        if (list.Count == 0) { Console.WriteLine("  (none)"); return; }
        int rank = 1;
        foreach (var e in list)
        {
            Console.WriteLine($"  {rank++}. {e.Name} - {e.Score} ({e.Date:u})");
        }
    }

    static bool IsHighscore(int score, List<HighscoreEntry> list, int maxEntries)
    {
        if (list.Count < maxEntries) return true;
        //list sorted descending on server and if not, sort locally
        int minScore = int.MaxValue;
        foreach (var e in list) if (e.Score < minScore) minScore = e.Score;
        return score > minScore;
    }
}