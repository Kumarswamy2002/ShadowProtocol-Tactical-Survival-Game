using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Combat;

namespace ShadowProtocol.Game.Weapons;

public enum AmmoType
{
    Pistol9mm,
    LightSMG45,
    Shotgun12Gauge,
    Rifle556,
    Sniper762,
    EnergyCell,
    None
}

public enum AttachmentSlot
{
    Optic,
    Muzzle,
    Magazine,
    Underbarrel
}

public class WeaponAttachment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AttachmentSlot Slot { get; set; }
    public float DamageModifier { get; set; } = 0.0f;
    public float RangeModifier { get; set; } = 0.0f;
    public float RecoilModifier { get; set; } = 0.0f; // negative is better recoil control
    public float NoiseModifier { get; set; } = 0.0f;  // negative is quieter (suppressor)
    public int ExtraMagazineCapacity { get; set; } = 0;
    public float ZoomLevel { get; set; } = 1.0f;
}

public readonly record struct WeaponFiredEvent(
    string WeaponName,
    AmmoType AmmoType,
    int RemainingAmmo,
    Vector3F Origin,
    Vector3F Direction
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct WeaponReloadedEvent(string WeaponName, int AmmoLoaded, int CurrentTotalInMag) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public abstract class WeaponBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Weapon";
    public float Weight { get; set; } = 1.5f; // kg
    public float BaseDamage { get; set; } = 25f;
    public float MaxRange { get; set; } = 50f;
    public float Accuracy { get; set; } = 0.85f; // 0 to 1
    public float ArmorPenetration { get; set; } = 0.15f; // 0 to 1
    public float MaxDurability { get; set; } = 100f;
    public float CurrentDurability { get; set; } = 100f;

    public bool IsBroken => CurrentDurability <= 0f;

    public virtual void Repair(float amount)
    {
        CurrentDurability = Math.Clamp(CurrentDurability + amount, 0f, MaxDurability);
    }

    public virtual void Degrade(float amount)
    {
        CurrentDurability = Math.Max(0f, CurrentDurability - amount);
    }
}

public class Firearm : WeaponBase
{
    public AmmoType AmmoType { get; set; } = AmmoType.Pistol9mm;
    public int MagazineCapacity { get; set; } = 15;
    public int CurrentAmmo { get; set; } = 15;
    public float FireRateRoundsPerMin { get; set; } = 300f; // RPM
    public float ReloadTimeSeconds { get; set; } = 1.8f;
    public float RecoilIntensity { get; set; } = 1.0f;
    public float NoiseLevel { get; set; } = 50f; // sound radius

    public Dictionary<AttachmentSlot, WeaponAttachment> Attachments { get; } = new();

    public float ShotsPerSecond => FireRateRoundsPerMin / 60f;
    public float TimeBetweenShots => 1.0f / ShotsPerSecond;

    private float _lastShotTime = -100f;
    private bool _isReloading = false;
    private float _reloadTimer = 0f;

    public bool IsReloading => _isReloading;
    public int EffectiveMagazineCapacity => MagazineCapacity + (Attachments.TryGetValue(AttachmentSlot.Magazine, out var mag) ? mag.ExtraMagazineCapacity : 0);
    public float EffectiveNoiseLevel => Math.Max(5f, NoiseLevel + (Attachments.TryGetValue(AttachmentSlot.Muzzle, out var muz) ? muz.NoiseModifier : 0f));
    public float EffectiveDamage => BaseDamage + (Attachments.TryGetValue(AttachmentSlot.Muzzle, out var muz) ? muz.DamageModifier : 0f);
    public float EffectiveRange => MaxRange + (Attachments.TryGetValue(AttachmentSlot.Optic, out var opt) ? opt.RangeModifier : 0f);

    public bool CanShoot(float currentTime)
    {
        return !IsBroken && !_isReloading && CurrentAmmo > 0 && (currentTime - _lastShotTime >= TimeBetweenShots);
    }

    public bool Shoot(float currentTime, Vector3F origin, Vector3F direction, IEventBus? eventBus = null)
    {
        if (!CanShoot(currentTime)) return false;

        CurrentAmmo--;
        _lastShotTime = currentTime;
        Degrade(0.2f);

        eventBus?.Publish(new WeaponFiredEvent(Name, AmmoType, CurrentAmmo, origin, direction));
        return true;
    }

    public bool StartReload()
    {
        if (_isReloading || CurrentAmmo >= EffectiveMagazineCapacity || IsBroken) return false;
        _isReloading = true;
        _reloadTimer = 0f;
        return true;
    }

    public bool UpdateReload(float deltaTime, int availableReserveAmmo, out int ammoConsumed, IEventBus? eventBus = null)
    {
        ammoConsumed = 0;
        if (!_isReloading) return false;

        _reloadTimer += deltaTime;
        if (_reloadTimer >= ReloadTimeSeconds)
        {
            int needed = EffectiveMagazineCapacity - CurrentAmmo;
            int toLoad = Math.Min(needed, availableReserveAmmo);
            CurrentAmmo += toLoad;
            ammoConsumed = toLoad;
            _isReloading = false;

            eventBus?.Publish(new WeaponReloadedEvent(Name, toLoad, CurrentAmmo));
            return true;
        }

        return false;
    }

    public void CancelReload()
    {
        _isReloading = false;
        _reloadTimer = 0f;
    }

    public void Attach(WeaponAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        Attachments[attachment.Slot] = attachment;
    }

    public bool Detach(AttachmentSlot slot, out WeaponAttachment? detached)
    {
        return Attachments.Remove(slot, out detached);
    }
}

public class MeleeWeapon : WeaponBase
{
    public float AttackSpeed { get; set; } = 1.2f; // attacks per second
    public float StaminaCostPerSwing { get; set; } = 15f;
    public float BleedChance { get; set; } = 0.25f;

    public MeleeWeapon()
    {
        MaxRange = 2.2f;
        Accuracy = 1.0f;
        ArmorPenetration = 0.05f;
    }
}
