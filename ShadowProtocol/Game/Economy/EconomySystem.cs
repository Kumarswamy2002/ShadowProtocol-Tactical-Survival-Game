using ShadowProtocol.Core.Events;
using ShadowProtocol.Game.Inventory;
using ShadowProtocol.Game.Player;

namespace ShadowProtocol.Game.Economy;

public readonly record struct TradeCompletedEvent(
    string MerchantId,
    string ItemId,
    int Quantity,
    long TotalPrice,
    bool IsPlayerPurchase
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class MerchantStockItem
{
    public ItemData Data { get; set; } = null!;
    public int AvailableQuantity { get; set; } = 10;
    public long CustomPriceOverride { get; set; } = -1;
}

public class MerchantShop
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MerchantName { get; set; } = "Black Market Quartermaster";
    public string FactionId { get; set; } = "FreeHaven";
    public float BuyPriceMultiplier { get; set; } = 1.25f; // Merchant markup on sales to player
    public float SellPriceMultiplier { get; set; } = 0.50f; // What merchant pays to player
    public long MerchantCurrencyReserve { get; set; } = 5000;

    public List<MerchantStockItem> InventoryStock { get; } = new();

    private readonly IEventBus? _eventBus;

    public MerchantShop(string merchantName, IEventBus? eventBus = null)
    {
        MerchantName = merchantName;
        _eventBus = eventBus;
    }

    public long CalculatePlayerBuyPrice(ItemData item, int factionReputation = 0)
    {
        float repDiscount = Math.Clamp(factionReputation * 0.002f, -0.2f, 0.3f); // up to 30% discount
        float finalMultiplier = Math.Max(0.5f, BuyPriceMultiplier - repDiscount);
        return Math.Max(1, (long)(item.BasePrice * finalMultiplier));
    }

    public long CalculatePlayerSellPrice(ItemData item, int factionReputation = 0)
    {
        float repBonus = Math.Clamp(factionReputation * 0.001f, 0f, 0.2f);
        float finalMultiplier = Math.Min(1.0f, SellPriceMultiplier + repBonus);
        return Math.Max(1, (long)(item.BasePrice * finalMultiplier));
    }

    public bool BuyFromMerchant(string itemId, int quantity, PlayerAttributes player, Inventory.Inventory playerInventory, int reputation = 0)
    {
        var stock = InventoryStock.FirstOrDefault(s => s.Data.Id == itemId);
        if (stock == null || stock.AvailableQuantity < quantity) return false;

        long unitPrice = CalculatePlayerBuyPrice(stock.Data, reputation);
        long totalPrice = unitPrice * quantity;

        if (player.Currency < totalPrice) return false;

        if (playerInventory.AddItem(stock.Data, quantity))
        {
            player.DeductCurrency(totalPrice);
            stock.AvailableQuantity -= quantity;
            MerchantCurrencyReserve += totalPrice;

            _eventBus?.Publish(new TradeCompletedEvent(Id, itemId, quantity, totalPrice, true));
            return true;
        }

        return false;
    }

    public bool SellToMerchant(string itemId, int quantity, PlayerAttributes player, Inventory.Inventory playerInventory, int reputation = 0)
    {
        if (playerInventory.GetTotalItemCount(itemId) < quantity) return false;

        var sampleSlot = playerInventory.Slots.FirstOrDefault(s => s != null && s.Data.Id == itemId);
        if (sampleSlot == null) return false;

        var itemData = sampleSlot.Data;
        long unitPrice = CalculatePlayerSellPrice(itemData, reputation);
        long totalPrice = unitPrice * quantity;

        if (MerchantCurrencyReserve < totalPrice) return false;

        if (playerInventory.RemoveItem(itemId, quantity))
        {
            player.AddCurrency(totalPrice);
            MerchantCurrencyReserve -= totalPrice;

            var existingStock = InventoryStock.FirstOrDefault(s => s.Data.Id == itemId);
            if (existingStock != null)
            {
                existingStock.AvailableQuantity += quantity;
            }
            else
            {
                InventoryStock.Add(new MerchantStockItem { Data = itemData, AvailableQuantity = quantity });
            }

            _eventBus?.Publish(new TradeCompletedEvent(Id, itemId, quantity, totalPrice, false));
            return true;
        }

        return false;
    }
}
