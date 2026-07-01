using System.IO;
using System.Text.Json;

namespace OpenGL_Game.Engine.Config
{
    static class SettingsLoader
    {
        public static T Load<T>(string path) where T : class
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}