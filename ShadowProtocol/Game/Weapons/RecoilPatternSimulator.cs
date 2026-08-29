using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Weapons
{
    public class RecoilKeyframe
    {
        public int ShotIndex { get; set; }
        public float VerticalKickDeg { get; set; }
        public float HorizontalKickDeg { get; set; }
        public float CameraSnapMultiplier { get; set; } = 1.0f;
    }

    public class RecoilPatternSimulator
    {
        private readonly List<RecoilKeyframe> _pattern = new List<RecoilKeyframe>();
        private int _currentContinuousShots = 0;
        private float _currentPitchOffset = 0f;
        private float _currentYawOffset = 0f;
        private float _recoverySpeed = 12.0f;

        public float CurrentPitch => _currentPitchOffset;
        public float CurrentYaw => _currentYawOffset;

        public RecoilPatternSimulator()
        {
            GenerateStandardAssaultPattern();
        }

        private void GenerateStandardAssaultPattern()
        {
            for (int i = 0; i < 30; i++)
            {
                float vertical = 1.5f + (float)Math.Sin(i * 0.2f) * 0.4f;
                float horizontal = (float)Math.Sin(i * 0.6f) * 0.8f;
                if (i > 10) horizontal += (i % 2 == 0 ? 0.4f : -0.4f);

                _pattern.Add(new RecoilKeyframe
                {
                    ShotIndex = i,
                    VerticalKickDeg = vertical,
                    HorizontalKickDeg = horizontal
                });
            }
        }

        public Vector3F TriggerShotRecoil(float recoilDampening = 1.0f)
        {
            int index = Math.Min(_currentContinuousShots, _pattern.Count - 1);
            var frame = _pattern[index];

            float pitchImpulse = frame.VerticalKickDeg * recoilDampening;
            float yawImpulse = frame.HorizontalKickDeg * recoilDampening;

            _currentPitchOffset += pitchImpulse;
            _currentYawOffset += yawImpulse;
            _currentContinuousShots++;

            return new Vector3F(pitchImpulse, yawImpulse, 0f);
        }

        public void UpdateRecovery(float deltaTime)
        {
            if (_currentPitchOffset > 0.001f)
            {
                _currentPitchOffset = Math.Max(0f, _currentPitchOffset - (_recoverySpeed * deltaTime * _currentPitchOffset));
            }
            if (Math.Abs(_currentYawOffset) > 0.001f)
            {
                _currentYawOffset = MathUtils.Lerp(_currentYawOffset, 0f, _recoverySpeed * deltaTime);
            }
            if (_currentPitchOffset < 0.01f && Math.Abs(_currentYawOffset) < 0.01f)
            {
                _currentContinuousShots = 0;
            }
        }
    }
}
