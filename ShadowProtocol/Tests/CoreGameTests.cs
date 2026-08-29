using Xunit;
using ShadowProtocol.AI.CombatAI;
using ShadowProtocol.AI.Perception;
using ShadowProtocol.AI.SquadAI;
using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Combat;
using ShadowProtocol.Game.Crafting;
using ShadowProtocol.Game.Dialogue;
using ShadowProtocol.Game.Economy;
using ShadowProtocol.Game.Inventory;
using ShadowProtocol.Game.Missions;
using ShadowProtocol.Game.NPC;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.SaveSystem;
using ShadowProtocol.Game.Skills;
using ShadowProtocol.Game.Vehicles;
using ShadowProtocol.Game.Weapons;
using ShadowProtocol.Game.World;

namespace ShadowProtocol.Tests;

public class PlayerSystemTests
{
    [Fact]
    public void PlayerVitals_ApplyDamage_ReducesArmorAndHealthCorrectly()
    {
        var vitals = new PlayerVitals();
        float initialHealth = vitals.Health;
        float initialArmor = vitals.Armor;

        vitals.ApplyDamage(40f, armorPenetration: 0.2f);

        Assert.True(vitals.Health < initialHealth);
        Assert.True(vitals.Armor < initialArmor);
        Assert.False(vitals.IsDead);
    }

    [Fact]
    public void PlayerVitals_SurvivalTick_DepletesHungerAndHydration()
    {
        var vitals = new PlayerVitals();
        float startHunger = vitals.Hunger;
        float startHydration = vitals.Hydration;

        vitals.UpdateSurvivalTick(10.0f, isExerting: true);

        Assert.True(vitals.Hunger < startHunger);
        Assert.True(vitals.Hydration < startHydration);
    }

    [Fact]
    public void PlayerAttributes_AddExperience_TriggersLevelUp()
    {
        var eventBus = new EventBus();
        bool levelUpFired = false;
        eventBus.Subscribe<LevelUpEvent>(e => levelUpFired = true);

        var attributes = new PlayerAttributes(eventBus);
        Assert.Equal(1, attributes.Level);

        attributes.AddExperience(150);

        Assert.Equal(2, attributes.Level);
        Assert.Equal(2, attributes.AvailableSkillPoints);
        Assert.True(levelUpFired);
    }

    [Fact]
    public void PlayerController_SprintInput_ChangesStateAndDrainsStamina()
    {
        var controller = new PlayerController();
        controller.HandleMovementInput(Vector3F.Forward, wantsToSprint: true, wantsToCrouch: false, wantsStealth: false);

        Assert.IsType<SprintingState>(controller.StateMachine.CurrentState);

        float initialStamina = controller.Vitals.Stamina;
        controller.Update(1.0f);

        Assert.True(controller.Vitals.Stamina < initialStamina);
    }
}

public class CombatAndWeaponTests
{
    [Fact]
    public void DamageCalculator_Headshot_AppliesHeadshotMultiplier()
    {
        float bodyDamage = DamageCalculator.CalculateDamage(
            baseDamage: 50f,
            maxRange: 50f,
            actualDistance: 10f,
            hitZone: HitZone.Torso,
            armorPenetration: 0f,
            targetArmor: 0f,
            critChance: 0f
        );

        float headshotDamage = DamageCalculator.CalculateDamage(
            baseDamage: 50f,
            maxRange: 50f,
            actualDistance: 10f,
            hitZone: HitZone.Head,
            armorPenetration: 0f,
            targetArmor: 0f,
            critChance: 0f
        );

        Assert.Equal(50f, bodyDamage, 0.1f);
        Assert.Equal(125f, headshotDamage, 0.1f); // 2.5x headshot
    }

    [Fact]
    public void Firearm_Shooting_ConsumesAmmoAndDegradesDurability()
    {
        var pistol = WeaponFactory.CreatePistol();
        int initialAmmo = pistol.CurrentAmmo;
        float initialDurability = pistol.CurrentDurability;

        bool shot = pistol.Shoot(1.0f, Vector3F.Zero, Vector3F.Forward);

        Assert.True(shot);
        Assert.Equal(initialAmmo - 1, pistol.CurrentAmmo);
        Assert.True(pistol.CurrentDurability < initialDurability);
    }

    [Fact]
    public void Firearm_Attachments_ModifyWeaponStats()
    {
        var rifle = WeaponFactory.CreateAssaultRifle();
        float baseDmg = rifle.EffectiveDamage;

        var suppressor = new WeaponAttachment
        {
            Slot = AttachmentSlot.Muzzle,
            DamageModifier = -2f,
            NoiseModifier = -25f
        };
        rifle.Attach(suppressor);

        Assert.Equal(baseDmg - 2f, rifle.EffectiveDamage);
        Assert.Equal(35f, rifle.EffectiveNoiseLevel);
    }
}

public class EnemyAITests
{
    [Fact]
    public void AIPerception_DetectsTargetInSightCone()
    {
        var perception = new AIPerception();
        Vector3F aiPos = Vector3F.Zero;
        Vector3F aiForward = Vector3F.Forward;
        Vector3F targetPos = new(0, 0, 15); // Directly in front at 15m

        perception.ProcessSight(aiPos, aiForward, targetPos, targetStealthFactor: 1.0f, hasLineOfSight: true, deltaTime: 2.0f);

        Assert.True(perception.Alertness > 50f);
        Assert.True(perception.HasTargetInSight);
    }

    [Fact]
    public void SquadManager_AssignsTacticalOrdersToMembers()
    {
        var squad = new SquadManager();
        var scout = new EnemyAIController(EnemyArchetype.Scout, Vector3F.Zero);
        var heavy = new EnemyAIController(EnemyArchetype.Heavy, Vector3F.Zero);

        squad.AddMember(scout);
        squad.AddMember(heavy);

        squad.CoordinateTactics(new Vector3F(0, 0, 30));

        var scoutMember = squad.Members.First(m => m.Role == EnemyArchetype.Scout);
        var heavyMember = squad.Members.First(m => m.Role == EnemyArchetype.Heavy);

        Assert.Equal(SquadOrder.Flank, scoutMember.CurrentOrder);
        Assert.Equal(SquadOrder.Attack, heavyMember.CurrentOrder);
    }
}

public class ProgressionAndEconomyTests
{
    [Fact]
    public void Inventory_AddItem_StacksAndRespectsCapacity()
    {
        var inv = new Inventory(maxSlots: 5, maxWeight: 20f);
        var ammo = new ItemData { Id = "ammo_9mm", Name = "9mm Ammo", MaxStack = 30, Weight = 0.01f };

        bool added1 = inv.AddItem(ammo, 25);
        bool added2 = inv.AddItem(ammo, 15);

        Assert.True(added1);
        Assert.True(added2);
        Assert.Equal(40, inv.GetTotalItemCount("ammo_9mm"));
    }

    [Fact]
    public void CraftingEngine_CraftsItemWhenIngredientsAreAvailable()
    {
        var engine = new CraftingEngine();
        var inv = new Inventory(maxSlots: 10, maxWeight: 50f);

        var scrap = new ItemData { Id = "scrap_metal", Name = "Scrap", MaxStack = 50 };
        var electronics = new ItemData { Id = "electronic_components", Name = "Electronics", MaxStack = 50 };

        inv.AddItem(scrap, 10);
        inv.AddItem(electronics, 5);

        bool crafted = engine.Craft("craft_weapon_parts", inv, CraftingStationType.Workbench);

        Assert.True(crafted);
        Assert.Equal(2, inv.GetTotalItemCount("weapon_parts"));
        Assert.Equal(7, inv.GetTotalItemCount("scrap_metal")); // 10 - 3
    }

    [Fact]
    public void SkillTree_UpgradesSkillAndAppliesBonuses()
    {
        var skillMgr = new SkillTreeManager();
        var attrs = new PlayerAttributes();
        var vitals = new PlayerVitals();
        attrs.AddSkillPoints(3);

        bool upgraded = skillMgr.UpgradeSkill("combat_weapon_damage", attrs, vitals);

        Assert.True(upgraded);
        Assert.Equal(1.10f, attrs.DamageMultiplier);
        Assert.Equal(2, attrs.AvailableSkillPoints);
    }

    [Fact]
    public void Economy_BuyAndSell_UpdatesBalancesCorrectly()
    {
        var shop = new MerchantShop("Outpost Trader");
        var medkit = new ItemData { Id = "medkit", Name = "Medkit", BasePrice = 100 };
        shop.InventoryStock.Add(new MerchantStockItem { Data = medkit, AvailableQuantity = 5 });

        var player = new PlayerAttributes(); // starting 500 currency
        var playerInv = new Inventory();

        bool bought = shop.BuyFromMerchant("medkit", 2, player, playerInv);

        Assert.True(bought);
        Assert.Equal(2, playerInv.GetTotalItemCount("medkit"));
        Assert.True(player.Currency < 500);
    }
}

public class SaveAndWorldTests
{
    [Fact]
    public void SaveManager_SnapshotSerialization_RoundTripsAccurately()
    {
        var saveMgr = new SaveManager();
        var player = new PlayerController();
        player.Attributes.AddCurrency(1000);
        player.Vitals.ModifyHealth(-20f);

        var inv = new Inventory();
        var skills = new SkillTreeManager();
        var missions = new MissionManager();
        var world = new WorldManager();

        var snapshot = saveMgr.CreateSnapshot(1, "TestSlot", player, inv, skills, missions, world);
        string json = saveMgr.SerializeSnapshot(snapshot);

        var deserialized = saveMgr.DeserializeSnapshot(json);

        Assert.NotNull(deserialized);
        Assert.Equal("TestSlot", deserialized.SaveName);
        Assert.Equal(1500, deserialized.Player.Currency); // 500 start + 1000
        Assert.Equal(80f, deserialized.Player.Health, 0.1f);
    }

    [Fact]
    public void VehicleController_Drive_ConsumesFuelAndMoves()
    {
        var vehicle = new VehicleController(VehicleType.Car, "Rover", Vector3F.Zero);
        vehicle.EnterVehicle();

        float startFuel = vehicle.CurrentFuel;
        vehicle.Drive(throttle: 1.0f, steering: 0f, deltaTime: 1.0f);

        Assert.True(vehicle.CurrentFuel < startFuel);
        Assert.True(vehicle.Position.Z > 0f || vehicle.Velocity.Magnitude > 0f);
    }
}
