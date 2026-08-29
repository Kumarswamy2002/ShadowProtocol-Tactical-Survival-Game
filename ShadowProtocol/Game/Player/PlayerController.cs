using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.FSM;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Player;

public class PlayerController : IPlayerContext
{
    public PlayerVitals Vitals { get; }
    public PlayerAttributes Attributes { get; }
    public StateMachine<IPlayerContext> StateMachine { get; }

    public Vector3F Position { get; set; } = Vector3F.Zero;
    public Vector3F Velocity { get; set; } = Vector3F.Zero;
    public Vector3F InputDirection { get; set; } = Vector3F.Zero;
    public MovementStance Stance { get; set; } = MovementStance.Stand;

    public bool IsGrounded { get; set; } = true;
    public bool IsInWater { get; set; } = false;
    public bool IsNearClimbable { get; set; } = false;
    public bool IsNearVaultable { get; set; } = false;

    public float BaseWalkSpeed { get; set; } = 4.5f;
    public float CurrentSpeed => Velocity.Magnitude;
    public float NoiseLevel { get; set; } = 0.0f;

    public PlayerController(IEventBus? eventBus = null)
    {
        Vitals = new PlayerVitals(eventBus);
        Attributes = new PlayerAttributes(eventBus);

        StateMachine = new StateMachine<IPlayerContext>(this);
        StateMachine.RegisterState(new IdleState());
        StateMachine.RegisterState(new WalkingState());
        StateMachine.RegisterState(new RunningState());
        StateMachine.RegisterState(new SprintingState());
        StateMachine.RegisterState(new CrouchingState());
        StateMachine.RegisterState(new StealthMovementState());
        StateMachine.RegisterState(new JumpingState());
        StateMachine.RegisterState(new ClimbingState());
        StateMachine.RegisterState(new VaultingState());
        StateMachine.RegisterState(new DodgingState());
        StateMachine.RegisterState(new SwimmingState());

        StateMachine.ChangeState<IdleState>();
    }

    public void HandleMovementInput(Vector3F direction, bool wantsToSprint, bool wantsToCrouch, bool wantsStealth)
    {
        InputDirection = direction.Normalized;

        if (IsInWater)
        {
            if (StateMachine.CurrentState is not SwimmingState)
                StateMachine.ChangeState<SwimmingState>();
            return;
        }

        if (IsNearClimbable && direction.Y > 0.1f)
        {
            if (StateMachine.CurrentState is not ClimbingState)
                StateMachine.ChangeState<ClimbingState>();
            return;
        }

        if (InputDirection.SqrMagnitude < 0.001f)
        {
            if (StateMachine.CurrentState is not IdleState &&
                StateMachine.CurrentState is not VaultingState &&
                StateMachine.CurrentState is not DodgingState)
            {
                StateMachine.ChangeState<IdleState>();
            }
            return;
        }

        if (wantsStealth)
        {
            StateMachine.ChangeState<StealthMovementState>();
        }
        else if (wantsToCrouch)
        {
            StateMachine.ChangeState<CrouchingState>();
        }
        else if (wantsToSprint && !Vitals.IsExhausted)
        {
            StateMachine.ChangeState<SprintingState>();
        }
        else
        {
            StateMachine.ChangeState<WalkingState>();
        }
    }

    public void PerformJump()
    {
        if (IsGrounded && !Vitals.IsExhausted && StateMachine.CurrentState is not JumpingState)
        {
            StateMachine.ChangeState<JumpingState>();
        }
    }

    public void PerformDodge()
    {
        if (IsGrounded && !Vitals.IsExhausted && StateMachine.CurrentState is not DodgingState)
        {
            StateMachine.ChangeState<DodgingState>();
        }
    }

    public void PerformVault()
    {
        if (IsNearVaultable && StateMachine.CurrentState is not VaultingState)
        {
            StateMachine.ChangeState<VaultingState>();
        }
    }

    public void Update(float deltaTime)
    {
        StateMachine.Update(deltaTime);

        // Check for state completion transitions
        if (StateMachine.CurrentState is VaultingState vault && vault.IsFinished)
        {
            StateMachine.ChangeState<IdleState>();
        }
        else if (StateMachine.CurrentState is DodgingState dodge && dodge.IsFinished)
        {
            StateMachine.ChangeState<IdleState>();
        }

        // Apply basic velocity translation
        Position += Velocity * deltaTime;
    }
}
