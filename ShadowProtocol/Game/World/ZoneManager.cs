using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.World
{
    public class ZoneDefinition
    {
        public string ZoneId { get; set; }
        public string Name { get; set; }
        public int ThreatLevel { get; set; } // 1 to 10
        public string ControllingFaction { get; set; }
        public Vector3F CenterPosition { get; set; }
        public float RadiusMeters { get; set; }
        public float ResourceRichnessMultiplier { get; set; } = 1.0f;
        public bool IsRadiationHazardZone { get; set; }
    }

    public class ZoneManager
    {
        private readonly Dictionary<string, ZoneDefinition> _zones = new Dictionary<string, ZoneDefinition>();

        public ZoneManager()
        {
            InitializeZones();
        }

        private void InitializeZones()
        {
            RegisterZone(new ZoneDefinition
            {
                ZoneId = "zone_spire_central",
                Name = "The Directorate Spire",
                ThreatLevel = 9,
                ControllingFaction = "The Directorate",
                CenterPosition = new Vector3F(0f, 50f, 0f),
                RadiusMeters = 800f,
                ResourceRichnessMultiplier = 2.5f
            });

            RegisterZone(new ZoneDefinition
            {
                ZoneId = "zone_ruins_industrial",
                Name = "Sector 4 Industrial Ruins",
                ThreatLevel = 5,
                ControllingFaction = "Iron Vanguard",
                CenterPosition = new Vector3F(1200f, 10f, 400f),
                RadiusMeters = 650f,
                ResourceRichnessMultiplier = 1.4f
            });

            RegisterZone(new ZoneDefinition
            {
                ZoneId = "zone_sunken_district",
                Name = "Old Veyra Sunken District",
                ThreatLevel = 7,
                ControllingFaction = "Echo Syndicate",
                CenterPosition = new Vector3F(-900f, -15f, 1100f),
                RadiusMeters = 750f,
                ResourceRichnessMultiplier = 1.8f,
                IsRadiationHazardZone = true
            });

            RegisterZone(new ZoneDefinition
            {
                ZoneId = "zone_enclave_safehaven",
                Name = "Enclave 7 Sanctuary",
                ThreatLevel = 1,
                ControllingFaction = "Enclave 7",
                CenterPosition = new Vector3F(-1500f, 25f, -800f),
                RadiusMeters = 500f,
                ResourceRichnessMultiplier = 0.8f
            });
        }

        public void RegisterZone(ZoneDefinition zone)
        {
            _zones[zone.ZoneId] = zone;
        }

        public ZoneDefinition GetZoneAtPosition(Vector3F position)
        {
            foreach (var zone in _zones.Values)
            {
                float dist = (position - zone.CenterPosition).Magnitude();
                if (dist <= zone.RadiusMeters)
                {
                    return zone;
                }
            }
            return null; // Wild outlands
        }

        public IEnumerable<ZoneDefinition> GetAllZones() => _zones.Values;
    }
}
