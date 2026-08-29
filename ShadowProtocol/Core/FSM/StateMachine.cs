namespace ShadowProtocol.Core.FSM;

public interface IState<TContext>
{
    string Name { get; }
    void OnEnter(TContext context);
    void OnUpdate(TContext context, float deltaTime);
    void OnFixedUpdate(TContext context, float fixedDeltaTime);
    void OnExit(TContext context);
}

public interface IStateMachine<TContext>
{
    IState<TContext>? CurrentState { get; }
    IState<TContext>? PreviousState { get; }
    float TimeInCurrentState { get; }

    void RegisterState(IState<TContext> state);
    void ChangeState<TState>() where TState : IState<TContext>;
    void ChangeState(Type stateType);
    void Update(float deltaTime);
    void FixedUpdate(float fixedDeltaTime);
}

public class StateMachine<TContext> : IStateMachine<TContext>
{
    private readonly TContext _context;
    private readonly Dictionary<Type, IState<TContext>> _states = new();

    public IState<TContext>? CurrentState { get; private set; }
    public IState<TContext>? PreviousState { get; private set; }
    public float TimeInCurrentState { get; private set; }

    public event Action<IState<TContext>?, IState<TContext>>? OnStateChanged;

    public StateMachine(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RegisterState(IState<TContext> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _states[state.GetType()] = state;
    }

    public void ChangeState<TState>() where TState : IState<TContext>
    {
        ChangeState(typeof(TState));
    }

    public void ChangeState(Type stateType)
    {
        if (!_states.TryGetValue(stateType, out var newState))
        {
            throw new KeyNotFoundException($"State type {stateType.Name} is not registered in state machine.");
        }

        if (CurrentState == newState)
        {
            return;
        }

        CurrentState?.OnExit(_context);
        PreviousState = CurrentState;
        CurrentState = newState;
        TimeInCurrentState = 0f;
        CurrentState.OnEnter(_context);

        OnStateChanged?.Invoke(PreviousState, CurrentState);
    }

    public void Update(float deltaTime)
    {
        if (CurrentState != null)
        {
            TimeInCurrentState += deltaTime;
            CurrentState.OnUpdate(_context, deltaTime);
        }
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        CurrentState?.OnFixedUpdate(_context, fixedDeltaTime);
    }
}
