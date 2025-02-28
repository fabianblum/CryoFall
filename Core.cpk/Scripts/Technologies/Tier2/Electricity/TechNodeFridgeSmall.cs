﻿namespace AtomicTorch.CBND.CoreMod.Technologies.Tier2.Electricity
{
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Fridges;

    public class TechNodeFridgeSmall : TechNode<TechGroupElectricityT2>
    {
        protected override void PrepareTechNode(Config config)
        {
            config.Effects
                  .AddStructure<ObjectFridgeSmall>();

            config.SetRequiredNode<TechNodePowerStorage>();
        }
    }
}