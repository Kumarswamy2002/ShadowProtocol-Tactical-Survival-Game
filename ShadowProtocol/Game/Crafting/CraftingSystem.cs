using ShadowProtocol.Core.Events;
using ShadowProtocol.Game.Inventory;

namespace ShadowProtocol.Game.Crafting;

public enum CraftingStationType
{
    Field,
    Workbench,
    ChemistryStation,
    ArmorBench,
    MunitionsStation
}

public class CraftingIngredient
{
    public string ItemId { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; } = 1;
}

public class CraftingRecipe
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CraftingStationType RequiredStation { get; set; } = CraftingStationType.Field;
    public float CraftTimeSeconds { get; set; } = 2.0f;
    public List<CraftingIngredient> Ingredients { get; } = new();
    public ItemData ResultItem { get; set; } = null!;
    public int ResultQuantity { get; set; } = 1;
    public bool IsUnlockedByDefault { get; set; } = true;
}

public readonly record struct ItemCraftedEvent(string RecipeId, string ItemName, int Quantity) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class CraftingEngine
{
    private readonly Dictionary<string, CraftingRecipe> _recipes = new();
    private readonly HashSet<string> _unlockedRecipes = new();
    private readonly IEventBus? _eventBus;

    public IReadOnlyCollection<CraftingRecipe> Recipes => _recipes.Values;

    public CraftingEngine(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
        RegisterDefaultRecipes();
    }

    public void RegisterRecipe(CraftingRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        _recipes[recipe.Id] = recipe;
        if (recipe.IsUnlockedByDefault)
        {
            _unlockedRecipes.Add(recipe.Id);
        }
    }

    public void UnlockBlueprint(string recipeId)
    {
        if (_recipes.ContainsKey(recipeId))
        {
            _unlockedRecipes.Add(recipeId);
        }
    }

    public bool CanCraft(string recipeId, Inventory.Inventory inventory, CraftingStationType currentStation)
    {
        if (!_recipes.TryGetValue(recipeId, out var recipe)) return false;
        if (!_unlockedRecipes.Contains(recipeId)) return false;
        if (recipe.RequiredStation != CraftingStationType.Field && recipe.RequiredStation != currentStation) return false;

        foreach (var ingredient in recipe.Ingredients)
        {
            if (inventory.GetTotalItemCount(ingredient.ItemId) < ingredient.RequiredQuantity)
            {
                return false;
            }
        }
        return true;
    }

    public bool Craft(string recipeId, Inventory.Inventory inventory, CraftingStationType currentStation)
    {
        if (!CanCraft(recipeId, inventory, currentStation)) return false;

        var recipe = _recipes[recipeId];

        // Consume ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            inventory.RemoveItem(ingredient.ItemId, ingredient.RequiredQuantity);
        }

        // Add result item
        inventory.AddItem(recipe.ResultItem, recipe.ResultQuantity);

        _eventBus?.Publish(new ItemCraftedEvent(recipe.Id, recipe.ResultItem.Name, recipe.ResultQuantity));
        return true;
    }

    private void RegisterDefaultRecipes()
    {
        // 1. Weapon Parts from Metal + Scrap
        var weaponPartsItem = new ItemData
        {
            Id = "weapon_parts",
            Name = "Weapon Parts",
            Category = ItemCategory.CraftingItems,
            Weight = 0.3f,
            MaxStack = 50,
            BasePrice = 30
        };
        var recipe1 = new CraftingRecipe
        {
            Id = "craft_weapon_parts",
            Name = "Assemble Weapon Parts",
            RequiredStation = CraftingStationType.Workbench,
            ResultItem = weaponPartsItem,
            ResultQuantity = 2
        };
        recipe1.Ingredients.Add(new CraftingIngredient { ItemId = "scrap_metal", RequiredQuantity = 3 });
        recipe1.Ingredients.Add(new CraftingIngredient { ItemId = "electronic_components", RequiredQuantity = 1 });
        RegisterRecipe(recipe1);

        // 2. Medkit from Cloth + Antiseptic
        var medkitItem = new ItemData
        {
            Id = "medkit_military",
            Name = "Military Medkit",
            Category = ItemCategory.Medical,
            Weight = 0.5f,
            MaxStack = 10,
            BasePrice = 75
        };
        var recipe2 = new CraftingRecipe
        {
            Id = "craft_medkit",
            Name = "Craft Military Medkit",
            RequiredStation = CraftingStationType.Field,
            ResultItem = medkitItem,
            ResultQuantity = 1
        };
        recipe2.Ingredients.Add(new CraftingIngredient { ItemId = "bandage_cloth", RequiredQuantity = 2 });
        recipe2.Ingredients.Add(new CraftingIngredient { ItemId = "antiseptic_alcohol", RequiredQuantity = 1 });
        RegisterRecipe(recipe2);

        // 3. AP Ammunition from Chemicals + Lead
        var apAmmoItem = new ItemData
        {
            Id = "ammo_556_ap",
            Name = "5.56mm Armor-Piercing Rounds",
            Category = ItemCategory.Ammunition,
            Weight = 0.02f,
            MaxStack = 120,
            BasePrice = 5
        };
        var recipe3 = new CraftingRecipe
        {
            Id = "craft_ap_ammo",
            Name = "Cast AP Rifle Ammo",
            RequiredStation = CraftingStationType.MunitionsStation,
            ResultItem = apAmmoItem,
            ResultQuantity = 30
        };
        recipe3.Ingredients.Add(new CraftingIngredient { ItemId = "gunpowder", RequiredQuantity = 2 });
        recipe3.Ingredients.Add(new CraftingIngredient { ItemId = "lead_ingot", RequiredQuantity = 2 });
        RegisterRecipe(recipe3);
    }
}
