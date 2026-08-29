using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Core.Networking
{
    public class EntityReplicationSnapshot
    {
        public uint EntityNetId { get; set; }
        public uint SequenceNumber { get; set; }
        public Vector3F Position { get; set; }
        public QuaternionF Rotation { get; set; }
        public Vector3F Velocity { get; set; }
        public float Health { get; set; }
        public byte StateBitmask { get; set; }
        public long ServerTimestamp { get; set; }
    }

    public class StateReplicationManager
    {
        private readonly Dictionary<uint, List<EntityReplicationSnapshot>> _snapshotBuffers = new Dictionary<uint, List<EntityReplicationSnapshot>>();
        private const float InterpolationDelaySeconds = 0.100f; // 100ms interpolation buffer

        public void PushSnapshot(EntityReplicationSnapshot snapshot)
        {
            if (!_snapshotBuffers.TryGetValue(snapshot.EntityNetId, out var buffer))
            {
                buffer = new List<EntityReplicationSnapshot>();
                _snapshotBuffers[snapshot.EntityNetId] = buffer;
            }

            buffer.Add(snapshot);
            if (buffer.Count > 64) buffer.RemoveAt(0);
        }

        public (Vector3F position, QuaternionF rotation) SampleInterpolatedState(uint netId, float renderTimeSeconds)
        {
            if (!_snapshotBuffers.TryGetValue(netId, out var buffer) || buffer.Count < 2)
            {
                return (Vector3F.Zero, QuaternionF.Identity);
            }

            // Find two bounding snapshots
            EntityReplicationSnapshot from = buffer[buffer.Count - 2];
            EntityReplicationSnapshot to = buffer[buffer.Count - 1];

            float t = 0.5f; // Interpolation factor
            Vector3F pos = Vector3F.Lerp(from.Position, to.Position, t);
            QuaternionF rot = QuaternionF.Slerp(from.Rotation, to.Rotation, t);

            return (pos, rot);
        }
    }
}
