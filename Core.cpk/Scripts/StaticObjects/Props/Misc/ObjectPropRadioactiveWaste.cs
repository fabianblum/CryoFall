﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Props.Misc
{
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectPropRadioactiveWaste : ProtoObjectProp
    {
        public override bool CanFlipSprite => true;

        public override ObjectMaterial ObjectMaterial
            => ObjectMaterial.Metal;

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = 0.65;
            renderer.Scale = 0.8;
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("#");
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle(size: (1.0, 0.9), offset: (0.0, 0.0))
                .AddShapeRectangle(size: (1.0, 0.8), offset: (0.0, 0.1), group: CollisionGroups.HitboxMelee);
        }
    }
}