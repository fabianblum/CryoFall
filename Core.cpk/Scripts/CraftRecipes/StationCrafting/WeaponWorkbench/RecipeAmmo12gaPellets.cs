﻿namespace AtomicTorch.CBND.CoreMod.CraftRecipes
{
    using System;
    using AtomicTorch.CBND.CoreMod.Items.Ammo;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.CraftingStations;
    using AtomicTorch.CBND.CoreMod.Systems;
    using AtomicTorch.CBND.CoreMod.Systems.Crafting;

    public class RecipeAmmo12gaPellets : Recipe.RecipeForStationCrafting
    {
        protected override void SetupRecipe(
            StationsList stations,
            out TimeSpan duration,
            InputItems inputItems,
            OutputItems outputItems)
        {
            stations.Add<ObjectWeaponWorkbench>();

            duration = CraftingDuration.VeryShort;

            inputItems.Add<ItemIngotCopper>(count: 4);
            inputItems.Add<ItemNitrocellulosePowder>(count: 30);

            outputItems.Add<ItemAmmo12gaPellets>(count: 10);
        }
    }
}