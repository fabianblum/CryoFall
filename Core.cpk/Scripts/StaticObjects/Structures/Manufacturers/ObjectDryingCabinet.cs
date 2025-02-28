﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Manufacturers
{
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectDryingCabinet : ProtoObjectManufacturer
    {
        public override byte ContainerFuelSlotsCount => 0;

        public override byte ContainerInputSlotsCount => 4;

        public override byte ContainerOutputSlotsCount => 4;

        public override string Description =>
            "Using this cabinet, one can produce specific kinds of products or food, like dried mushrooms or meat.";

        public override bool IsAutoSelectRecipe => false;

        public override bool IsFuelProduceByproducts => false;

        public override string Name => "Drying cabinet";

        public override ObjectMaterial ObjectMaterial => ObjectMaterial.Wood;

        public override double ObstacleBlockDamageCoef => 1.0;

        public override float StructurePointsMax => 1200;

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.PositionOffset += (0, 0.35);
            renderer.DrawOrderOffsetY = 0.25;
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryFood>();

            build.StagesCount = 5;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemPlanks>(count: 10);
            build.AddStageRequiredItem<ItemRope>(count: 1);

            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemPlanks>(count: 3);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            var offsetY = 0.4;
            data.PhysicsBody
                .AddShapeRectangle((1, 0.475),  offset: (0, offsetY))
                .AddShapeRectangle((0.9, 1.2),  offset: (0.05, offsetY),       group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle((0.8, 0.25), offset: (0.1, offsetY + 0.85), group: CollisionGroups.HitboxRanged)
                .AddShapeRectangle((0.9, 1.5),  offset: (0.05, offsetY),       group: CollisionGroups.ClickArea);
        }
    }
}