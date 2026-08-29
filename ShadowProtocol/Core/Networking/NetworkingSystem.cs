// =============================================================================
// ShadowProtocol.Core — Networking: Packet system, client, server, state sync
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowProtocol.Core.Networking
{
    /// <summary>
    /// Network packet opcodes for all game message types.
    /// </summary>
    public enum PacketOpcode : ushort
    {
        // Connection lifecycle (0x00xx)
        Handshake           = 0x0001,
        HandshakeResponse   = 0x0002,
        Heartbeat           = 0x0003,
        HeartbeatAck        = 0x0004,
        Disconnect          = 0x0005,
        ConnectionAccepted  = 0x0006,
        ConnectionRejected  = 0x0007,

        // Authentication (0x01xx)
        AuthRequest         = 0x0100,
        AuthResponse        = 0x0101,
        AuthChallenge       = 0x0102,

        // Player state (0x02xx)
        PlayerJoin          = 0x0200,
        PlayerLeave         = 0x0201,
        PlayerMove          = 0x0202,
        PlayerAction        = 0x0203,
        PlayerStateSync     = 0x0204,
        PlayerInput         = 0x0205,
        PlayerDamage        = 0x0206,
        PlayerDeath         = 0x0207,
        PlayerRespawn       = 0x0208,
        PlayerInventorySync = 0x0209,

        // World state (0x03xx)
        WorldStateSnapshot  = 0x0300,
        WorldStateDelta     = 0x0301,
        WorldEvent          = 0x0302,
        WorldTimeSync       = 0x0303,
        WeatherChange       = 0x0304,
        EntitySpawn         = 0x0305,
        EntityDestroy       = 0x0306,
        EntityUpdate        = 0x0307,

        // Combat (0x04xx)
        WeaponFire          = 0x0400,
        ProjectileSpawn     = 0x0401,
        HitConfirm          = 0x0402,
        ExplosionEvent      = 0x0403,
        GrenadeThrow        = 0x0404,

        // Chat and social (0x05xx)
        ChatMessage         = 0x0500,
        VoiceData           = 0x0501,
        TeamMessage         = 0x0502,

        // System (0xFFxx)
        Ping                = 0xFF00,
        Pong                = 0xFF01,
        ServerInfo          = 0xFF02,
        KickPlayer          = 0xFF03,
        BanPlayer           = 0xFF04,
    }

    /// <summary>
    /// Reliability mode for packet delivery.
    /// </summary>
    public enum DeliveryMode : byte
    {
        /// <summary>Fire-and-forget, no guarantees (lowest latency).</summary>
        Unreliable = 0,
        /// <summary>Guaranteed delivery with ordering.</summary>
        ReliableOrdered = 1,
        /// <summary>Guaranteed delivery without ordering guarantee.</summary>
        ReliableUnordered = 2,
        /// <summary>Unreliable but with sequencing (newer packets discard older ones).</summary>
        UnreliableSequenced = 3,
    }

    /// <summary>
    /// Network packet header structure.
    /// Total fixed header size: 16 bytes.
    /// </summary>
    public struct PacketHeader
    {
        public PacketOpcode Opcode;          // 2 bytes
        public DeliveryMode Delivery;         // 1 byte
        public byte Channel;                  // 1 byte
        public ushort SequenceNumber;         // 2 bytes
        public ushort AckSequence;            // 2 bytes
        public uint AckBitfield;              // 4 bytes
        public int PayloadLength;             // 4 bytes

        public const int HeaderSize = 16;

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)Opcode);
            writer.Write((byte)Delivery);
            writer.Write(Channel);
            writer.Write(SequenceNumber);
            writer.Write(AckSequence);
            writer.Write(AckBitfield);
            writer.Write(PayloadLength);
        }

        public static PacketHeader ReadFrom(BinaryReader reader)
        {
            return new PacketHeader
            {
                Opcode = (PacketOpcode)reader.ReadUInt16(),
                Delivery = (DeliveryMode)reader.ReadByte(),
                Channel = reader.ReadByte(),
                SequenceNumber = reader.ReadUInt16(),
                AckSequence = reader.ReadUInt16(),
                AckBitfield = reader.ReadUInt32(),
                PayloadLength = reader.ReadInt32()
            };
        }
    }

    /// <summary>
    /// Complete network packet with header and payload.
    /// </summary>
    public class Packet
    {
        public PacketHeader Header;
        public byte[] Payload;
        public DateTime Timestamp;
        public int SendAttempts;
        public float RetransmitTimer;

        /// <summary>Creates a packet for the given opcode with binary payload.</summary>
        public static Packet Create(PacketOpcode opcode, byte[] payload,
            DeliveryMode delivery = DeliveryMode.ReliableOrdered, byte channel = 0)
        {
            return new Packet
            {
                Header = new PacketHeader
                {
                    Opcode = opcode,
                    Delivery = delivery,
                    Channel = channel,
                    PayloadLength = payload?.Length ?? 0,
                },
                Payload = payload ?? Array.Empty<byte>(),
                Timestamp = DateTime.UtcNow,
                SendAttempts = 0,
            };
        }

        /// <summary>Serializes the full packet (header + payload) to bytes.</summary>
        public byte[] Serialize()
        {
            using var ms = new MemoryStream(PacketHeader.HeaderSize + Header.PayloadLength);
            using var writer = new BinaryWriter(ms);
            Header.WriteTo(writer);
            if (Payload != null && Payload.Length > 0)
                writer.Write(Payload);
            return ms.ToArray();
        }

        /// <summary>Deserializes a packet from raw bytes.</summary>
        public static Packet Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            var header = PacketHeader.ReadFrom(reader);
            byte[] payload = header.PayloadLength > 0
                ? reader.ReadBytes(header.PayloadLength)
                : Array.Empty<byte>();

            return new Packet
            {
                Header = header,
                Payload = payload,
                Timestamp = DateTime.UtcNow,
            };
        }
    }

    /// <summary>
    /// Reliable delivery layer that handles sequence numbers, acknowledgments,
    /// and packet retransmission for reliable-ordered delivery.
    /// </summary>
    public class ReliableChannel
    {
        private ushort _localSequence;
        private ushort _remoteSequence;
        private uint _ackBitfield;
        private readonly Dictionary<ushort, Packet> _pendingAcks = new();
        private readonly HashSet<ushort> _receivedSequences = new();
        private readonly Queue<Packet> _orderedQueue = new();
        private ushort _nextExpectedSequence;

        private const float RetransmitInterval = 0.2f; // 200ms
        private const int MaxRetransmits = 10;

        /// <summary>Current round-trip time estimate in seconds.</summary>
        public float RTT { get; private set; } = 0.1f;

        /// <summary>Packet loss rate (0..1).</summary>
        public float PacketLoss { get; private set; }

        private int _sentCount;
        private int _lostCount;

        /// <summary>Assigns sequence numbers to outgoing packets.</summary>
        public void PrepareOutgoing(Packet packet)
        {
            packet.Header.SequenceNumber = _localSequence++;
            packet.Header.AckSequence = _remoteSequence;
            packet.Header.AckBitfield = _ackBitfield;

            if (packet.Header.Delivery == DeliveryMode.ReliableOrdered ||
                packet.Header.Delivery == DeliveryMode.ReliableUnordered)
            {
                _pendingAcks[packet.Header.SequenceNumber] = packet;
            }

            _sentCount++;
        }

        /// <summary>Processes incoming packet acknowledgments and returns payload if valid.</summary>
        public bool ProcessIncoming(Packet packet, out Packet result)
        {
            result = packet;

            // Process piggybacked ACKs
            ProcessAcks(packet.Header.AckSequence, packet.Header.AckBitfield);

            // Update remote sequence tracking
            if (IsNewer(packet.Header.SequenceNumber, _remoteSequence))
            {
                int diff = SequenceDiff(packet.Header.SequenceNumber, _remoteSequence);
                _ackBitfield <<= diff;
                _ackBitfield |= 1;
                _remoteSequence = packet.Header.SequenceNumber;
            }
            else
            {
                int diff = SequenceDiff(_remoteSequence, packet.Header.SequenceNumber);
                if (diff < 32) _ackBitfield |= (1u << diff);
            }

            // Duplicate detection
            if (_receivedSequences.Contains(packet.Header.SequenceNumber))
                return false;

            _receivedSequences.Add(packet.Header.SequenceNumber);

            // Ordered delivery queue
            if (packet.Header.Delivery == DeliveryMode.ReliableOrdered)
            {
                if (packet.Header.SequenceNumber == _nextExpectedSequence)
                {
                    _nextExpectedSequence++;
                    return true;
                }
                else
                {
                    _orderedQueue.Enqueue(packet);
                    return false;
                }
            }

            // Sequenced: discard old packets
            if (packet.Header.Delivery == DeliveryMode.UnreliableSequenced)
            {
                if (!IsNewer(packet.Header.SequenceNumber, _remoteSequence))
                    return false;
            }

            return true;
        }

        /// <summary>Updates retransmission timers and resends timed-out reliable packets.</summary>
        public List<Packet> Update(float deltaTime)
        {
            var retransmits = new List<Packet>();

            var timedOut = new List<ushort>();
            foreach (var kvp in _pendingAcks)
            {
                kvp.Value.RetransmitTimer += deltaTime;
                if (kvp.Value.RetransmitTimer >= RetransmitInterval)
                {
                    kvp.Value.RetransmitTimer = 0;
                    kvp.Value.SendAttempts++;

                    if (kvp.Value.SendAttempts >= MaxRetransmits)
                    {
                        timedOut.Add(kvp.Key);
                        _lostCount++;
                    }
                    else
                    {
                        retransmits.Add(kvp.Value);
                    }
                }
            }

            foreach (var seq in timedOut)
                _pendingAcks.Remove(seq);

            // Update packet loss metric
            if (_sentCount > 0)
                PacketLoss = (float)_lostCount / _sentCount;

            // Flush ordered queue
            while (_orderedQueue.Count > 0 && _orderedQueue.Peek().Header.SequenceNumber == _nextExpectedSequence)
            {
                _orderedQueue.Dequeue();
                _nextExpectedSequence++;
            }

            return retransmits;
        }

        private void ProcessAcks(ushort ackSeq, uint ackBits)
        {
            if (_pendingAcks.Remove(ackSeq))
            {
                // RTT measurement: track time since packet was sent
            }

            for (int i = 0; i < 32; i++)
            {
                if ((ackBits & (1u << i)) != 0)
                {
                    ushort seq = (ushort)(ackSeq - i - 1);
                    _pendingAcks.Remove(seq);
                }
            }
        }

        private static bool IsNewer(ushort s1, ushort s2)
        {
            return ((s1 > s2) && (s1 - s2 <= 32768)) ||
                   ((s1 < s2) && (s2 - s1 > 32768));
        }

        private static int SequenceDiff(ushort newer, ushort older)
        {
            if (newer >= older) return newer - older;
            return 65536 + newer - older;
        }
    }

    /// <summary>
    /// Represents a connected client/peer with network state.
    /// </summary>
    public class NetworkPeer
    {
        /// <summary>Unique peer identifier.</summary>
        public int PeerId { get; set; }

        /// <summary>Remote endpoint address.</summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>Player display name.</summary>
        public string PlayerName { get; set; }

        /// <summary>Connection state.</summary>
        public PeerState State { get; set; } = PeerState.Connecting;

        /// <summary>Reliable delivery channel.</summary>
        public ReliableChannel Channel { get; } = new ReliableChannel();

        /// <summary>Time since last received packet.</summary>
        public float TimeSinceLastPacket { get; set; }

        /// <summary>Current latency in milliseconds.</summary>
        public float LatencyMs { get; set; }

        /// <summary>Outgoing packet queue.</summary>
        public Queue<Packet> OutgoingQueue { get; } = new Queue<Packet>();

        /// <summary>Incoming packet queue for processing.</summary>
        public Queue<Packet> IncomingQueue { get; } = new Queue<Packet>();

        /// <summary>Whether this peer has timed out (no packets for too long).</summary>
        public bool IsTimedOut => TimeSinceLastPacket > TimeoutSeconds;

        /// <summary>Timeout threshold in seconds.</summary>
        public const float TimeoutSeconds = 15f;

        /// <summary>Heartbeat interval in seconds.</summary>
        public const float HeartbeatInterval = 2f;

        private float _heartbeatTimer;

        /// <summary>Updates peer timers and returns true if a heartbeat should be sent.</summary>
        public bool Update(float deltaTime)
        {
            TimeSinceLastPacket += deltaTime;
            _heartbeatTimer += deltaTime;

            if (_heartbeatTimer >= HeartbeatInterval)
            {
                _heartbeatTimer = 0;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Connection states for network peers.
    /// </summary>
    public enum PeerState
    {
        Connecting,
        Authenticating,
        Connected,
        Disconnecting,
        Disconnected,
        TimedOut,
        Banned
    }

    /// <summary>
    /// Entity state snapshot for network synchronization.
    /// Contains interpolation data for smooth remote entity rendering.
    /// </summary>
    public class NetworkEntityState
    {
        public int EntityId;
        public Math.Vector3F Position;
        public Math.QuaternionF Rotation;
        public Math.Vector3F Velocity;
        public float Timestamp;

        // Interpolation buffer
        private readonly Queue<NetworkEntityState> _stateBuffer = new();
        private const int MaxBufferSize = 30;
        private const float InterpolationDelay = 0.1f; // 100ms

        /// <summary>Adds a received state to the interpolation buffer.</summary>
        public void AddState(NetworkEntityState state)
        {
            _stateBuffer.Enqueue(state);
            while (_stateBuffer.Count > MaxBufferSize)
                _stateBuffer.Dequeue();
        }

        /// <summary>
        /// Interpolates between buffered states to produce smooth movement.
        /// Uses a configurable interpolation delay to handle network jitter.
        /// </summary>
        public (Math.Vector3F Position, Math.QuaternionF Rotation) Interpolate(float currentTime)
        {
            float renderTime = currentTime - InterpolationDelay;

            var states = _stateBuffer.ToArray();
            if (states.Length < 2)
                return (Position, Rotation);

            // Find the two states surrounding our render time
            for (int i = 0; i < states.Length - 1; i++)
            {
                if (states[i].Timestamp <= renderTime && states[i + 1].Timestamp >= renderTime)
                {
                    float t = (renderTime - states[i].Timestamp) /
                              (states[i + 1].Timestamp - states[i].Timestamp);
                    t = Math.MathUtils.Clamp01(t);

                    var pos = Math.Vector3F.Lerp(states[i].Position, states[i + 1].Position, t);
                    var rot = Math.QuaternionF.Slerp(states[i].Rotation, states[i + 1].Rotation, t);
                    return (pos, rot);
                }
            }

            // If no matching states, extrapolate from the latest
            var latest = states[^1];
            float timeSince = currentTime - latest.Timestamp;
            var extrapolatedPos = latest.Position + latest.Velocity * timeSince;
            return (extrapolatedPos, latest.Rotation);
        }
    }

    /// <summary>
    /// Lag compensation system for hit detection on the server.
    /// Stores a history of world states that can be rewound for accurate
    /// hit validation despite network latency.
    /// </summary>
    public class LagCompensation
    {
        /// <summary>A snapshot of entity positions at a specific server time.</summary>
        public class WorldSnapshot
        {
            public float ServerTime;
            public Dictionary<int, (Math.Vector3F Position, float Radius)> EntityPositions = new();
        }

        private readonly Queue<WorldSnapshot> _history = new();
        private const int MaxHistory = 64; // ~1 second at 60fps
        private const float MaxRewindTime = 0.5f; // Maximum rewind window

        /// <summary>Records the current world state for lag compensation.</summary>
        public void RecordSnapshot(float serverTime, Dictionary<int, (Math.Vector3F Position, float Radius)> entities)
        {
            var snapshot = new WorldSnapshot
            {
                ServerTime = serverTime,
                EntityPositions = new Dictionary<int, (Math.Vector3F, float)>(entities)
            };

            _history.Enqueue(snapshot);
            while (_history.Count > MaxHistory)
                _history.Dequeue();
        }

        /// <summary>
        /// Performs a lag-compensated hit test by rewinding world state to the client's
        /// perceived time and checking for ray-entity intersections.
        /// </summary>
        /// <param name="clientTime">The server time when the client fired (adjusted for RTT).</param>
        /// <param name="rayOrigin">Ray origin in world space.</param>
        /// <param name="rayDirection">Normalized ray direction.</param>
        /// <param name="maxDistance">Maximum ray distance.</param>
        /// <returns>Entity ID of the hit target, or -1 if no hit.</returns>
        public int RewindHitTest(float clientTime, Math.Vector3F rayOrigin, Math.Vector3F rayDirection, float maxDistance)
        {
            // Find the closest snapshot to the client's perceived time
            WorldSnapshot bestSnapshot = null;
            float bestTimeDiff = float.MaxValue;

            foreach (var snapshot in _history)
            {
                float diff = System.Math.Abs(snapshot.ServerTime - clientTime);
                if (diff < bestTimeDiff && diff <= MaxRewindTime)
                {
                    bestTimeDiff = diff;
                    bestSnapshot = snapshot;
                }
            }

            if (bestSnapshot == null) return -1;

            // Test ray against entity positions at that time
            int hitEntityId = -1;
            float closestHit = maxDistance;

            foreach (var kvp in bestSnapshot.EntityPositions)
            {
                if (Math.MathUtils.RaySphereIntersect(rayOrigin, rayDirection,
                    kvp.Value.Position, kvp.Value.Radius, out float hitDist))
                {
                    if (hitDist < closestHit)
                    {
                        closestHit = hitDist;
                        hitEntityId = kvp.Key;
                    }
                }
            }

            return hitEntityId;
        }
    }

    /// <summary>
    /// Network statistics tracker for monitoring connection quality.
    /// </summary>
    public class NetworkStats
    {
        private readonly Queue<(DateTime Time, int Bytes)> _sentHistory = new();
        private readonly Queue<(DateTime Time, int Bytes)> _receivedHistory = new();
        private readonly TimeSpan _windowSize = TimeSpan.FromSeconds(1);

        public long TotalBytesSent { get; private set; }
        public long TotalBytesReceived { get; private set; }
        public int TotalPacketsSent { get; private set; }
        public int TotalPacketsReceived { get; private set; }
        public float CurrentRTT { get; set; }
        public float AverageRTT { get; set; }
        public float PacketLoss { get; set; }

        /// <summary>Bandwidth usage in bytes per second (outgoing).</summary>
        public float OutgoingBandwidth
        {
            get
            {
                PruneOld(_sentHistory);
                return _sentHistory.Sum(x => x.Bytes);
            }
        }

        /// <summary>Bandwidth usage in bytes per second (incoming).</summary>
        public float IncomingBandwidth
        {
            get
            {
                PruneOld(_receivedHistory);
                return _receivedHistory.Sum(x => x.Bytes);
            }
        }

        public void RecordSent(int bytes)
        {
            TotalBytesSent += bytes;
            TotalPacketsSent++;
            _sentHistory.Enqueue((DateTime.UtcNow, bytes));
        }

        public void RecordReceived(int bytes)
        {
            TotalBytesReceived += bytes;
            TotalPacketsReceived++;
            _receivedHistory.Enqueue((DateTime.UtcNow, bytes));
        }

        private void PruneOld(Queue<(DateTime Time, int Bytes)> queue)
        {
            var cutoff = DateTime.UtcNow - _windowSize;
            while (queue.Count > 0 && queue.Peek().Time < cutoff)
                queue.Dequeue();
        }

        public override string ToString()
        {
            return $"NET: RTT={CurrentRTT:F1}ms | Loss={PacketLoss:P1} | " +
                   $"Out={OutgoingBandwidth/1024:F1}KB/s | In={IncomingBandwidth/1024:F1}KB/s | " +
                   $"Total: {TotalPacketsSent} sent / {TotalPacketsReceived} recv";
        }
    }

    /// <summary>
    /// Client-side prediction and server reconciliation system.
    /// Processes player input locally for responsive feel while
    /// correcting for authoritative server state.
    /// </summary>
    public class ClientPrediction
    {
        /// <summary>An input sample with server tick and movement data.</summary>
        public struct InputSample
        {
            public uint Tick;
            public Math.Vector3F MoveDirection;
            public float DeltaTime;
            public bool Jump;
            public bool Sprint;
        }

        private readonly Queue<InputSample> _pendingInputs = new();
        private Math.Vector3F _predictedPosition;
        private uint _lastServerTick;

        /// <summary>Current predicted position.</summary>
        public Math.Vector3F PredictedPosition => _predictedPosition;

        /// <summary>
        /// Records and applies a local input prediction.
        /// </summary>
        public void AddInput(InputSample input, float moveSpeed)
        {
            _pendingInputs.Enqueue(input);

            // Apply prediction locally
            float speed = input.Sprint ? moveSpeed * 1.5f : moveSpeed;
            _predictedPosition = _predictedPosition + input.MoveDirection * speed * input.DeltaTime;
        }

        /// <summary>
        /// Reconciles with authoritative server state.
        /// Discards acknowledged inputs and re-applies unacknowledged ones.
        /// </summary>
        public void Reconcile(Math.Vector3F serverPosition, uint serverTick, float moveSpeed)
        {
            _lastServerTick = serverTick;

            // Discard inputs that the server has already processed
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().Tick <= serverTick)
                _pendingInputs.Dequeue();

            // Start from server authoritative position
            _predictedPosition = serverPosition;

            // Re-apply unacknowledged inputs
            foreach (var input in _pendingInputs)
            {
                float speed = input.Sprint ? moveSpeed * 1.5f : moveSpeed;
                _predictedPosition = _predictedPosition + input.MoveDirection * speed * input.DeltaTime;
            }
        }
    }
}
