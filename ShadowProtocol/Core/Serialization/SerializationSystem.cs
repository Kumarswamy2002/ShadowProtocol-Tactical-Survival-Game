// =============================================================================
// ShadowProtocol.Core — Serialization: Binary serializer and versioned save format
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShadowProtocol.Core.Serialization
{
    /// <summary>
    /// Magic bytes identifying Shadow Protocol save files.
    /// </summary>
    public static class SaveFileConstants
    {
        /// <summary>Magic header: "SHPF" (Shadow Protocol File)</summary>
        public static readonly byte[] MagicBytes = { 0x53, 0x48, 0x50, 0x46 };

        /// <summary>Current save format version.</summary>
        public const int CurrentVersion = 3;

        /// <summary>Minimum supported version for backwards compatibility.</summary>
        public const int MinSupportedVersion = 1;

        /// <summary>File extension for save files.</summary>
        public const string SaveExtension = ".spsave";

        /// <summary>File extension for config files.</summary>
        public const string ConfigExtension = ".spcfg";
    }

    /// <summary>
    /// Header structure stored at the beginning of every save file.
    /// Provides version info, integrity checks, and metadata.
    /// </summary>
    public class SaveFileHeader
    {
        public int FormatVersion { get; set; }
        public string GameVersion { get; set; }
        public string SaveName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public long PlayTimeSeconds { get; set; }
        public int SlotIndex { get; set; }
        public uint Checksum { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Computes a CRC32 checksum for data integrity verification.
        /// </summary>
        public static uint ComputeChecksum(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }
            return ~crc;
        }
    }

    /// <summary>
    /// High-performance binary writer for game data serialization.
    /// Supports all primitive types, collections, vectors, and custom serializable objects.
    /// </summary>
    public class BinarySerializer : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly Dictionary<string, long> _sectionPositions = new();

        /// <summary>Current position in the output stream.</summary>
        public long Position => _stream.Position;

        /// <summary>Total bytes written so far.</summary>
        public long Length => _stream.Length;

        public BinarySerializer(int initialCapacity = 4096)
        {
            _stream = new MemoryStream(initialCapacity);
            _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        }

        // =====================================================================
        // Primitive Writers
        // =====================================================================

        public void WriteBool(bool value) => _writer.Write(value);
        public void WriteByte(byte value) => _writer.Write(value);
        public void WriteSByte(sbyte value) => _writer.Write(value);
        public void WriteInt16(short value) => _writer.Write(value);
        public void WriteUInt16(ushort value) => _writer.Write(value);
        public void WriteInt32(int value) => _writer.Write(value);
        public void WriteUInt32(uint value) => _writer.Write(value);
        public void WriteInt64(long value) => _writer.Write(value);
        public void WriteUInt64(ulong value) => _writer.Write(value);
        public void WriteFloat(float value) => _writer.Write(value);
        public void WriteDouble(double value) => _writer.Write(value);

        /// <summary>
        /// Writes a string with a length prefix.
        /// Null strings are stored as length -1.
        /// </summary>
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteInt32(bytes.Length);
            _writer.Write(bytes);
        }

        /// <summary>
        /// Writes a raw byte array with length prefix.
        /// </summary>
        public void WriteBytes(byte[] data)
        {
            if (data == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(data.Length);
            _writer.Write(data);
        }

        /// <summary>
        /// Writes a DateTime as a UTC ticks value.
        /// </summary>
        public void WriteDateTime(DateTime value) => WriteInt64(value.ToUniversalTime().Ticks);

        /// <summary>
        /// Writes a TimeSpan as ticks.
        /// </summary>
        public void WriteTimeSpan(TimeSpan value) => WriteInt64(value.Ticks);

        /// <summary>
        /// Writes a GUID as 16 bytes.
        /// </summary>
        public void WriteGuid(Guid value) => _writer.Write(value.ToByteArray());

        // =====================================================================
        // Game Type Writers
        // =====================================================================

        /// <summary>Writes a 3D vector (12 bytes).</summary>
        public void WriteVector3(Math.Vector3F v)
        {
            WriteFloat(v.X);
            WriteFloat(v.Y);
            WriteFloat(v.Z);
        }

        /// <summary>Writes a quaternion (16 bytes).</summary>
        public void WriteQuaternion(Math.QuaternionF q)
        {
            WriteFloat(q.X);
            WriteFloat(q.Y);
            WriteFloat(q.Z);
            WriteFloat(q.W);
        }

        /// <summary>Writes a 4x4 matrix (64 bytes).</summary>
        public void WriteMatrix4x4(Math.Matrix4x4F m)
        {
            float[] data = m.ToRowMajorArray();
            foreach (float f in data) WriteFloat(f);
        }

        // =====================================================================
        // Collection Writers
        // =====================================================================

        /// <summary>
        /// Writes a list of items using the provided element writer.
        /// </summary>
        public void WriteList<T>(IList<T> list, Action<BinarySerializer, T> elementWriter)
        {
            if (list == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(list.Count);
            foreach (var item in list)
                elementWriter(this, item);
        }

        /// <summary>
        /// Writes a dictionary using provided key/value writers.
        /// </summary>
        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict,
            Action<BinarySerializer, TKey> keyWriter,
            Action<BinarySerializer, TValue> valueWriter)
        {
            if (dict == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(dict.Count);
            foreach (var kvp in dict)
            {
                keyWriter(this, kvp.Key);
                valueWriter(this, kvp.Value);
            }
        }

        /// <summary>
        /// Writes a string-to-string dictionary (common for metadata).
        /// </summary>
        public void WriteStringDictionary(Dictionary<string, string> dict)
        {
            WriteDictionary(dict,
                (s, k) => s.WriteString(k),
                (s, v) => s.WriteString(v));
        }

        // =====================================================================
        // Section Management
        // =====================================================================

        /// <summary>
        /// Begins a named section in the binary stream.
        /// Sections allow skipping unknown data during deserialization.
        /// </summary>
        public void BeginSection(string name)
        {
            WriteString(name);
            _sectionPositions[name] = _stream.Position;
            WriteInt32(0); // Placeholder for section size
        }

        /// <summary>
        /// Ends the current section and writes its size.
        /// </summary>
        public void EndSection(string name)
        {
            if (!_sectionPositions.TryGetValue(name, out long startPos))
                throw new InvalidOperationException($"Section '{name}' was not started");

            long currentPos = _stream.Position;
            int sectionSize = (int)(currentPos - startPos - 4); // -4 for the size field itself

            _stream.Position = startPos;
            _writer.Write(sectionSize);
            _stream.Position = currentPos;

            _sectionPositions.Remove(name);
        }

        // =====================================================================
        // Output
        // =====================================================================

        /// <summary>Returns all written data as a byte array.</summary>
        public byte[] ToArray()
        {
            _writer.Flush();
            return _stream.ToArray();
        }

        /// <summary>Resets the stream for reuse.</summary>
        public void Reset()
        {
            _stream.SetLength(0);
            _stream.Position = 0;
            _sectionPositions.Clear();
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _stream?.Dispose();
        }
    }

    /// <summary>
    /// High-performance binary reader for game data deserialization.
    /// </summary>
    public class BinaryDeserializer : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly BinaryReader _reader;

        /// <summary>Current position in the stream.</summary>
        public long Position => _stream.Position;

        /// <summary>Total length of the data.</summary>
        public long Length => _stream.Length;

        /// <summary>Whether the end of the stream has been reached.</summary>
        public bool IsAtEnd => _stream.Position >= _stream.Length;

        public BinaryDeserializer(byte[] data)
        {
            _stream = new MemoryStream(data);
            _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        }

        // =====================================================================
        // Primitive Readers
        // =====================================================================

        public bool ReadBool() => _reader.ReadBoolean();
        public byte ReadByte() => _reader.ReadByte();
        public sbyte ReadSByte() => _reader.ReadSByte();
        public short ReadInt16() => _reader.ReadInt16();
        public ushort ReadUInt16() => _reader.ReadUInt16();
        public int ReadInt32() => _reader.ReadInt32();
        public uint ReadUInt32() => _reader.ReadUInt32();
        public long ReadInt64() => _reader.ReadInt64();
        public ulong ReadUInt64() => _reader.ReadUInt64();
        public float ReadFloat() => _reader.ReadSingle();
        public double ReadDouble() => _reader.ReadDouble();

        public string ReadString()
        {
            int length = ReadInt32();
            if (length < 0) return null;
            if (length == 0) return string.Empty;
            byte[] bytes = _reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadBytes()
        {
            int length = ReadInt32();
            if (length < 0) return null;
            return _reader.ReadBytes(length);
        }

        public DateTime ReadDateTime() => new DateTime(ReadInt64(), DateTimeKind.Utc);
        public TimeSpan ReadTimeSpan() => new TimeSpan(ReadInt64());
        public Guid ReadGuid() => new Guid(_reader.ReadBytes(16));

        // =====================================================================
        // Game Type Readers
        // =====================================================================

        public Math.Vector3F ReadVector3() => new Math.Vector3F(ReadFloat(), ReadFloat(), ReadFloat());
        public Math.QuaternionF ReadQuaternion() => new Math.QuaternionF(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());

        public Math.Matrix4x4F ReadMatrix4x4()
        {
            return new Math.Matrix4x4F(
                ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(),
                ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(),
                ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat(),
                ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat()
            );
        }

        // =====================================================================
        // Collection Readers
        // =====================================================================

        public List<T> ReadList<T>(Func<BinaryDeserializer, T> elementReader)
        {
            int count = ReadInt32();
            if (count < 0) return null;
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
                list.Add(elementReader(this));
            return list;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            Func<BinaryDeserializer, TKey> keyReader,
            Func<BinaryDeserializer, TValue> valueReader)
        {
            int count = ReadInt32();
            if (count < 0) return null;
            var dict = new Dictionary<TKey, TValue>(count);
            for (int i = 0; i < count; i++)
            {
                TKey key = keyReader(this);
                TValue value = valueReader(this);
                dict[key] = value;
            }
            return dict;
        }

        public Dictionary<string, string> ReadStringDictionary()
        {
            return ReadDictionary(r => r.ReadString(), r => r.ReadString());
        }

        // =====================================================================
        // Section Navigation
        // =====================================================================

        /// <summary>
        /// Reads a section header and returns its name and size.
        /// </summary>
        public (string Name, int Size) ReadSectionHeader()
        {
            string name = ReadString();
            int size = ReadInt32();
            return (name, size);
        }

        /// <summary>
        /// Skips ahead by the specified number of bytes.
        /// </summary>
        public void Skip(int bytes)
        {
            _stream.Position += bytes;
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _stream?.Dispose();
        }
    }

    /// <summary>
    /// High-level save file manager for reading and writing complete game saves.
    /// Supports compression, integrity checks, and version migration.
    /// </summary>
    public static class SaveFileSerializer
    {
        /// <summary>
        /// Serializes game data to a complete save file byte array.
        /// </summary>
        /// <param name="header">Save file metadata header.</param>
        /// <param name="serializer">Pre-populated binary serializer containing game data.</param>
        /// <param name="compress">Whether to GZip compress the data.</param>
        public static byte[] WriteSaveFile(SaveFileHeader header, BinarySerializer serializer, bool compress = true)
        {
            byte[] gameData = serializer.ToArray();

            if (compress)
            {
                gameData = Compress(gameData);
                header.IsCompressed = true;
            }

            header.Checksum = SaveFileHeader.ComputeChecksum(gameData);
            header.ModifiedAt = DateTime.UtcNow;
            header.FormatVersion = SaveFileConstants.CurrentVersion;

            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output, Encoding.UTF8);

            // Write magic bytes
            writer.Write(SaveFileConstants.MagicBytes);

            // Write header as JSON (human-readable portion)
            string headerJson = JsonSerializer.Serialize(header, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            byte[] headerBytes = Encoding.UTF8.GetBytes(headerJson);
            writer.Write(headerBytes.Length);
            writer.Write(headerBytes);

            // Write game data
            writer.Write(gameData.Length);
            writer.Write(gameData);

            return output.ToArray();
        }

        /// <summary>
        /// Reads a save file and returns the header and deserialized game data.
        /// </summary>
        public static (SaveFileHeader Header, BinaryDeserializer Data) ReadSaveFile(byte[] fileData)
        {
            using var input = new MemoryStream(fileData);
            using var reader = new BinaryReader(input, Encoding.UTF8);

            // Verify magic bytes
            byte[] magic = reader.ReadBytes(4);
            for (int i = 0; i < 4; i++)
            {
                if (magic[i] != SaveFileConstants.MagicBytes[i])
                    throw new InvalidDataException("Not a valid Shadow Protocol save file");
            }

            // Read header
            int headerLen = reader.ReadInt32();
            byte[] headerBytes = reader.ReadBytes(headerLen);
            string headerJson = Encoding.UTF8.GetString(headerBytes);
            var header = JsonSerializer.Deserialize<SaveFileHeader>(headerJson);

            // Version check
            if (header.FormatVersion < SaveFileConstants.MinSupportedVersion)
                throw new InvalidDataException(
                    $"Save file version {header.FormatVersion} is too old (minimum: {SaveFileConstants.MinSupportedVersion})");

            // Read game data
            int dataLen = reader.ReadInt32();
            byte[] gameData = reader.ReadBytes(dataLen);

            // Verify checksum
            uint checksum = SaveFileHeader.ComputeChecksum(gameData);
            if (checksum != header.Checksum)
                throw new InvalidDataException("Save file integrity check failed — data may be corrupted");

            // Decompress if needed
            if (header.IsCompressed)
                gameData = Decompress(gameData);

            // Apply version migrations
            if (header.FormatVersion < SaveFileConstants.CurrentVersion)
                gameData = MigrateData(gameData, header.FormatVersion);

            return (header, new BinaryDeserializer(gameData));
        }

        /// <summary>
        /// Reads only the header from a save file (without loading game data).
        /// </summary>
        public static SaveFileHeader ReadHeader(byte[] fileData)
        {
            using var input = new MemoryStream(fileData);
            using var reader = new BinaryReader(input, Encoding.UTF8);

            byte[] magic = reader.ReadBytes(4);
            int headerLen = reader.ReadInt32();
            byte[] headerBytes = reader.ReadBytes(headerLen);

            return JsonSerializer.Deserialize<SaveFileHeader>(Encoding.UTF8.GetString(headerBytes));
        }

        /// <summary>
        /// Lists all save files in a directory with their headers.
        /// </summary>
        public static List<(string FilePath, SaveFileHeader Header)> ListSaves(string directory)
        {
            var saves = new List<(string, SaveFileHeader)>();

            if (!Directory.Exists(directory)) return saves;

            foreach (string file in Directory.GetFiles(directory, $"*{SaveFileConstants.SaveExtension}"))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    var header = ReadHeader(data);
                    saves.Add((file, header));
                }
                catch
                {
                    // Skip corrupt save files
                }
            }

            return saves.OrderByDescending(s => s.Header.ModifiedAt).ToList();
        }

        // =====================================================================
        // Compression
        // =====================================================================

        private static byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        // =====================================================================
        // Version Migration
        // =====================================================================

        /// <summary>
        /// Applies sequential migrations to bring old save data up to the current version.
        /// </summary>
        private static byte[] MigrateData(byte[] data, int fromVersion)
        {
            var currentData = data;

            if (fromVersion < 2)
                currentData = MigrateV1ToV2(currentData);

            if (fromVersion < 3)
                currentData = MigrateV2ToV3(currentData);

            return currentData;
        }

        /// <summary>
        /// Migration from v1 to v2: Added player faction standings.
        /// </summary>
        private static byte[] MigrateV1ToV2(byte[] data)
        {
            // V1 saves don't have faction data — append default faction standings
            using var output = new MemoryStream();
            output.Write(data, 0, data.Length);

            using var writer = new BinaryWriter(output, Encoding.UTF8);
            // Write empty faction dictionary
            writer.Write(0); // 0 faction entries

            return output.ToArray();
        }

        /// <summary>
        /// Migration from v2 to v3: Added vehicle state and expanded inventory.
        /// </summary>
        private static byte[] MigrateV2ToV3(byte[] data)
        {
            using var output = new MemoryStream();
            output.Write(data, 0, data.Length);

            using var writer = new BinaryWriter(output, Encoding.UTF8);
            writer.Write(0);  // 0 vehicles
            writer.Write(0);  // 0 expanded inventory slots

            return output.ToArray();
        }
    }

    /// <summary>
    /// Interface for objects that can be serialized by the binary serializer.
    /// </summary>
    public interface IBinarySerializable
    {
        void Serialize(BinarySerializer serializer);
        void Deserialize(BinaryDeserializer deserializer);
    }

    /// <summary>
    /// Auto-save manager that periodically saves game state.
    /// </summary>
    public class AutoSaveManager
    {
        private float _intervalSeconds;
        private float _timeSinceLastSave;
        private int _maxAutoSaves;
        private string _saveDirectory;
        private bool _enabled;

        /// <summary>Whether auto-save is currently enabled.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Time remaining until next auto-save.</summary>
        public float TimeUntilNextSave => _intervalSeconds - _timeSinceLastSave;

        /// <summary>
        /// Creates a new auto-save manager.
        /// </summary>
        /// <param name="saveDirectory">Directory to store auto-saves.</param>
        /// <param name="intervalSeconds">Seconds between auto-saves.</param>
        /// <param name="maxAutoSaves">Maximum number of auto-save files to keep.</param>
        public AutoSaveManager(string saveDirectory, float intervalSeconds = 300f, int maxAutoSaves = 5)
        {
            _saveDirectory = saveDirectory;
            _intervalSeconds = intervalSeconds;
            _maxAutoSaves = maxAutoSaves;
            _timeSinceLastSave = 0;
            _enabled = true;

            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);
        }

        /// <summary>
        /// Updates the auto-save timer. Returns true if an auto-save should be triggered.
        /// </summary>
        public bool Update(float deltaTime)
        {
            if (!_enabled) return false;

            _timeSinceLastSave += deltaTime;
            if (_timeSinceLastSave >= _intervalSeconds)
            {
                _timeSinceLastSave = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Saves data and rotates old auto-saves.
        /// </summary>
        public void PerformAutoSave(byte[] saveData)
        {
            string filename = $"autosave_{DateTime.UtcNow:yyyyMMdd_HHmmss}{SaveFileConstants.SaveExtension}";
            string filepath = Path.Combine(_saveDirectory, filename);

            File.WriteAllBytes(filepath, saveData);
            RotateAutoSaves();
        }

        /// <summary>
        /// Removes old auto-saves exceeding the maximum count.
        /// </summary>
        private void RotateAutoSaves()
        {
            var saves = Directory.GetFiles(_saveDirectory, $"autosave_*{SaveFileConstants.SaveExtension}")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            for (int i = _maxAutoSaves; i < saves.Count; i++)
            {
                try { File.Delete(saves[i]); }
                catch { /* ignore deletion failures */ }
            }
        }

        /// <summary>
        /// Returns the most recent auto-save file path, or null if none exist.
        /// </summary>
        public string GetLatestAutoSave()
        {
            return Directory.GetFiles(_saveDirectory, $"autosave_*{SaveFileConstants.SaveExtension}")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();
        }
    }
}
