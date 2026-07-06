using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessInventorySystem.Models;

namespace BusinessInventorySystem.Services
{
    /// <summary>
    /// Reads/writes the inventory snapshot as JSON under %AppData%\BusinessInventorySystem.
    /// Pure persistence — no business logic lives here.
    /// </summary>
    public static class InventoryFileStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string FilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BusinessInventorySystem",
            "inventory.json");

        /// <summary>
        /// Returns the saved snapshot, or null if no file exists yet. A corrupt file is
        /// moved aside to inventory.json.corrupt so the app can start fresh without losing it.
        /// </summary>
        public static InventorySnapshot? Load()
        {
            if (!File.Exists(FilePath))
                return null;

            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<InventorySnapshot>(json, Options);
            }
            catch (JsonException)
            {
                File.Copy(FilePath, FilePath + ".corrupt", overwrite: true);
                File.Delete(FilePath);
                return null;
            }
        }

        /// <summary>Writes to a temp file first so a crash mid-write can't corrupt the save.</summary>
        public static void Save(InventorySnapshot snapshot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(snapshot, Options);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, overwrite: true);
        }
    }
}
