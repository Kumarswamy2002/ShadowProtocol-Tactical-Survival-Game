using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.World
{
    public class WorldSimulationEngine
    {
        public float TimeOfDayHours { get; set; } = 8.5f; // 0.0 to 24.0
        public float DayLengthMinutes { get; set; } = 48.0f; // 48 min real time = 24h game time
        public float AmbientTemperatureCelsius { get; set; } = 18.0f;
        public float FogDensityPercent { get; set; } = 0.05f;
        public float RadiationLevelMsv { get; set; } = 0.02f;
        public bool IsNight => TimeOfDayHours < 5.5f || TimeOfDayHours > 20.5f;

        public void Tick(float deltaTimeSeconds)
        {
            float hoursPerSecond = 24.0f / (DayLengthMinutes * 60.0f);
            TimeOfDayHours = (TimeOfDayHours + (hoursPerSecond * deltaTimeSeconds)) % 24.0f;

            // Temperature curve (peak at 14:00, coldest at 04:00)
            float tempCycle = (float)Math.Sin((TimeOfDayHours - 9.0f) * (Math.PI / 12.0));
            AmbientTemperatureCelsius = 15.0f + (tempCycle * 12.0f);
        }

        public Vector3F GetSunLightDirection()
        {
            float sunAngle = (TimeOfDayHours / 24.0f) * (float)(2.0 * Math.PI) - ((float)Math.PI / 2.0f);
            return new Vector3F((float)Math.Cos(sunAngle), (float)Math.Sin(sunAngle), 0.25f).Normalized();
        }

        public float GetAmbientLightIntensity()
        {
            if (TimeOfDayHours >= 6.0f && TimeOfDayHours <= 18.0f)
            {
                return 1.0f;
            }
            if (TimeOfDayHours > 18.0f && TimeOfDayHours <= 20.5f)
            {
                return MathUtils.Lerp(1.0f, 0.08f, (TimeOfDayHours - 18.0f) / 2.5f);
            }
            if (TimeOfDayHours >= 4.0f && TimeOfDayHours < 6.0f)
            {
                return MathUtils.Lerp(0.08f, 1.0f, (TimeOfDayHours - 4.0f) / 2.0f);
            }
            return 0.08f; // Night ambient starlight/moonlight
        }
    }
}
