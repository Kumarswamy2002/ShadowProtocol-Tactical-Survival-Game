using ShadowProtocol.Core.Events;

namespace ShadowProtocol.Game.Dialogue;

public readonly record struct DialogueChoiceSelectedEvent(
    string NodeId,
    string ChoiceText,
    string NextNodeId
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class DialogueChoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public Func<DialogueContext, bool>? Condition { get; set; }
    public Action<DialogueContext>? OnSelected { get; set; }

    public bool IsAvailable(DialogueContext context) => Condition == null || Condition(context);
}

public class DialogueNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SpeakerName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<DialogueChoice> Choices { get; } = new();
    public Action<DialogueContext>? OnNodeEnter { get; set; }
}

public class DialogueContext
{
    public string PlayerId { get; set; } = "Player";
    public int PlayerLevel { get; set; } = 1;
    public int ReputationScore { get; set; } = 0;
    public Dictionary<string, object> Variables { get; } = new();

    public bool HasFlag(string key) => Variables.TryGetValue(key, out var v) && v is bool b && b;
    public void SetFlag(string key, bool value) => Variables[key] = value;
}

public class DialogueGraph
{
    public string GraphId { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Conversation";
    public string StartNodeId { get; set; } = "start";
    public Dictionary<string, DialogueNode> Nodes { get; } = new();

    public void AddNode(DialogueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes[node.Id] = node;
    }

    public DialogueNode? GetNode(string nodeId)
    {
        return Nodes.GetValueOrDefault(nodeId);
    }
}

public class DialogueSession
{
    public DialogueGraph Graph { get; }
    public DialogueContext Context { get; }
    public DialogueNode? CurrentNode { get; private set; }
    public bool IsFinished { get; private set; }

    private readonly IEventBus? _eventBus;

    public DialogueSession(DialogueGraph graph, DialogueContext context, IEventBus? eventBus = null)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _eventBus = eventBus;

        SetCurrentNode(graph.StartNodeId);
    }

    public IReadOnlyList<DialogueChoice> GetAvailableChoices()
    {
        if (CurrentNode == null || IsFinished) return Array.Empty<DialogueChoice>();
        return CurrentNode.Choices.Where(c => c.IsAvailable(Context)).ToList();
    }

    public bool SelectChoice(int choiceIndex)
    {
        var available = GetAvailableChoices();
        if (choiceIndex < 0 || choiceIndex >= available.Count)
        {
            return false;
        }

        var choice = available[choiceIndex];
        choice.OnSelected?.Invoke(Context);
        _eventBus?.Publish(new DialogueChoiceSelectedEvent(CurrentNode!.Id, choice.Text, choice.TargetNodeId));

        if (string.IsNullOrEmpty(choice.TargetNodeId) || !Graph.Nodes.ContainsKey(choice.TargetNodeId))
        {
            IsFinished = true;
            CurrentNode = null;
            return true;
        }

        SetCurrentNode(choice.TargetNodeId);
        return true;
    }

    private void SetCurrentNode(string nodeId)
    {
        if (Graph.Nodes.TryGetValue(nodeId, out var node))
        {
            CurrentNode = node;
            CurrentNode.OnNodeEnter?.Invoke(Context);
            if (CurrentNode.Choices.Count == 0)
            {
                IsFinished = true;
            }
        }
        else
        {
            IsFinished = true;
            CurrentNode = null;
        }
    }
}
