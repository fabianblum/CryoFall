﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Props.Walls
{
    public class ObjectPropRuinsWallCornerBR : ProtoObjectProp
    {
        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle(size: (0.85, 0.65))
                .AddShapeRectangle(size: (0.7, 0.65), offset: (0.15, 0.5));
            AddFullHeightWallHitboxes(data, width: 0.85);
        }
    }
}