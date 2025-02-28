﻿namespace AtomicTorch.CBND.CoreMod.Items.Fishing
{
    using System;
    using AtomicTorch.CBND.CoreMod.Items.Fishing.Base;
    using AtomicTorch.CBND.CoreMod.Items.Food;
    using AtomicTorch.CBND.CoreMod.Systems.Droplists;

    public class ItemFishKoi : ProtoItemFish
    {
        public override string Description => GetProtoEntity<ItemFishBass>().Description;

        public override TimeSpan FreshnessDuration => ExpirationDuration.Normal;

        public override bool IsSaltwaterFish => false;

        public override float MaxLength => 50;

        public override float MaxWeight => 8;

        public override string Name => "Koi";

        public override byte RequiredFishingKnowledgeLevel => 70;

        protected override void PrepareProtoItemFish(FishingBaitWeightList baitWeightList, DropItemsList dropItemsList)
        {
            baitWeightList.Add<ItemFishingBaitInsect>(weight: 35)
                          .Add<ItemFishingBaitMix>(weight: 35)
                          .Add<ItemFishingBaitFish>(weight: 30);

            dropItemsList.Add<ItemFishFilletRed>(count: 2);
        }
    }
}