using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Combat;

public class BallisticsSimulator
{
    public float CurrentSpread { get; private set; } = 0.0f;
    public float CurrentRecoilPitch { get; private set; } = 0.0f;
    public float CurrentRecoilYaw { get; private set; } = 0.0f;

    public void ApplyRecoil(float recoilIntensity, float recoilStability)
    {
        float stabilityFactor = Math.Clamp(1.0f - recoilStability, 0.1f, 1.0f);
        CurrentRecoilPitch += recoilIntensity * stabilityFactor * (0.8f + ((float)Random.Shared.NextDouble() * 0.4f));
        CurrentRecoilYaw += (recoilIntensity * 0.4f * stabilityFactor) * ((float)(Random.Shared.NextDouble() * 2.0 - 1.0));
        CurrentSpread += recoilIntensity * 0.02f;
    }

    public void UpdateRecovery(float deltaTime, float recoverySpeed = 5.0f)
    {
        CurrentRecoilPitch = Math.Max(0f, CurrentRecoilPitch - (CurrentRecoilPitch * recoverySpeed * deltaTime));
        CurrentRecoilYaw -= CurrentRecoilYaw * recoverySpeed * deltaTime;
        CurrentSpread = Math.Max(0f, CurrentSpread - (CurrentSpread * recoverySpeed * 1.5f * deltaTime));
    }

    public Vector3F CalculateBulletDirection(Vector3F aimDirection, float baseAccuracy)
    {
        float totalSpread = Math.Max(0.001f, (1.0f - baseAccuracy) * 0.05f + CurrentSpread);

        float spreadX = ((float)Random.Shared.NextDouble() * 2f - 1f) * totalSpread;
        float spreadY = ((float)Random.Shared.NextDouble() * 2f - 1f) * totalSpread;

        Vector3F perturbed = new(
            aimDirection.X + spreadX + (CurrentRecoilYaw * 0.01f),
            aimDirection.Y + spreadY + (CurrentRecoilPitch * 0.01f),
            aimDirection.Z
        );

        return perturbed.Normalized;
    }
}
