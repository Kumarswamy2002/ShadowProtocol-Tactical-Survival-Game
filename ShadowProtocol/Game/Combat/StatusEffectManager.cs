using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Combat
{
    public enum StatusEffectCategory
    {
        Debuff,
        Buff,
        EnvironmentalHazard,
        CombatStance
    }

    public class StatusEffectInstance
    {
        public string EffectId { get; set; }
        public string DisplayName { get; set; }
        public StatusEffectCategory Category { get; set; }
        public float DurationRemainingSeconds { get; set; }
        public float TotalDurationSeconds { get; set; }
        public int CurrentStacks { get; set; }
        public int MaxStacks { get; set; }
        public float TickIntervalSeconds { get; set; }
        public float TimeSinceLastTick { get; set; }
        public float Magnitude { get; set; }

        public Action<StatusEffectInstance> OnTickAction { get; set; }
        public Action<StatusEffectInstance> OnApplyAction { get; set; }
        public Action<StatusEffectInstance> OnExpireAction { get; set; }
    }

    public class StatusEffectManager
    {
        private readonly Dictionary<string, StatusEffectInstance> _activeEffects = new Dictionary<string, StatusEffectInstance>();
        private readonly List<string> _expiredKeys = new List<string>();

        public IReadOnlyDictionary<string, StatusEffectInstance> ActiveEffects => _activeEffects;

        public void ApplyEffect(StatusEffectInstance effect)
        {
            if (_activeEffects.TryGetValue(effect.EffectId, out var existing))
            {
                // Refresh duration and increment stack
                existing.DurationRemainingSeconds = Math.Max(existing.DurationRemainingSeconds, effect.TotalDurationSeconds);
                existing.CurrentStacks = Math.Min(existing.MaxStacks, existing.CurrentStacks + 1);
                existing.Magnitude = Math.Max(existing.Magnitude, effect.Magnitude);
            }
            else
            {
                _activeEffects[effect.EffectId] = effect;
                effect.OnApplyAction?.Invoke(effect);
            }
        }

        public bool RemoveEffect(string effectId)
        {
            if (_activeEffects.TryGetValue(effectId, out var effect))
            {
                effect.OnExpireAction?.Invoke(effect);
                return _activeEffects.Remove(effectId);
            }
            return false;
        }

        public void Update(float deltaTime)
        {
            _expiredKeys.Clear();

            foreach (var kvp in _activeEffects)
            {
                var effect = kvp.Value;
                effect.DurationRemainingSeconds -= deltaTime;
                effect.TimeSinceLastTick += deltaTime;

                if (effect.TickIntervalSeconds > 0f && effect.TimeSinceLastTick >= effect.TickIntervalSeconds)
                {
                    effect.TimeSinceLastTick -= effect.TickIntervalSeconds;
                    effect.OnTickAction?.Invoke(effect);
                }

                if (effect.DurationRemainingSeconds <= 0f)
                {
                    _expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in _expiredKeys)
            {
                if (_activeEffects.TryGetValue(key, out var effect))
                {
                    effect.OnExpireAction?.Invoke(effect);
                    _activeEffects.Remove(key);
                }
            }
        }

        public bool HasEffect(string effectId) => _activeEffects.ContainsKey(effectId);

        public void ClearAll()
        {
            foreach (var effect in _activeEffects.Values)
            {
                effect.OnExpireAction?.Invoke(effect);
            }
            _activeEffects.Clear();
        }

        public static StatusEffectInstance CreateBleedEffect(float dps, float duration)
        {
            return new StatusEffectInstance
            {
                EffectId = "bleed",
                DisplayName = "Arterial Bleeding",
                Category = StatusEffectCategory.Debuff,
                TotalDurationSeconds = duration,
                DurationRemainingSeconds = duration,
                CurrentStacks = 1,
                MaxStacks = 5,
                TickIntervalSeconds = 1.0f,
                Magnitude = dps
            };
        }

        public static StatusEffectInstance CreateAdrenalineBuff(float speedBoost, float duration)
        {
            return new StatusEffectInstance
            {
                EffectId = "adrenaline_rush",
                DisplayName = "Combat Adrenaline",
                Category = StatusEffectCategory.Buff,
                TotalDurationSeconds = duration,
                DurationRemainingSeconds = duration,
                CurrentStacks = 1,
                MaxStacks = 1,
                TickIntervalSeconds = 0f,
                Magnitude = speedBoost
            };
        }
    }
}
