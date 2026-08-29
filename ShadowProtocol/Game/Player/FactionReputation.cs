using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Player
{
    public enum ReputationStanding
    {
        HostileOnSight,
        Unfriendly,
        Neutral,
        Friendly,
        HonoredAlly
    }

    public class FactionReputationState
    {
        public string FactionId { get; set; }
        public string FactionName { get; set; }
        public int ReputationScore { get; set; } = 0; // -1000 to +1000

        public ReputationStanding Standing
        {
            get
            {
                if (ReputationScore <= -500) return ReputationStanding.HostileOnSight;
                if (ReputationScore < -100) return ReputationStanding.Unfriendly;
                if (ReputationScore <= 200) return ReputationStanding.Neutral;
                if (ReputationScore <= 650) return ReputationStanding.Friendly;
                return ReputationStanding.HonoredAlly;
            }
        }
    }

    public class FactionReputationManager
    {
        private readonly Dictionary<string, FactionReputationState> _factions = new Dictionary<string, FactionReputationState>();

        public FactionReputationManager()
        {
            InitializeFactions();
        }

        private void InitializeFactions()
        {
            RegisterFaction("faction_directorate", "The Directorate", -600);
            RegisterFaction("faction_enclave7", "Enclave 7 Resistance", 250);
            RegisterFaction("faction_iron_vanguard", "Iron Vanguard Mercenaries", 50);
            RegisterFaction("faction_echo_syndicate", "Echo Syndicate Smugglers", 0);
            RegisterFaction("faction_bio_synth", "BioSynthetic Cult", -150);
        }

        public void RegisterFaction(string id, string name, int initialScore = 0)
        {
            _factions[id] = new FactionReputationState
            {
                FactionId = id,
                FactionName = name,
                ReputationScore = Math.Clamp(initialScore, -1000, 1000)
            };
        }

        public void AdjustReputation(string factionId, int delta)
        {
            if (_factions.TryGetValue(factionId, out var state))
            {
                state.ReputationScore = Math.Clamp(state.ReputationScore + delta, -1000, 1000);
            }
        }

        public ReputationStanding GetStanding(string factionId)
        {
            return _factions.TryGetValue(factionId, out var state) ? state.Standing : ReputationStanding.Neutral;
        }

        public float GetVendorDiscountMultiplier(string factionId)
        {
            var standing = GetStanding(factionId);
            return standing switch
            {
                ReputationStanding.HonoredAlly => 0.75f,
                ReputationStanding.Friendly => 0.90f,
                ReputationStanding.Neutral => 1.0f,
                ReputationStanding.Unfriendly => 1.25f,
                _ => 2.0f
            };
        }
    }
}
