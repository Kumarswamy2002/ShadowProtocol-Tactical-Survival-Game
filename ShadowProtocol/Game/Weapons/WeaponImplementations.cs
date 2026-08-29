namespace ShadowProtocol.Game.Weapons;

public static class WeaponFactory
{
    public static Firearm CreatePistol() => new()
    {
        Name = "V-9 Tactical Sidearm",
        Weight = 1.1f,
        BaseDamage = 28f,
        MaxRange = 35f,
        Accuracy = 0.88f,
        ArmorPenetration = 0.2f,
        AmmoType = AmmoType.Pistol9mm,
        MagazineCapacity = 15,
        CurrentAmmo = 15,
        FireRateRoundsPerMin = 400f,
        ReloadTimeSeconds = 1.4f,
        RecoilIntensity = 0.7f,
        NoiseLevel = 35f
    };

    public static Firearm CreateSMG() => new()
    {
        Name = "Spectre-45 Submachine Gun",
        Weight = 2.4f,
        BaseDamage = 22f,
        MaxRange = 45f,
        Accuracy = 0.82f,
        ArmorPenetration = 0.25f,
        AmmoType = AmmoType.LightSMG45,
        MagazineCapacity = 30,
        CurrentAmmo = 30,
        FireRateRoundsPerMin = 750f,
        ReloadTimeSeconds = 1.9f,
        RecoilIntensity = 1.1f,
        NoiseLevel = 45f
    };

    public static Firearm CreateShotgun() => new()
    {
        Name = "Breacher-12 Combat Shotgun",
        Weight = 3.6f,
        BaseDamage = 110f, // Pellet cluster total
        MaxRange = 20f,
        Accuracy = 0.65f,
        ArmorPenetration = 0.35f,
        AmmoType = AmmoType.Shotgun12Gauge,
        MagazineCapacity = 8,
        CurrentAmmo = 8,
        FireRateRoundsPerMin = 90f,
        ReloadTimeSeconds = 2.8f,
        RecoilIntensity = 2.5f,
        NoiseLevel = 70f
    };

    public static Firearm CreateAssaultRifle() => new()
    {
        Name = "AR-556 Directorate Service Rifle",
        Weight = 3.2f,
        BaseDamage = 36f,
        MaxRange = 75f,
        Accuracy = 0.86f,
        ArmorPenetration = 0.45f,
        AmmoType = AmmoType.Rifle556,
        MagazineCapacity = 30,
        CurrentAmmo = 30,
        FireRateRoundsPerMin = 650f,
        ReloadTimeSeconds = 2.1f,
        RecoilIntensity = 1.4f,
        NoiseLevel = 60f
    };

    public static Firearm CreateSniperRifle() => new()
    {
        Name = "Ghost-762 Marksman Rifle",
        Weight = 4.8f,
        BaseDamage = 95f,
        MaxRange = 180f,
        Accuracy = 0.96f,
        ArmorPenetration = 0.75f,
        AmmoType = AmmoType.Sniper762,
        MagazineCapacity = 5,
        CurrentAmmo = 5,
        FireRateRoundsPerMin = 45f,
        ReloadTimeSeconds = 3.0f,
        RecoilIntensity = 3.2f,
        NoiseLevel = 85f
    };

    public static MeleeWeapon CreateCombatKnife() => new()
    {
        Name = "Tanto Tactical Blade",
        Weight = 0.4f,
        BaseDamage = 35f,
        AttackSpeed = 1.6f,
        StaminaCostPerSwing = 10f,
        BleedChance = 0.4f
    };
}

public class WeaponInventory
{
    public Firearm? PrimaryWeapon { get; set; }
    public Firearm? SecondaryWeapon { get; set; }
    public Firearm? Sidearm { get; set; }
    public MeleeWeapon? Melee { get; set; }

    public WeaponBase? ActiveWeapon { get; private set; }

    private readonly Dictionary<AmmoType, int> _ammoReserves = new();

    public WeaponInventory()
    {
        // Initialize standard reserve slots
        foreach (var ammo in Enum.GetValues<AmmoType>())
        {
            _ammoReserves[ammo] = 0;
        }
    }

    public void AddAmmo(AmmoType type, int count)
    {
        if (count <= 0 || type == AmmoType.None) return;
        _ammoReserves[type] = _ammoReserves.GetValueOrDefault(type, 0) + count;
    }

    public int GetAmmoCount(AmmoType type) => _ammoReserves.GetValueOrDefault(type, 0);

    public bool ConsumeAmmo(AmmoType type, int count)
    {
        if (type == AmmoType.None) return true;
        int current = GetAmmoCount(type);
        if (current < count) return false;
        _ammoReserves[type] = current - count;
        return true;
    }

    public void EquipPrimary(Firearm weapon)
    {
        PrimaryWeapon = weapon;
        ActiveWeapon ??= weapon;
    }

    public void EquipSecondary(Firearm weapon)
    {
        SecondaryWeapon = weapon;
    }

    public void EquipSidearm(Firearm weapon)
    {
        Sidearm = weapon;
    }

    public void EquipMelee(MeleeWeapon weapon)
    {
        Melee = weapon;
        ActiveWeapon ??= weapon;
    }

    public void SelectPrimary() => ActiveWeapon = PrimaryWeapon ?? ActiveWeapon;
    public void SelectSecondary() => ActiveWeapon = SecondaryWeapon ?? ActiveWeapon;
    public void SelectSidearm() => ActiveWeapon = Sidearm ?? ActiveWeapon;
    public void SelectMelee() => ActiveWeapon = Melee ?? ActiveWeapon;
}
