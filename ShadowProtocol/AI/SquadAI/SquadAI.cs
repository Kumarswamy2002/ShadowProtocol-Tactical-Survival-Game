using ShadowProtocol.AI.CombatAI;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.SquadAI;

public enum SquadOrder
{
    Attack,
    Flank,
    Search,
    Retreat,
    Hold,
    Protect,
    CallBackup
}

public class SquadMember
{
    public EnemyAIController Controller { get; }
    public EnemyArchetype Role => Controller.Archetype;
    public SquadOrder CurrentOrder { get; set; } = SquadOrder.Hold;

    public SquadMember(EnemyAIController controller)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }
}

public class SquadManager
{
    public string SquadId { get; } = Guid.NewGuid().ToString();
    public EnemyAIController? Commander { get; private set; }
    public List<SquadMember> Members { get; } = new();

    public Vector3F? SquadTarget { get; set; }
    public bool IsAlerted => Members.Any(m => m.Controller.Perception.Alertness > 50f);
    public int ActiveCount => Members.Count(m => !m.Controller.IsDead);

    public void AddMember(EnemyAIController enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        if (enemy.Archetype == EnemyArchetype.Commander && Commander == null)
        {
            Commander = enemy;
        }
        Members.Add(new SquadMember(enemy));
    }

    public void AssignOrder(SquadMember member, SquadOrder order)
    {
        member.CurrentOrder = order;
        switch (order)
        {
            case SquadOrder.Attack:
                member.Controller.SetState(AIStateType.Attack);
                break;
            case SquadOrder.Flank:
                member.Controller.SetState(AIStateType.Flank);
                break;
            case SquadOrder.Search:
                member.Controller.SetState(AIStateType.Search);
                break;
            case SquadOrder.Retreat:
                member.Controller.SetState(AIStateType.Retreat);
                break;
            case SquadOrder.Hold:
                member.Controller.SetState(AIStateType.TakeCover);
                break;
            case SquadOrder.CallBackup:
                member.Controller.SetState(AIStateType.CallBackup);
                break;
        }
    }

    public void CoordinateTactics(Vector3F playerPos)
    {
        if (ActiveCount == 0) return;

        // Clean up dead members
        Members.RemoveAll(m => m.Controller.IsDead);

        // Elect new commander if needed
        if (Commander == null || Commander.IsDead)
        {
            var newLeader = Members.FirstOrDefault(m => m.Role == EnemyArchetype.Elite)?.Controller ??
                            Members.FirstOrDefault()?.Controller;
            Commander = newLeader;
        }

        // Share target information across the squad
        foreach (var member in Members)
        {
            if (member.Controller.Perception.HasTargetInSight)
            {
                SquadTarget = playerPos;
                break;
            }
        }

        if (SquadTarget.HasValue)
        {
            // Tactical distribution: Scouts flank, Heavies & Assault push, Snipers hold back
            foreach (var member in Members)
            {
                member.Controller.TargetPosition = SquadTarget.Value;

                switch (member.Role)
                {
                    case EnemyArchetype.Scout:
                        AssignOrder(member, SquadOrder.Flank);
                        break;
                    case EnemyArchetype.Sniper:
                        AssignOrder(member, SquadOrder.Hold);
                        break;
                    case EnemyArchetype.Heavy:
                    case EnemyArchetype.BasicSoldier:
                        AssignOrder(member, SquadOrder.Attack);
                        break;
                    case EnemyArchetype.Commander:
                    case EnemyArchetype.Elite:
                        // Coordinate squad backup if heavily damaged
                        if (ActiveCount <= 2)
                        {
                            AssignOrder(member, SquadOrder.CallBackup);
                        }
                        else
                        {
                            AssignOrder(member, SquadOrder.Attack);
                        }
                        break;
                }
            }
        }
    }
}
