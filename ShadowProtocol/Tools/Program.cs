using System.Text.Json;
using ShadowProtocol.AI.CombatAI;
using ShadowProtocol.AI.SquadAI;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Combat;
using ShadowProtocol.Game.Dialogue;
using ShadowProtocol.Game.Inventory;
using ShadowProtocol.Game.Missions;
using ShadowProtocol.Game.NPC;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.SaveSystem;
using ShadowProtocol.Game.Skills;
using ShadowProtocol.Game.Weapons;
using ShadowProtocol.Game.World;

namespace ShadowProtocol.Tools;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("    SHADOW PROTOCOL - DEVELOPER TOOLS SUITE      ");
        Console.WriteLine("=================================================");

        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        switch (command)
        {
            case "mission-demo":
                RunMissionDemo();
                break;
            case "combat-sim":
                RunCombatSimulation();
                break;
            case "squad-sim":
                RunSquadTacticsSimulation();
                break;
            case "dialogue-sim":
                RunDialogueSimulation();
                break;
            case "save-check":
                RunSaveSystemInspection();
                break;
            default:
                PrintHelp();
                break;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("\nAvailable Commands:");
        Console.WriteLine("  mission-demo  : Creates sample main and side missions and verifies objective flows");
        Console.WriteLine("  combat-sim    : Simulates weapon ballistics, recoil curves, and armor penetration");
        Console.WriteLine("  squad-sim     : Simulates AI squad commander tactical orders and flanking");
        Console.WriteLine("  dialogue-sim  : Runs an interactive branching conversation tree with condition checks");
        Console.WriteLine("  save-check    : Serializes full world state snapshot and verifies JSON schema");
    }

    public static void RunMissionDemo()
    {
        Console.WriteLine("\n[Mission Engine Demo]");
        var manager = new MissionManager();

        var mainQuest = new Mission
        {
            Id = "mq_01_blackout_origin",
            Title = "Echoes in the Dark",
            Description = "Investigate the Directorate Blackout research terminal in Sector 7",
            Category = MissionCategory.Investigation,
            Type = MissionType.MainStory,
            RequiredPlayerLevel = 1
        };
        var obj1 = new MissionObjective { Description = "Reach Sector 7 Underground Bunker", RequiredAmount = 1 };
        var obj2 = new MissionObjective { Description = "Hack Directorate Mainframe", RequiredAmount = 1 };
        var obj3 = new MissionObjective { Description = "Eliminate Directorate Sentinels", RequiredAmount = 3 };

        mainQuest.Objectives.Add(obj1);
        mainQuest.Objectives.Add(obj2);
        mainQuest.Objectives.Add(obj3);
        manager.RegisterMission(mainQuest);

        manager.StartMission(mainQuest.Id);
        Console.WriteLine($"Started: {mainQuest.Title} - Status: {mainQuest.Status}");

        manager.ProgressObjective(mainQuest.Id, obj1.Id, 1);
        manager.ProgressObjective(mainQuest.Id, obj2.Id, 1);
        manager.ProgressObjective(mainQuest.Id, obj3.Id, 3);

        Console.WriteLine($"After completion - Status: {mainQuest.Status}");
    }

    public static void RunCombatSimulation()
    {
        Console.WriteLine("\n[Combat Ballistics Simulation]");
        var rifle = WeaponFactory.CreateAssaultRifle();
        var simulator = new BallisticsSimulator();

        Console.WriteLine($"Weapon: {rifle.Name} (Base Dmg: {rifle.BaseDamage}, Armor Pen: {rifle.ArmorPenetration:P0})");

        for (int i = 1; i <= 3; i++)
        {
            Vector3F aimDir = Vector3F.Forward;
            Vector3F bulletDir = simulator.CalculateBulletDirection(aimDir, rifle.Accuracy);
            simulator.ApplyRecoil(rifle.RecoilIntensity, 0.5f);

            float distance = 40f;
            float damage = DamageCalculator.CalculateDamage(
                baseDamage: rifle.EffectiveDamage,
                maxRange: rifle.EffectiveRange,
                actualDistance: distance,
                hitZone: HitZone.Head,
                armorPenetration: rifle.ArmorPenetration,
                targetArmor: 50f
            );

            Console.WriteLine($"Shot {i} -> Bullet Dir: {bulletDir} | Headshot Dmg at {distance}m: {damage:F1}");
        }
    }

    public static void RunSquadTacticsSimulation()
    {
        Console.WriteLine("\n[Squad AI Coordination Simulation]");
        var squad = new SquadManager();

        var commander = new EnemyAIController(EnemyArchetype.Commander, new Vector3F(0, 0, 0));
        var scout = new EnemyAIController(EnemyArchetype.Scout, new Vector3F(-5, 0, 0));
        var heavy = new EnemyAIController(EnemyArchetype.Heavy, new Vector3F(5, 0, 0));

        squad.AddMember(commander);
        squad.AddMember(scout);
        squad.AddMember(heavy);

        Vector3F playerPosition = new(0, 0, 25);
        scout.Perception.ProcessSight(scout.Position, Vector3F.Forward, playerPosition, 1.0f, true, 2.0f);

        Console.WriteLine($"Scout Alertness: {scout.Perception.Alertness:F0} (Sight: {scout.Perception.HasTargetInSight})");
        squad.CoordinateTactics(playerPosition);

        foreach (var member in squad.Members)
        {
            Console.WriteLine($"Role: {member.Role} -> Order: {member.CurrentOrder} -> State: {member.Controller.CurrentStateType}");
        }
    }

    public static void RunDialogueSimulation()
    {
        Console.WriteLine("\n[Branching Dialogue Engine Simulation]");
        var graph = new DialogueGraph { Title = "Encounter with Informant Caleb", StartNodeId = "start" };

        var startNode = new DialogueNode
        {
            Id = "start",
            SpeakerName = "Caleb (Informant)",
            Text = "Keep your voice down. The Directorate patrols are double-shifting tonight. You looking for the lab?"
        };

        var choice1 = new DialogueChoice
        {
            Text = "Tell me where the security access codes are kept.",
            TargetNodeId = "intel_branch"
        };
        var choice2 = new DialogueChoice
        {
            Text = "Why should I trust a Directorate defector?",
            TargetNodeId = "trust_branch"
        };
        startNode.Choices.Add(choice1);
        startNode.Choices.Add(choice2);

        var intelNode = new DialogueNode
        {
            Id = "intel_branch",
            SpeakerName = "Caleb (Informant)",
            Text = "The commander carries the cipher card in the barracks. Watch out for snipers on the catwalk."
        };
        var trustNode = new DialogueNode
        {
            Id = "trust_branch",
            SpeakerName = "Caleb (Informant)",
            Text = "Because if you don't shut down that facility, nobody in Veyra sees next winter."
        };

        graph.AddNode(startNode);
        graph.AddNode(intelNode);
        graph.AddNode(trustNode);

        var context = new DialogueContext { PlayerLevel = 3, ReputationScore = 20 };
        var session = new DialogueSession(graph, context);

        Console.WriteLine($"[{session.CurrentNode!.SpeakerName}]: \"{session.CurrentNode.Text}\"");
        var choices = session.GetAvailableChoices();
        for (int i = 0; i < choices.Count; i++)
        {
            Console.WriteLine($"  Option {i + 1}: {choices[i].Text}");
        }

        session.SelectChoice(0);
        Console.WriteLine($"Chosen Branch -> [{session.CurrentNode!.SpeakerName}]: \"{session.CurrentNode.Text}\"");
    }

    public static void RunSaveSystemInspection()
    {
        Console.WriteLine("\n[Save System Snapshot Engine Inspection]");
        var player = new PlayerController();
        player.Attributes.AddCurrency(1250);
        player.Attributes.AddExperience(350);

        var inventory = new Inventory();
        inventory.AddItem(new ItemData { Id = "scrap_metal", Name = "Scrap Metal", BasePrice = 10, MaxStack = 50 }, 15);
        inventory.AddItem(new ItemData { Id = "medkit", Name = "First Aid Kit", BasePrice = 50 }, 2);

        var skills = new SkillTreeManager();
        var missions = new MissionManager();
        var world = new WorldManager();

        var saveMgr = new SaveManager();
        var snapshot = saveMgr.CreateSnapshot(1, "Bunker Entrance Autosave", player, inventory, skills, missions, world);
        string json = saveMgr.SerializeSnapshot(snapshot);

        Console.WriteLine($"Generated Snapshot JSON (Length: {json.Length} chars):\n{json.Substring(0, Math.Min(400, json.Length))}...");

        var restored = saveMgr.DeserializeSnapshot(json);
        Console.WriteLine($"Restored Currency: {restored?.Player.Currency} Credits | Items: {restored?.InventoryItems.Count}");
    }
}
