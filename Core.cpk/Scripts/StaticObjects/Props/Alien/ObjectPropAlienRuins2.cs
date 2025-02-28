﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Props.Alien
{
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectPropAlienRuins2 : ProtoObjectProp
    {
        public override bool CanFlipSprite => true;

        public override ObjectMaterial ObjectMaterial
            => ObjectMaterial.Metal;

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = 0.3;
            renderer.Scale = 0.8;
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("#");
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle(size: (0.8, 0.6), offset: (0.1, 0.0))
                .AddShapeRectangle(size: (0.8, 0.7), offset: (0.1, 0.1), group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle(size: (0.6, 0.6), offset: (0.2, 0.7), group: CollisionGroups.HitboxRanged);
        }
    }
}