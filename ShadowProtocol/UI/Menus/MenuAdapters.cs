using ShadowProtocol.Game.Dialogue;
using ShadowProtocol.Game.Economy;
using ShadowProtocol.Game.Inventory;
using ShadowProtocol.Game.Missions;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.Skills;

namespace ShadowProtocol.UI.Menus;

public class InventoryMenuAdapter
{
    public record ItemView(string Id, string Name, string Category, int Quantity, float Weight, string Rarity);

    public List<ItemView> GetInventoryItems(Inventory inventory)
    {
        return inventory.Slots
            .Where(s => s != null)
            .Select(s => new ItemView(
                s!.Data.Id,
                s.Data.Name,
                s.Data.Category.ToString(),
                s.Quantity,
                s.TotalWeight,
                s.Data.Rarity.ToString()
            ))
            .ToList();
    }
}

public class MissionJournalAdapter
{
    public record MissionEntryView(string Id, string Title, string Description, string Category, string Status, List<string> Objectives);

    public List<MissionEntryView> GetMissions(MissionManager manager)
    {
        return manager.AllMissions.Select(m => new MissionEntryView(
            m.Id,
            m.Title,
            m.Description,
            m.Category.ToString(),
            m.Status.ToString(),
            m.Objectives.Select(o => $"{(o.IsCompleted ? "[✓]" : "[ ]")} {o.Description} ({o.CurrentAmount}/{o.RequiredAmount})").ToList()
        )).ToList();
    }
}

public class DialogueUIAdapter
{
    public string SpeakerName { get; private set; } = string.Empty;
    public string DialogueText { get; private set; } = string.Empty;
    public List<string> Options { get; } = new();

    public void BindSession(DialogueSession session)
    {
        if (session.CurrentNode != null)
        {
            SpeakerName = session.CurrentNode.SpeakerName;
            DialogueText = session.CurrentNode.Text;
            Options.Clear();
            var choices = session.GetAvailableChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                Options.Add($"{i + 1}. {choices[i].Text}");
            }
        }
    }
}
