using ShadowProtocol.Core.Events;
using ShadowProtocol.Game.Combat;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.Weapons;

namespace ShadowProtocol.UI.HUD;

public class HUDViewModel
{
    public float HealthPercent { get; set; }
    public float StaminaPercent { get; set; }
    public float ArmorPercent { get; set; }
    public float HungerPercent { get; set; }
    public float HydrationPercent { get; set; }

    public string ActiveWeaponName { get; set; } = "Unarmed";
    public int CurrentAmmo { get; set; }
    public int ReserveAmmo { get; set; }
    public bool IsReloading { get; set; }

    public string CurrentObjectiveText { get; set; } = "Explore the ruins of Veyra";
    public string InteractionPrompt { get; set; } = string.Empty;
    public string CompassHeading { get; set; } = "N";
    public float StealthVulnerability { get; set; } = 0.0f; // 0 (hidden) to 1.0 (exposed)
}

public class HUDController
{
    public HUDViewModel ViewModel { get; } = new();

    public void UpdateHUD(
        PlayerVitals vitals,
        WeaponInventory weapons,
        string objectiveText,
        string interactionPrompt = "",
        float stealthExposure = 0.0f
    )
    {
        ViewModel.HealthPercent = vitals.Health / vitals.MaxHealth;
        ViewModel.StaminaPercent = vitals.Stamina / vitals.MaxStamina;
        ViewModel.ArmorPercent = vitals.Armor / vitals.MaxArmor;
        ViewModel.HungerPercent = vitals.Hunger / vitals.MaxHunger;
        ViewModel.HydrationPercent = vitals.Hydration / vitals.MaxHydration;

        if (weapons.ActiveWeapon is Firearm gun)
        {
            ViewModel.ActiveWeaponName = gun.Name;
            ViewModel.CurrentAmmo = gun.CurrentAmmo;
            ViewModel.ReserveAmmo = weapons.GetAmmoCount(gun.AmmoType);
            ViewModel.IsReloading = gun.IsReloading;
        }
        else if (weapons.ActiveWeapon is MeleeWeapon melee)
        {
            ViewModel.ActiveWeaponName = melee.Name;
            ViewModel.CurrentAmmo = 0;
            ViewModel.ReserveAmmo = 0;
            ViewModel.IsReloading = false;
        }
        else
        {
            ViewModel.ActiveWeaponName = "Holstered";
            ViewModel.CurrentAmmo = 0;
            ViewModel.ReserveAmmo = 0;
            ViewModel.IsReloading = false;
        }

        ViewModel.CurrentObjectiveText = objectiveText;
        ViewModel.InteractionPrompt = interactionPrompt;
        ViewModel.StealthVulnerability = stealthExposure;
    }
}
