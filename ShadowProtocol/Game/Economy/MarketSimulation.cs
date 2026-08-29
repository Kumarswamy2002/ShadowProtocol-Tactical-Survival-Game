using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Economy
{
    public class CommodityItem
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public float BasePriceCredits { get; set; }
        public float CurrentPriceCredits { get; set; }
        public int StockQuantity { get; set; }
        public float DailyDemandRate { get; set; }
        public float PriceVolatility { get; set; } = 0.15f;
    }

    public class MarketSimulation
    {
        private readonly Dictionary<string, CommodityItem> _commodities = new Dictionary<string, CommodityItem>();
        private static readonly Random _rng = new Random(2048);

        public MarketSimulation()
        {
            RegisterItem(new CommodityItem { ItemId = "res_fuel_canister", Name = "Refined Bio-Fuel", BasePriceCredits = 120f, CurrentPriceCredits = 120f, StockQuantity = 45, DailyDemandRate = 12f });
            RegisterItem(new CommodityItem { ItemId = "res_medical_gauze", Name = "Sterile Med-Gauze", BasePriceCredits = 65f, CurrentPriceCredits = 65f, StockQuantity = 120, DailyDemandRate = 35f });
            RegisterItem(new CommodityItem { ItemId = "res_electronics_scrap", Name = "Military Circuitry", BasePriceCredits = 340f, CurrentPriceCredits = 340f, StockQuantity = 18, DailyDemandRate = 6f });
            RegisterItem(new CommodityItem { ItemId = "res_pure_water", Name = "Purified Hydration Pack", BasePriceCredits = 40f, CurrentPriceCredits = 40f, StockQuantity = 200, DailyDemandRate = 80f });
            RegisterItem(new CommodityItem { ItemId = "res_gunpowder", Name = "Smokeless Propellant", BasePriceCredits = 180f, CurrentPriceCredits = 180f, StockQuantity = 60, DailyDemandRate = 22f });
        }

        public void RegisterItem(CommodityItem item)
        {
            _commodities[item.ItemId] = item;
        }

        public void SimulateDayFluctuation()
        {
            foreach (var item in _commodities.Values)
            {
                // Dynamic supply vs demand price calculation: Price = Base * (Demand / (Supply + 1))
                float supplyRatio = (float)item.StockQuantity / Math.Max(1.0f, item.DailyDemandRate * 3.0f);
                float priceModifier = 1.0f / Math.Clamp(supplyRatio, 0.4f, 2.5f);

                // Add random market noise
                float noise = 1.0f + ((float)(_rng.NextDouble() - 0.5) * item.PriceVolatility);
                item.CurrentPriceCredits = Math.Clamp((float)Math.Round(item.BasePriceCredits * priceModifier * noise), 5f, 5000f);
            }
        }

        public float GetPrice(string itemId)
        {
            return _commodities.TryGetValue(itemId, out var item) ? item.CurrentPriceCredits : 0f;
        }

        public bool BuyItem(string itemId, int quantity, ref float playerCredits)
        {
            if (!_commodities.TryGetValue(itemId, out var item)) return false;
            if (item.StockQuantity < quantity) return false;

            float cost = item.CurrentPriceCredits * quantity;
            if (playerCredits < cost) return false;

            playerCredits -= cost;
            item.StockQuantity -= quantity;
            return true;
        }

        public bool SellItem(string itemId, int quantity, ref float playerCredits)
        {
            if (!_commodities.TryGetValue(itemId, out var item)) return false;

            float earnings = (item.CurrentPriceCredits * 0.7f) * quantity;
            playerCredits += earnings;
            item.StockQuantity += quantity;
            return true;
        }
    }
}
