using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.World
{
    public enum WeatherType
    {
        ClearSkies,
        Overcast,
        AcidRain,
        ToxicFog,
        ElectricalStorm,
        AshFallout,
        Blizzard
    }

    public class WeatherState
    {
        public WeatherType CurrentWeather { get; set; } = WeatherType.ClearSkies;
        public float TransitionProgress { get; set; } = 1.0f;
        public float PrecipitationRate { get; set; } = 0.0f;
        public float WindSpeedMs { get; set; } = 3.5f;
        public Vector3F WindDirection { get; set; } = new Vector3F(1.0f, 0f, 0f);
        public float ToxicityPpm { get; set; } = 0.0f;
        public float LightningFrequencyHertz { get; set; } = 0.0f;
    }

    public class DynamicWeatherEngine
    {
        private WeatherState _state = new WeatherState();
        private float _timeUntilNextWeatherChange = 600f; // 10 minutes
        private static readonly Random _rng = new Random(1337);

        public WeatherState State => _state;

        public void Update(float deltaTime)
        {
            _timeUntilNextWeatherChange -= deltaTime;
            if (_timeUntilNextWeatherChange <= 0f)
            {
                _timeUntilNextWeatherChange = 450f + (float)(_rng.NextDouble() * 300f);
                TriggerRandomWeather();
            }

            // Fluctuations in wind
            _state.WindSpeedMs = Math.Clamp(_state.WindSpeedMs + ((float)(_rng.NextDouble() - 0.5) * deltaTime * 0.5f), 0.5f, 28f);
        }

        private void TriggerRandomWeather()
        {
            Array values = Enum.GetValues(typeof(WeatherType));
            WeatherType next = (WeatherType)values.GetValue(_rng.Next(values.Length));
            SetWeather(next);
        }

        public void SetWeather(WeatherType type)
        {
            _state.CurrentWeather = type;
            switch (type)
            {
                case WeatherType.ClearSkies:
                    _state.PrecipitationRate = 0.0f;
                    _state.ToxicityPpm = 0.0f;
                    _state.LightningFrequencyHertz = 0.0f;
                    break;
                case WeatherType.AcidRain:
                    _state.PrecipitationRate = 0.85f;
                    _state.ToxicityPpm = 45.0f;
                    _state.LightningFrequencyHertz = 0.02f;
                    break;
                case WeatherType.ToxicFog:
                    _state.PrecipitationRate = 0.1f;
                    _state.ToxicityPpm = 120.0f;
                    _state.LightningFrequencyHertz = 0.0f;
                    break;
                case WeatherType.ElectricalStorm:
                    _state.PrecipitationRate = 0.95f;
                    _state.ToxicityPpm = 10.0f;
                    _state.LightningFrequencyHertz = 0.15f;
                    break;
                default:
                    _state.PrecipitationRate = 0.2f;
                    _state.ToxicityPpm = 5.0f;
                    break;
            }
        }
    }
}
