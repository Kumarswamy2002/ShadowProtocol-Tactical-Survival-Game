using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Crafting
{
    public enum WorkbenchTier
    {
        FieldCrafting = 0,
        BasicWorkbench = 1,
        AdvancedMachiningStation = 2,
        NanoFabricationForge = 3
    }

    public class CraftingIngredient
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class CraftingRecipe
    {
        public string RecipeId { get; set; }
        public string DisplayName { get; set; }
        public WorkbenchTier RequiredTier { get; set; }
        public float CraftingTimeSeconds { get; set; }
        public string ResultItemId { get; set; }
        public int ResultQuantity { get; set; } = 1;
        public List<CraftingIngredient> Ingredients { get; } = new List<CraftingIngredient>();
        public int RequiredCraftingSkillLevel { get; set; } = 1;

        public CraftingRecipe AddIngredient(string itemId, int quantity)
        {
            Ingredients.Add(new CraftingIngredient { ItemId = itemId, Quantity = quantity });
            return this;
        }
    }

    public class RecipeEngineeringDatabase
    {
        private readonly Dictionary<string, CraftingRecipe> _recipes = new Dictionary<string, CraftingRecipe>();

        public RecipeEngineeringDatabase()
        {
            InitializeRecipes();
        }

        private void InitializeRecipes()
        {
            Register(new CraftingRecipe
            {
                RecipeId = "rcp_medkit_military",
                DisplayName = "Tactical Trauma Kit",
                RequiredTier = WorkbenchTier.BasicWorkbench,
                CraftingTimeSeconds = 6.0f,
                ResultItemId = "item_trauma_kit",
                ResultQuantity = 1,
                RequiredCraftingSkillLevel = 2
            }.AddIngredient("res_medical_gauze", 2).AddIngredient("res_pure_water", 1).AddIngredient("item_antiseptic", 1));

            Register(new CraftingRecipe
            {
                RecipeId = "rcp_ammo_762_ap",
                DisplayName = "7.62x39mm Armor Piercing (x30)",
                RequiredTier = WorkbenchTier.AdvancedMachiningStation,
                CraftingTimeSeconds = 12.0f,
                ResultItemId = "ammo_762x39_ap",
                ResultQuantity = 30,
                RequiredCraftingSkillLevel = 4
            }.AddIngredient("res_gunpowder", 3).AddIngredient("res_metal_scrap", 4).AddIngredient("res_tungsten_core", 2));

            Register(new CraftingRecipe
            {
                RecipeId = "rcp_armor_plate_ceramic",
                DisplayName = "Level IV Ceramic Plate",
                RequiredTier = WorkbenchTier.AdvancedMachiningStation,
                CraftingTimeSeconds = 18.0f,
                ResultItemId = "armor_plate_lvl4",
                ResultQuantity = 1,
                RequiredCraftingSkillLevel = 5
            }.AddIngredient("res_ceramic_tiles", 5).AddIngredient("res_kevlar_fabric", 4).AddIngredient("res_resin_epoxy", 2));

            Register(new CraftingRecipe
            {
                RecipeId = "rcp_emp_grenade",
                DisplayName = "EMP Disruptor Grenade",
                RequiredTier = WorkbenchTier.NanoFabricationForge,
                CraftingTimeSeconds = 20.0f,
                ResultItemId = "weapon_grenade_emp",
                ResultQuantity = 2,
                RequiredCraftingSkillLevel = 6
            }.AddIngredient("res_electronics_scrap", 4).AddIngredient("res_battery_cell", 2).AddIngredient("res_gunpowder", 2));
        }

        public void Register(CraftingRecipe recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
        }

        public CraftingRecipe Get(string recipeId) => _recipes.TryGetValue(recipeId, out var r) ? r : null;
        public IEnumerable<CraftingRecipe> GetAll() => _recipes.Values;
    }
}
