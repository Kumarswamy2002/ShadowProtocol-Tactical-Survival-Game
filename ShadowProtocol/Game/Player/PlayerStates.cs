using ShadowProtocol.Core.FSM;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Player;

public enum MovementStance
{
    Stand,
    Crouch,
    Climb,
    Swim
}

public interface IPlayerContext
{
    PlayerVitals Vitals { get; }
    PlayerAttributes Attributes { get; }
    Vector3F Position { get; set; }
    Vector3F Velocity { get; set; }
    Vector3F InputDirection { get; set; }
    MovementStance Stance { get; set; }
    bool IsGrounded { get; set; }
    bool IsInWater { get; set; }
    bool IsNearClimbable { get; set; }
    bool IsNearVaultable { get; set; }
    float BaseWalkSpeed { get; }
    float CurrentSpeed { get; }
    float NoiseLevel { get; set; }
}

public abstract class BasePlayerState : IState<IPlayerContext>
{
    public abstract string Name { get; }
    public virtual void OnEnter(IPlayerContext context) { }
    public virtual void OnUpdate(IPlayerContext context, float deltaTime) { }
    public virtual void OnFixedUpdate(IPlayerContext context, float fixedDeltaTime) { }
    public virtual void OnExit(IPlayerContext context) { }
}

public class IdleState : BasePlayerState
{
    public override string Name => "Idle";

    public override void OnEnter(IPlayerContext context)
    {
        context.Velocity = new Vector3F(0, context.Velocity.Y, 0);
        context.NoiseLevel = 0.0f;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: false);
    }
}

public class WalkingState : BasePlayerState
{
    public override string Name => "Walking";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Stand;
        context.NoiseLevel = 5.0f * context.Attributes.NoiseMultiplier;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        float speed = context.BaseWalkSpeed * context.Attributes.MoveSpeedMultiplier;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: false);
    }
}

public class RunningState : BasePlayerState
{
    public override string Name => "Running";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Stand;
        context.NoiseLevel = 15.0f * context.Attributes.NoiseMultiplier;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        float speed = context.BaseWalkSpeed * 1.8f * context.Attributes.MoveSpeedMultiplier;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: true);
    }
}

public class SprintingState : BasePlayerState
{
    private const float StaminaDrainRate = 12.0f;

    public override string Name => "Sprinting";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Stand;
        context.NoiseLevel = 30.0f * context.Attributes.NoiseMultiplier;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        if (!context.Vitals.ConsumeStamina(StaminaDrainRate * deltaTime))
        {
            // Exhausted, drop back
            return;
        }

        float speed = context.BaseWalkSpeed * 2.6f * context.Attributes.MoveSpeedMultiplier;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: true);
    }
}

public class CrouchingState : BasePlayerState
{
    public override string Name => "Crouching";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Crouch;
        context.NoiseLevel = 1.5f * context.Attributes.NoiseMultiplier;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        float speed = context.BaseWalkSpeed * 0.5f * context.Attributes.MoveSpeedMultiplier;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: false);
    }
}

public class StealthMovementState : BasePlayerState
{
    public override string Name => "Stealth";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Crouch;
        context.NoiseLevel = 0.2f * context.Attributes.NoiseMultiplier;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        float speed = context.BaseWalkSpeed * 0.35f * context.Attributes.MoveSpeedMultiplier;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: false);
    }
}

public class JumpingState : BasePlayerState
{
    private const float JumpStaminaCost = 15.0f;
    private const float JumpVelocity = 6.5f;

    public override string Name => "Jumping";

    public override void OnEnter(IPlayerContext context)
    {
        if (context.Vitals.ConsumeStamina(JumpStaminaCost))
        {
            context.Velocity = new Vector3F(context.Velocity.X, JumpVelocity, context.Velocity.Z);
            context.IsGrounded = false;
            context.NoiseLevel = 10.0f * context.Attributes.NoiseMultiplier;
        }
    }
}

public class ClimbingState : BasePlayerState
{
    private const float ClimbStaminaDrain = 8.0f;

    public override string Name => "Climbing";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Climb;
        context.NoiseLevel = 2.0f;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        if (!context.Vitals.ConsumeStamina(ClimbStaminaDrain * deltaTime))
        {
            // Drop from climb
            context.IsNearClimbable = false;
            return;
        }
        float climbSpeed = 2.5f;
        context.Velocity = new Vector3F(0, context.InputDirection.Y * climbSpeed, 0);
    }
}

public class VaultingState : BasePlayerState
{
    private float _vaultTimer;
    private const float VaultDuration = 0.6f;

    public override string Name => "Vaulting";

    public override void OnEnter(IPlayerContext context)
    {
        _vaultTimer = 0f;
        context.Vitals.ConsumeStamina(10f);
        context.NoiseLevel = 8.0f;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        _vaultTimer += deltaTime;
        context.Velocity = context.InputDirection * (context.BaseWalkSpeed * 2.0f);
    }

    public bool IsFinished => _vaultTimer >= VaultDuration;
}

public class DodgingState : BasePlayerState
{
    private float _dodgeTimer;
    private const float DodgeDuration = 0.4f;
    private Vector3F _dodgeDirection;

    public override string Name => "Dodging";

    public override void OnEnter(IPlayerContext context)
    {
        _dodgeTimer = 0f;
        context.Vitals.ConsumeStamina(20f);
        _dodgeDirection = context.InputDirection.SqrMagnitude > 0.01f ? context.InputDirection.Normalized : Vector3F.Back;
        context.NoiseLevel = 12.0f;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        _dodgeTimer += deltaTime;
        float speed = context.BaseWalkSpeed * 3.5f * (1.0f - (_dodgeTimer / DodgeDuration));
        context.Velocity = _dodgeDirection * speed;
    }

    public bool IsFinished => _dodgeTimer >= DodgeDuration;
}

public class SwimmingState : BasePlayerState
{
    private const float SwimStaminaDrain = 4.0f;

    public override string Name => "Swimming";

    public override void OnEnter(IPlayerContext context)
    {
        context.Stance = MovementStance.Swim;
        context.NoiseLevel = 8.0f;
    }

    public override void OnUpdate(IPlayerContext context, float deltaTime)
    {
        context.Vitals.ConsumeStamina(SwimStaminaDrain * deltaTime);
        float speed = context.BaseWalkSpeed * 0.75f;
        context.Velocity = context.InputDirection * speed;
        context.Vitals.UpdateSurvivalTick(deltaTime, isExerting: true);
    }
}
