using ShadowProtocol.AI.Perception;
using ShadowProtocol.Core.FSM;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Combat;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.Weapons;

namespace ShadowProtocol.AI.CombatAI;

public enum AIStateType
{
    Idle,
    Patrol,
    Investigate,
    Search,
    Alert,
    Chase,
    Attack,
    TakeCover,
    Flank,
    Retreat,
    CallBackup,
    Dead
}

public enum EnemyArchetype
{
    BasicSoldier,
    Scout,
    Heavy,
    Sniper,
    Commander,
    Elite
}

public interface IAIContext
{
    string Id { get; }
    EnemyArchetype Archetype { get; }
    AIStateType CurrentStateType { get; }
    Vector3F Position { get; set; }
    Vector3F Forward { get; set; }
    Vector3F Velocity { get; set; }
    float MoveSpeed { get; }
    float Health { get; set; }
    float MaxHealth { get; }
    float Armor { get; set; }
    bool IsDead { get; }
    AIPerception Perception { get; }
    Firearm? EquippedWeapon { get; set; }
    Vector3F? TargetPosition { get; set; }
    Vector3F? CoverPosition { get; set; }
    bool InCover { get; set; }
    float AttackRange { get; }

    void SetState(AIStateType newState);
    void TakeDamage(float amount, Vector3F sourcePosition, float armorPen = 0.2f);
}

public class EnemyAIController : IAIContext
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public EnemyArchetype Archetype { get; }
    public AIStateType CurrentStateType { get; private set; } = AIStateType.Idle;

    public Vector3F Position { get; set; } = Vector3F.Zero;
    public Vector3F Forward { get; set; } = Vector3F.Forward;
    public Vector3F Velocity { get; set; } = Vector3F.Zero;

    public float MoveSpeed { get; private set; } = 3.5f;
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; } = 100f;
    public float Armor { get; set; } = 25f;
    public bool IsDead => Health <= 0f;

    public AIPerception Perception { get; } = new();
    public Firearm? EquippedWeapon { get; set; }

    public Vector3F? TargetPosition { get; set; }
    public Vector3F? CoverPosition { get; set; }
    public bool InCover { get; set; } = false;
    public float AttackRange => EquippedWeapon?.EffectiveRange ?? 20f;

    public List<Vector3F> PatrolWaypoints { get; } = new();
    private int _currentWaypointIndex = 0;
    private float _stateTimer = 0f;

    public EnemyAIController(EnemyArchetype archetype, Vector3F initialPosition)
    {
        Archetype = archetype;
        Position = initialPosition;
        ConfigureArchetype(archetype);
    }

    private void ConfigureArchetype(EnemyArchetype archetype)
    {
        switch (archetype)
        {
            case EnemyArchetype.Scout:
                MoveSpeed = 5.2f;
                MaxHealth = 80f;
                Health = 80f;
                Armor = 10f;
                Perception.ViewDistance = 55f;
                Perception.HearingSensitivity = 1.4f;
                EquippedWeapon = WeaponFactory.CreateSMG();
                break;
            case EnemyArchetype.Heavy:
                MoveSpeed = 2.4f;
                MaxHealth = 220f;
                Health = 220f;
                Armor = 100f;
                Perception.ViewDistance = 35f;
                EquippedWeapon = WeaponFactory.CreateShotgun();
                break;
            case EnemyArchetype.Sniper:
                MoveSpeed = 3.0f;
                MaxHealth = 70f;
                Health = 70f;
                Armor = 15f;
                Perception.ViewDistance = 90f;
                EquippedWeapon = WeaponFactory.CreateSniperRifle();
                break;
            case EnemyArchetype.Commander:
                MoveSpeed = 3.8f;
                MaxHealth = 150f;
                Health = 150f;
                Armor = 50f;
                Perception.ViewDistance = 50f;
                EquippedWeapon = WeaponFactory.CreateAssaultRifle();
                break;
            case EnemyArchetype.Elite:
                MoveSpeed = 4.5f;
                MaxHealth = 180f;
                Health = 180f;
                Armor = 75f;
                Perception.ViewDistance = 60f;
                EquippedWeapon = WeaponFactory.CreateAssaultRifle();
                break;
            default: // BasicSoldier
                MoveSpeed = 3.5f;
                MaxHealth = 100f;
                Health = 100f;
                Armor = 30f;
                EquippedWeapon = WeaponFactory.CreateAssaultRifle();
                break;
        }
    }

    public void SetState(AIStateType newState)
    {
        if (CurrentStateType == newState || IsDead) return;
        CurrentStateType = newState;
        _stateTimer = 0f;
    }

    public void TakeDamage(float amount, Vector3F sourcePosition, float armorPen = 0.2f)
    {
        if (IsDead) return;

        float effectiveArmor = Math.Max(0f, Armor * (1.0f - armorPen));
        float mitigation = effectiveArmor / (effectiveArmor + 100f);
        float actualDamage = amount * (1.0f - mitigation);

        Health = Math.Max(0f, Health - actualDamage);
        Perception.ReceiveDirectDamage(sourcePosition);

        if (IsDead)
        {
            SetState(AIStateType.Dead);
        }
        else if (CurrentStateType is AIStateType.Idle or AIStateType.Patrol)
        {
            SetState(AIStateType.Alert);
        }
    }

    public void Update(float deltaTime, Vector3F playerPos, float playerStealth, bool hasLoS, float gameTime)
    {
        if (IsDead) return;

        _stateTimer += deltaTime;
        Perception.ProcessSight(Position, Forward, playerPos, playerStealth, hasLoS, deltaTime);

        if (Perception.HasTargetInSight)
        {
            TargetPosition = playerPos;
        }

        // State Machine Decision Evaluation
        switch (CurrentStateType)
        {
            case AIStateType.Idle:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (Perception.Alertness > 50f)
                {
                    SetState(AIStateType.Investigate);
                }
                else if (PatrolWaypoints.Count > 0 && _stateTimer > 4.0f)
                {
                    SetState(AIStateType.Patrol);
                }
                break;

            case AIStateType.Patrol:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (Perception.Alertness > 40f)
                {
                    SetState(AIStateType.Investigate);
                }
                else
                {
                    UpdatePatrol(deltaTime);
                }
                break;

            case AIStateType.Investigate:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (Perception.LastKnownTargetPosition.HasValue)
                {
                    MoveTowards(Perception.LastKnownTargetPosition.Value, deltaTime, MoveSpeed * 0.8f);
                    if (Vector3F.Distance(Position, Perception.LastKnownTargetPosition.Value) < 3.0f)
                    {
                        SetState(AIStateType.Search);
                    }
                }
                else
                {
                    SetState(AIStateType.Idle);
                }
                break;

            case AIStateType.Search:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (_stateTimer > 6.0f)
                {
                    SetState(AIStateType.Idle);
                }
                break;

            case AIStateType.Alert:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (_stateTimer > 3.0f)
                {
                    SetState(AIStateType.Investigate);
                }
                break;

            case AIStateType.Attack:
                if (!Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Chase);
                }
                else
                {
                    UpdateCombat(deltaTime, gameTime);
                }
                break;

            case AIStateType.Chase:
                if (Perception.HasTargetInSight)
                {
                    SetState(AIStateType.Attack);
                }
                else if (TargetPosition.HasValue)
                {
                    MoveTowards(TargetPosition.Value, deltaTime, MoveSpeed * 1.2f);
                    if (_stateTimer > 8.0f)
                    {
                        SetState(AIStateType.Search);
                    }
                }
                break;

            case AIStateType.TakeCover:
                if (CoverPosition.HasValue)
                {
                    MoveTowards(CoverPosition.Value, deltaTime, MoveSpeed * 1.3f);
                    if (Vector3F.Distance(Position, CoverPosition.Value) < 1.0f)
                    {
                        InCover = true;
                        if (_stateTimer > 4.0f)
                        {
                            SetState(AIStateType.Attack);
                        }
                    }
                }
                else
                {
                    SetState(AIStateType.Attack);
                }
                break;

            case AIStateType.Flank:
                if (TargetPosition.HasValue)
                {
                    Vector3F flankOffset = Vector3F.Cross(Forward, Vector3F.Up).Normalized * 10f;
                    Vector3F flankDestination = TargetPosition.Value + flankOffset;
                    MoveTowards(flankDestination, deltaTime, MoveSpeed * 1.1f);
                    if (_stateTimer > 5.0f)
                    {
                        SetState(AIStateType.Attack);
                    }
                }
                break;

            case AIStateType.Retreat:
                if (TargetPosition.HasValue)
                {
                    Vector3F runAwayDir = (Position - TargetPosition.Value).Normalized;
                    MoveTowards(Position + (runAwayDir * 15f), deltaTime, MoveSpeed * 1.4f);
                    if (_stateTimer > 5.0f)
                    {
                        SetState(AIStateType.TakeCover);
                    }
                }
                break;
        }
    }

    private void UpdatePatrol(float deltaTime)
    {
        if (PatrolWaypoints.Count == 0) return;

        Vector3F destination = PatrolWaypoints[_currentWaypointIndex];
        MoveTowards(destination, deltaTime, MoveSpeed * 0.6f);

        if (Vector3F.Distance(Position, destination) < 1.5f)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % PatrolWaypoints.Count;
            SetState(AIStateType.Idle);
        }
    }

    private void UpdateCombat(float deltaTime, float gameTime)
    {
        if (!TargetPosition.HasValue || EquippedWeapon == null) return;

        Vector3F toTarget = TargetPosition.Value - Position;
        Forward = toTarget.Normalized;

        float dist = toTarget.Magnitude;

        // Reposition if too close or too far
        if (dist > AttackRange * 0.9f)
        {
            MoveTowards(TargetPosition.Value, deltaTime, MoveSpeed);
        }
        else if (dist < 4.0f && Archetype != EnemyArchetype.Heavy)
        {
            // Back up
            MoveTowards(Position - (Forward * 5f), deltaTime, MoveSpeed * 0.8f);
        }

        // Check health for retreat / cover
        if (Health < MaxHealth * 0.25f && Archetype != EnemyArchetype.Heavy)
        {
            SetState(AIStateType.Retreat);
            return;
        }

        // Fire weapon if possible
        if (EquippedWeapon.CanShoot(gameTime))
        {
            EquippedWeapon.Shoot(gameTime, Position, Forward);
        }
        else if (EquippedWeapon.CurrentAmmo <= 0)
        {
            if (EquippedWeapon.StartReload())
            {
                SetState(AIStateType.TakeCover);
            }
        }
    }

    private void MoveTowards(Vector3F destination, float deltaTime, float speed)
    {
        Vector3F dir = (destination - Position).Normalized;
        if (dir.SqrMagnitude > 0.001f)
        {
            Forward = dir;
            Velocity = dir * speed;
            Position += Velocity * deltaTime;
        }
    }
}
