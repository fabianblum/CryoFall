﻿namespace AtomicTorch.CBND.CoreMod.Items.Food
{
    using System;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Neutral;
    using AtomicTorch.CBND.CoreMod.SoundPresets;

    public class ItemCucumbersPickled : ProtoItemFood
    {
        public override string Description => "Sweet and crunchy pickles.";

        public override float FoodRestore => 8;

        public override TimeSpan FreshnessDuration => ExpirationDuration.LongLasting;

        public override string Name => "Pickled cucumbers";

        public override ushort OrganicValue => 5;

        public override float WaterRestore => 1;

        protected override ReadOnlySoundPreset<ItemSound> PrepareSoundPresetItem()
        {
            return ItemsSoundPresets.ItemFood;
        }

        protected override void PrepareEffects(EffectActionsList effects)
        {
            effects
                .WillRemoveEffect<StatusEffectDrunk>(intensityToRemove: 0.05); // just a little bit as a joke :)
        }
    }
}