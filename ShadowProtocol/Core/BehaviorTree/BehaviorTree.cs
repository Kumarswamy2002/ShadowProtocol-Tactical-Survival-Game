namespace ShadowProtocol.Core.BehaviorTree;

public enum BehaviorStatus
{
    Success,
    Failure,
    Running
}

public class Blackboard
{
    private readonly Dictionary<string, object?> _data = new();

    public void Set<T>(string key, T value) => _data[key] = value;

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_data.TryGetValue(key, out var val) && val is T typedVal)
        {
            return typedVal;
        }
        return defaultValue;
    }

    public bool Has(string key) => _data.ContainsKey(key);
    public bool Remove(string key) => _data.Remove(key);
    public void Clear() => _data.Clear();
}

public abstract class BTNode
{
    public string Name { get; set; }

    protected BTNode(string name = "Node")
    {
        Name = name;
    }

    public abstract BehaviorStatus Tick(Blackboard blackboard);
    public virtual void Reset() { }
}

public abstract class CompositeNode : BTNode
{
    protected readonly List<BTNode> Children = new();

    protected CompositeNode(string name, params BTNode[] children) : base(name)
    {
        if (children != null)
        {
            Children.AddRange(children);
        }
    }

    public void AddChild(BTNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Children.Add(child);
    }

    public override void Reset()
    {
        foreach (var child in Children)
        {
            child.Reset();
        }
    }
}

public class SequenceNode : CompositeNode
{
    private int _currentChildIndex;

    public SequenceNode(string name, params BTNode[] children) : base(name, children) { }

    public override BehaviorStatus Tick(Blackboard blackboard)
    {
        while (_currentChildIndex < Children.Count)
        {
            var status = Children[_currentChildIndex].Tick(blackboard);
            if (status == BehaviorStatus.Running)
            {
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Failure)
            {
                Reset();
                return BehaviorStatus.Failure;
            }
            _currentChildIndex++;
        }

        Reset();
        return BehaviorStatus.Success;
    }

    public override void Reset()
    {
        _currentChildIndex = 0;
        base.Reset();
    }
}

public class SelectorNode : CompositeNode
{
    private int _currentChildIndex;

    public SelectorNode(string name, params BTNode[] children) : base(name, children) { }

    public override BehaviorStatus Tick(Blackboard blackboard)
    {
        while (_currentChildIndex < Children.Count)
        {
            var status = Children[_currentChildIndex].Tick(blackboard);
            if (status == BehaviorStatus.Running)
            {
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Success)
            {
                Reset();
                return BehaviorStatus.Success;
            }
            _currentChildIndex++;
        }

        Reset();
        return BehaviorStatus.Failure;
    }

    public override void Reset()
    {
        _currentChildIndex = 0;
        base.Reset();
    }
}

public class InverterNode : BTNode
{
    private readonly BTNode _child;

    public InverterNode(string name, BTNode child) : base(name)
    {
        _child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public override BehaviorStatus Tick(Blackboard blackboard)
    {
        var status = _child.Tick(blackboard);
        return status switch
        {
            BehaviorStatus.Success => BehaviorStatus.Failure,
            BehaviorStatus.Failure => BehaviorStatus.Success,
            _ => BehaviorStatus.Running
        };
    }

    public override void Reset() => _child.Reset();
}

public class ConditionNode : BTNode
{
    private readonly Func<Blackboard, bool> _predicate;

    public ConditionNode(string name, Func<Blackboard, bool> predicate) : base(name)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public override BehaviorStatus Tick(Blackboard blackboard)
    {
        return _predicate(blackboard) ? BehaviorStatus.Success : BehaviorStatus.Failure;
    }
}

public class ActionNode : BTNode
{
    private readonly Func<Blackboard, BehaviorStatus> _action;

    public ActionNode(string name, Func<Blackboard, BehaviorStatus> action) : base(name)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override BehaviorStatus Tick(Blackboard blackboard)
    {
        return _action(blackboard);
    }
}
