﻿namespace AtomicTorch.CBND.CoreMod.Systems.Crafting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.Items;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Systems.Creative;
    using AtomicTorch.CBND.CoreMod.Technologies;
    using AtomicTorch.CBND.GameApi.Data;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Extensions;
    using JetBrains.Annotations;

    /// <summary>
    /// Please use specific recipes abstract types. You cannot create instances of this type yourself.
    /// </summary>
    [PrepareOrder(afterType: typeof(IProtoItem))]
    public abstract class Recipe : ProtoEntity
    {
        private static readonly IReadOnlyList<TechNode> EmptyList = new TechNode[0];

        private static IReadOnlyList<Recipe> allRecipes;

        private ITextureResource customIcon;

        private List<TechNode> listedInTechNodes;

        /// <summary>
        /// Please use specific recipes abstract types. You cannot create instances of this type yourself.
        /// </summary>
        private Recipe()
        {
        }

        public static IReadOnlyList<Recipe> AllRecipes
        {
            get
            {
                if (allRecipes is null)
                {
                    var allRecipesList = Api.FindProtoEntities<Recipe>();
                    allRecipesList.RemoveAll(r => !r.IsEnabled);
                    allRecipesList.SortBy(r => r.Id);
                    allRecipes = allRecipesList;
                }

                return allRecipes;
            }
        }

        public virtual ITextureResource Icon
        {
            get
            {
                if (this.customIcon is null)
                {
                    // by default use icon from the first output item
                    return this.OutputItems.Items.FirstOrDefault()?.ProtoItem.Icon;
                }

                return this.customIcon;
            }
            protected set => this.customIcon = value;
        }

        // we're not using a IReadOnlyList as it's optimized for faster enumeration
        public ProtoItemWithCount[] InputItems { get; private set; }

        public virtual bool IsAutoUnlocked => false;

        public bool IsCancellable
        {
            get
            {
                foreach (var protoItemWithCount in this.InputItems)
                {
                    var protoItem = protoItemWithCount.ProtoItem;

                    switch (protoItem)
                    {
                        case IProtoItemWithFreshness itemWithFreshness when itemWithFreshness.FreshnessMaxValue > 0:
                        case IProtoItemWithDurability durability when durability.DurabilityMax > 0:
                            // not cancellable
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Determines if the recipe is available in recipe lists.
        /// </summary>
        public virtual bool IsEnabled => true;

        public IReadOnlyList<TechNode> ListedInTechNodes => this.listedInTechNodes ?? EmptyList;

        public override string Name => this.OutputItems.Items[0].ProtoItem.Name;

        public double OriginalDuration { get; private set; }

        public IReadOnlyOutputItems OutputItems { get; private set; }

        public abstract RecipeType RecipeType { get; }

        public virtual bool CanBeCrafted(
            [CanBeNull] ICharacter character,
            [CanBeNull] IStaticWorldObject objectStation,
            CraftingQueue craftingQueue,
            ushort countToCraft)
        {
            if (character is not null
                && !this.SharedIsTechUnlocked(character))
            {
                // locked recipe
                return false;
            }

            foreach (var craftingInputItem in this.InputItems)
            {
                var requiredCount = countToCraft * (uint)craftingInputItem.Count;
                if (requiredCount > ushort.MaxValue)
                {
                    throw new Exception(
                        $"Too many items set to craft. {craftingInputItem}x{countToCraft}={requiredCount}");
                }

                foreach (var inputItemsContainer in craftingQueue.InputContainersArray)
                {
                    if (inputItemsContainer.ContainsItemsOfType(
                        craftingInputItem.ProtoItem,
                        requiredCount,
                        out var containerAvailableCount))
                    {
                        // item is available in required amount
                        requiredCount = 0;
                        break;
                    }

                    // item is available in none or some amount
                    requiredCount -= containerAvailableCount;
                }

                if (requiredCount > 0)
                {
                    // item is not available in required amount after checking all the containers
                    return false;
                }
            }

            // everything is available
            return true;
        }

        public void PrepareProtoSetLinkWithTechNode(TechNode techNode)
        {
            if (this.listedInTechNodes is null)
            {
                if (this.IsAutoUnlocked)
                {
                    Logger.Error(
                        this
                        + " is marked as "
                        + nameof(this.IsAutoUnlocked)
                        + " but the technology is set as the prerequisite: "
                        + techNode);
                }

                this.listedInTechNodes = new List<TechNode>();
            }

            this.listedInTechNodes.AddIfNotContains(techNode);
        }

        public double SharedGetDurationForPlayer(ICharacter character, bool cutForCreativeMode = true)
        {
            if (this.RecipeType == RecipeType.Manufacturing
                || this.RecipeType == RecipeType.ManufacturingByproduct)
            {
                return this.OriginalDuration;
            }

            double result;
            if (cutForCreativeMode
                && CreativeModeSystem.SharedIsInCreativeMode(character))
            {
                result = 0.5; // creative mode - craft in 0.5 seconds 
            }
            else
            {
                result = this.OriginalDuration
                         / (IsServer
                                ? CraftingSystem.ServerCraftingSpeedMultiplier
                                : CraftingSystem.ClientCraftingSpeedMultiplier);
            }

            // hand or station crafting - apply crafting speed bonus
            var multiplier = character.SharedGetFinalStatMultiplier(StatName.CraftingSpeed);
            if (multiplier <= 0)
            {
                // infinitely long! cannot craft
                return double.MaxValue;
            }

            result = result / multiplier;
            result = Math.Max(0.5, result); // clamp so the crafting duration cannot be less than 0.5 seconds
            return result;
        }

        public bool SharedIsTechUnlocked(ICharacter character, bool allowIfAdmin = true)
        {
            if (this.listedInTechNodes is null
                || this.listedInTechNodes.Count == 0)
            {
                return this.IsAutoUnlocked;
            }

            if (allowIfAdmin
                && CreativeModeSystem.SharedIsInCreativeMode(character))
            {
                return true;
            }

            var techs = character.SharedGetTechnologies();
            foreach (var node in this.listedInTechNodes)
            {
                if (techs.SharedIsNodeUnlocked(node))
                {
                    return true;
                }
            }

            return false;
        }

        protected static T GetItem<T>()
            where T : IProtoEntity, new()
        {
            return Api.GetProtoEntity<T>();
        }

        protected sealed override void PrepareProto()
        {
            var inputItems = new InputItems();
            var outputItems = new OutputItems();
            this.SetupRecipe(out var craftDuration, inputItems, outputItems);

            if (this.RecipeType != RecipeType.ManufacturingByproduct
                && this.RecipeType != RecipeType.Manufacturing)
            {
                Api.Assert(inputItems.Count > 0, "Crafting recipe requires at least one input item.");
            }

            Api.Assert(outputItems.Count > 0, "Crafting recipe requires at least one output item.");

            this.OriginalDuration = craftDuration.TotalSeconds;

            inputItems.ApplyRates(RecipeConstants.InputItemsCountMultiplier);
            outputItems.ApplyRates(RecipeConstants.OutputItemsCountMultiplier);

            this.InputItems = inputItems.AsReadOnly().ToArray();
            this.OutputItems = outputItems.AsReadOnly();
        }

        protected abstract void SetupRecipe(
            out TimeSpan duration,
            InputItems inputItems,
            OutputItems outputItems);

        public abstract class BaseRecipeForStation : Recipe
        {
            public IReadOnlyStationsList StationTypes { get; private set; }

            public override bool CanBeCrafted(
                ICharacter character,
                IStaticWorldObject objectStation,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                if (this.RecipeType != RecipeType.Hand
                    && (objectStation is null
                        || !this.StationTypes.Contains(objectStation.ProtoWorldObject)))
                {
                    // requires a specific station
                    return false;
                }

                return base.CanBeCrafted(character, objectStation, craftingQueue, countToCraft);
            }

            protected sealed override void SetupRecipe(
                out TimeSpan duration,
                InputItems inputItems,
                OutputItems outputItems)
            {
                var stationTypes = new StationsList();
                this.SetupRecipe(stationTypes, out duration, inputItems, outputItems);
                this.StationTypes = stationTypes;

                this.ValidateRecipe();
            }

            protected abstract void SetupRecipe(
                StationsList stations,
                out TimeSpan duration,
                InputItems inputItems,
                OutputItems outputItems);

            protected virtual void ValidateRecipe()
            {
                if (!(this is RecipeForManufacturingByproduct))
                {
                    // only for non-byproduct recipes
                    Api.Assert(this.StationTypes.Count > 0, "Crafting station type cannot be null.");
                }
            }
        }

        // For technical reasons, RecipeForHandCrafting is inherited from RecipeForStationCrafting
        // in order to provide optional stations (that will list the recipe even though it's a hand-crafting recipe)
        public abstract class RecipeForHandCrafting : RecipeForStationCrafting
        {
            public sealed override RecipeType RecipeType => RecipeType.Hand;

            public override bool CanBeCrafted(
                ICharacter character,
                IStaticWorldObject objectStation,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                if (character is null)
                {
                    // this recipe requires a character
                    return false;
                }

                return base.CanBeCrafted(character, objectStation, craftingQueue, countToCraft);
            }

            protected sealed override void SetupRecipe(
                StationsList stations,
                out TimeSpan duration,
                InputItems inputItems,
                OutputItems outputItems)
            {
                this.SetupRecipe(out duration,
                                 inputItems,
                                 outputItems,
                                 optionalStations: stations);
            }

            protected abstract void SetupRecipe(
                out TimeSpan duration,
                InputItems inputItems,
                OutputItems outputItems,
                StationsList optionalStations);

            protected override void ValidateRecipe()
            {
                // do nothing as we don't need to enforce stations list for hand recipes
            }
        }

        public abstract class RecipeForManufacturing : BaseRecipeForStation
        {
            public sealed override RecipeType RecipeType => RecipeType.Manufacturing;

            public sealed override bool CanBeCrafted(
                ICharacter character,
                IStaticWorldObject objectStation,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                if (character is not null)
                {
                    // this recipe cannot be crafted by character as it's a manufacturing recipe
                    return false;
                }

                return this.CanBeCrafted(objectStation, craftingQueue, countToCraft);
            }

            public virtual void ServerOnManufacturingCompleted(
                IStaticWorldObject objectManufacturer,
                CraftingQueue craftingQueue)
            {
                // do nothing (override where necessary)
            }

            protected virtual bool CanBeCrafted(
                IStaticWorldObject objectManufacturer,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                return base.CanBeCrafted(null, objectManufacturer, craftingQueue, countToCraft);
            }
        }

        public abstract class RecipeForManufacturingByproduct : BaseRecipeForStation
        {
            public IProtoItem ProtoItemFuel { get; private set; }

            public sealed override RecipeType RecipeType => RecipeType.ManufacturingByproduct;

            public bool CanBeCrafted(IProtoItem currentFuelOrOtherProtoItem)
            {
                return this.ProtoItemFuel == currentFuelOrOtherProtoItem;
            }

            public override bool CanBeCrafted(
                ICharacter character,
                IStaticWorldObject objectStation,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                throw new Exception("Incorrect method for " + nameof(RecipeForManufacturingByproduct));
            }

            protected override void SetupRecipe(
                StationsList stations,
                out TimeSpan duration,
                InputItems inputItems,
                OutputItems outputItems)
            {
                this.SetupRecipe(out duration, out var protoItemFuel, outputItems);
                Api.Assert(protoItemFuel is not null, "Crafting byproduct recipe requires proto item fuel.");
                this.ProtoItemFuel = protoItemFuel;
            }

            protected abstract void SetupRecipe(
                out TimeSpan craftDuration,
                out IProtoItem protoItemFuel,
                OutputItems outputItems);
        }

        public abstract class RecipeForStationCrafting : BaseRecipeForStation
        {
            public override RecipeType RecipeType => RecipeType.StationCrafting;

            public override bool CanBeCrafted(
                ICharacter character,
                IStaticWorldObject objectStation,
                CraftingQueue craftingQueue,
                ushort countToCraft)
            {
                if (character is null)
                {
                    // this recipe requires a character
                    return false;
                }

                // Please note: base type check will perform whether the station argument is provided and valid.
                return base.CanBeCrafted(character, objectStation, craftingQueue, countToCraft);
            }
        }

        protected class StationsList : List<IProtoStaticWorldObject>, IReadOnlyStationsList
        {
            public StationsList() : base(capacity: 1)
            {
            }

            public StationsList Add<TStationType>()
                where TStationType : IProtoStaticWorldObject, new()
            {
                this.Add(Api.GetProtoEntity<TStationType>());
                return this;
            }

            public StationsList AddAll<TStationType>()
                where TStationType : class, IProtoStaticWorldObject
            {
                var protos = Api.FindProtoEntities<TStationType>();
                if (protos.Count == 0)
                {
                    throw new Exception("Cannot find proto classes implementing " + typeof(TStationType).Name);
                }

                this.AddRange(protos);
                return this;
            }

            // Optimized to minimize GC allocations.
            public bool Contains(IProtoWorldObject protoWorldObject)
            {
                foreach (var entry in this)
                {
                    if (ReferenceEquals(entry, protoWorldObject))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}