using ShadowProtocol.Core.Events;

namespace ShadowProtocol.Game.Inventory;

public enum ItemCategory
{
    Weapons,
    Armor,
    Ammunition,
    Medical,
    Food,
    Materials,
    QuestItems,
    CraftingItems,
    Miscellaneous
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public class ItemData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemCategory Category { get; set; } = ItemCategory.Miscellaneous;
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;
    public float Weight { get; set; } = 0.5f; // kg
    public int MaxStack { get; set; } = 1;
    public long BasePrice { get; set; } = 10;
    public float MaxDurability { get; set; } = 100f;
}

public class ItemStack
{
    public ItemData Data { get; }
    public int Quantity { get; set; }
    public float CurrentDurability { get; set; }

    public float TotalWeight => Data.Weight * Quantity;
    public bool IsBroken => CurrentDurability <= 0f;

    public ItemStack(ItemData data, int quantity = 1, float durability = 100f)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Quantity = Math.Clamp(quantity, 1, data.MaxStack);
        CurrentDurability = durability;
    }
}

public readonly record struct InventoryChangedEvent(int SlotCount, float TotalWeight, float MaxWeight) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class Inventory
{
    public int MaxSlots { get; set; } = 30;
    public float MaxWeight { get; set; } = 40.0f; // kg

    private readonly List<ItemStack?> _slots;
    private readonly IEventBus? _eventBus;

    public IReadOnlyList<ItemStack?> Slots => _slots;
    public float TotalWeight => _slots.Where(s => s != null).Sum(s => s!.TotalWeight);
    public bool IsOverburdened => TotalWeight > MaxWeight;

    public Inventory(int maxSlots = 30, float maxWeight = 40.0f, IEventBus? eventBus = null)
    {
        MaxSlots = maxSlots;
        MaxWeight = maxWeight;
        _eventBus = eventBus;
        _slots = new List<ItemStack?>(new ItemStack?[maxSlots]);
    }

    public bool AddItem(ItemData data, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (quantity <= 0) return false;

        // 1. Try stacking in existing slots if stackable
        if (data.MaxStack > 1)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var stack = _slots[i];
                if (stack != null && stack.Data.Id == data.Id && stack.Quantity < data.MaxStack)
                {
                    int canAdd = Math.Min(quantity, data.MaxStack - stack.Quantity);
                    stack.Quantity += canAdd;
                    quantity -= canAdd;

                    if (quantity == 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }
        }

        // 2. Place remainder in empty slots
        while (quantity > 0)
        {
            int emptyIndex = _slots.FindIndex(s => s == null);
            if (emptyIndex == -1)
            {
                NotifyInventoryChanged();
                return false; // Inventory full
            }

            int toPlace = Math.Min(quantity, data.MaxStack);
            _slots[emptyIndex] = new ItemStack(data, toPlace, data.MaxDurability);
            quantity -= toPlace;
        }

        NotifyInventoryChanged();
        return true;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0 || GetTotalItemCount(itemId) < quantity) return false;

        for (int i = 0; i < _slots.Count; i++)
        {
            var stack = _slots[i];
            if (stack != null && stack.Data.Id == itemId)
            {
                if (stack.Quantity <= quantity)
                {
                    quantity -= stack.Quantity;
                    _slots[i] = null;
                }
                else
                {
                    stack.Quantity -= quantity;
                    quantity = 0;
                }

                if (quantity == 0) break;
            }
        }

        NotifyInventoryChanged();
        return true;
    }

    public int GetTotalItemCount(string itemId)
    {
        return _slots.Where(s => s != null && s.Data.Id == itemId).Sum(s => s!.Quantity);
    }

    public void SortByCategory()
    {
        var items = _slots.Where(s => s != null).OrderBy(s => s!.Data.Category).ThenBy(s => s!.Data.Name).ToList();
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i] = i < items.Count ? items[i] : null;
        }
        NotifyInventoryChanged();
    }

    private void NotifyInventoryChanged()
    {
        _eventBus?.Publish(new InventoryChangedEvent(_slots.Count(s => s != null), TotalWeight, MaxWeight));
    }
}
