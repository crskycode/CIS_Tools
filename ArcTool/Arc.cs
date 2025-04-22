using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable IDE0270

namespace ArcTool
{
    internal partial class Arc
    {
        public static void Extract(string filePath, string outputPath)
        {
            var dirEntries = ReadDirectoryEntries(filePath);

            Directory.CreateDirectory(outputPath);

            foreach (var dirEntry in dirEntries)
            {
                var maxEntries = dirEntry.Entries.Count;

                for (var i = 0; i < maxEntries; i++)
                {
                    var entry = dirEntry.Entries[i];

                    var entryName = FormatInteger(i, maxEntries);

                    entry.Path = Path.Combine("Content", dirEntry.Name, entryName);

                    Console.WriteLine($"Extract {entry.Path}");

                    var partPath = filePath + (entry.Part != 1 ? entry.Part.ToString() : "");

                    using var fs = File.OpenRead(partPath);
                    using var br = new BinaryReader(fs);

                    fs.Position = entry.Position;

                    var data = br.ReadBytes(entry.Length);

                    var entryPath = Path.Combine(outputPath, entry.Path);
                    var entryDirPath = Path.GetDirectoryName(entryPath)!;

                    Directory.CreateDirectory(entryDirPath);

                    File.WriteAllBytes(entryPath, data);
                }
            }

            var indexObj = new Index
            {
                Name = Path.GetFileName(filePath),
                Directories = dirEntries,
            };

            var indexJson = JsonSerializer.Serialize(indexObj, JsonSourceGenerateContext.Default.Index);
            var indexJsonPath = Path.Combine(outputPath, "index.json");
            File.WriteAllText(indexJsonPath, indexJson);
        }

        private static List<DirEntry> ReadDirectoryEntries(string filePath)
        {
            var dirEntries = new List<DirEntry>();
            var encoding = Encoding.GetEncoding(932);

            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                var dirCount = reader.ReadInt32();

                for (var i = 0; i < dirCount; i++)
                {
                    var name = reader.ReadBytes(8)
                        .TakeWhile(x => x != 0)
                        .ToArray();

                    var entry = new DirEntry
                    {
                        Name = encoding.GetString(name),
                        Position = reader.ReadInt32(),
                        NumEntries = reader.ReadInt32(),
                    };

                    dirEntries.Add(entry);
                }

                foreach (var dirEntry in dirEntries)
                {
                    reader.BaseStream.Position = dirEntry.Position;

                    for (var i = 0; i < dirEntry.NumEntries; i++)
                    {
                        var entry = new Entry
                        {
                            Part = reader.ReadUInt16(),
                            Position = reader.ReadInt32(),
                            Length = reader.ReadInt32(),
                        };

                        dirEntry.Entries.Add(entry);
                    }
                }
            }

            return dirEntries;
        }

        public static void Create(string jsonPath, string outputPath)
        {
            var rootDir = Path.GetDirectoryName(jsonPath)!;

            var indexJson = File.ReadAllText(jsonPath);
            var indexObj = JsonSerializer.Deserialize(indexJson, JsonSourceGenerateContext.Default.Index);

            if (indexObj is null)
            {
                throw new Exception("Failed to load index.json");
            }

            Directory.CreateDirectory(outputPath);

            var partStreams = new Dictionary<int, FileStream>();

            try
            {
                // Write the main part
                var mainPartPath = Path.Combine(outputPath, indexObj.Name);
                var mainStream = File.Create(mainPartPath);
                partStreams[1] = mainStream;

                // Reserve space for the index in the file header
                var dirIndexLength = indexObj.Directories.Count * (8 + 4 + 4);
                var entryIndexLength = indexObj.Directories.Sum(x => x.Entries.Count) * (2 + 4 + 4);
                mainStream.Position = 4 + dirIndexLength + entryIndexLength;

                foreach (var dir in indexObj.Directories)
                {
                    foreach (var entry in dir.Entries)
                    {
                        Console.WriteLine($"Add {entry.Path}");

                        var entryPath = Path.Combine(rootDir, entry.Path);
                        var entryData = File.ReadAllBytes(entryPath);

                        if (partStreams.TryGetValue(entry.Part, out var stream))
                        {
                            entry.Position = Convert.ToInt32(stream.Position);
                            entry.Length = entryData.Length;

                            stream.Write(entryData);
                        }
                        else
                        {
                            var partName = indexObj.Name + (entry.Part != 1 ? entry.Part.ToString() : "");
                            var partPath = Path.Combine(outputPath, partName);

                            stream = File.Create(partPath);

                            entry.Position = Convert.ToInt32(stream.Position);
                            entry.Length = entryData.Length;

                            stream.Write(entryData);

                            partStreams.Add(entry.Part, stream);
                        }
                    }
                }

                // Write index
                Console.WriteLine("Create index");

                var bw = new BinaryWriter(mainStream);

                mainStream.Position = 0;

                bw.Write(indexObj.Directories.Count);

                mainStream.Position += dirIndexLength;

                foreach (var dir in indexObj.Directories)
                {
                    dir.Position = Convert.ToInt32(mainStream.Position);

                    foreach (var entry in dir.Entries)
                    {
                        bw.Write((ushort)entry.Part);
                        bw.Write(entry.Position);
                        bw.Write(entry.Length);
                    }
                }

                mainStream.Position = 4;

                var encoding = Encoding.GetEncoding(932);

                foreach (var dir in indexObj.Directories)
                {
                    var name = new byte[8];
                    var bytes = encoding.GetBytes(dir.Name);
                    Array.Copy(bytes, name, bytes.Length);

                    bw.Write(name);
                    bw.Write(dir.Position);
                    bw.Write(dir.Entries.Count);
                }

                Console.WriteLine("Finished");
            }
            finally
            {
                foreach (var item in partStreams)
                {
                    item.Value.Flush();
                    item.Value.Close();
                    item.Value.Dispose();
                }
            }
        }

        static string FormatInteger(int value, int maxValue)
        {
            var maxDigits = maxValue.ToString().Length;
            return value.ToString("D" + maxDigits);
        }

        private class DirEntry
        {
            public string Name { get; set; } = string.Empty;

            [JsonIgnore]
            public int Position { get; set; }

            [JsonIgnore]
            public int NumEntries { get; set; }

            public List<Entry> Entries { get; set; } = [];
        }

        private class Entry
        {
            public int Part { get; set; }

            [JsonIgnore]
            public int Position { get; set; }

            [JsonIgnore]
            public int Length { get; set; }

            public string Path { get; set; } = string.Empty;
        }

        private class Index
        {
            public string Name { get; set; } = string.Empty;
            public List<DirEntry> Directories { get; set; } = [];
        }

        [JsonSourceGenerationOptions(WriteIndented = true)]
        [JsonSerializable(typeof(Index))]
        private partial class JsonSourceGenerateContext : JsonSerializerContext
        {
        }
    }
}
