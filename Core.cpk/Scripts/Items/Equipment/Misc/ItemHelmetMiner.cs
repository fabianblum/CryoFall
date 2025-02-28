﻿namespace AtomicTorch.CBND.CoreMod.Items.Equipment
{
    using AtomicTorch.CBND.CoreMod.ClientComponents.Input;
    using AtomicTorch.CBND.CoreMod.ClientComponents.Rendering.Lighting;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Menu.Options.Data;
    using System.Collections.Generic;

    public class ItemHelmetMiner : ProtoItemEquipmentHeadWithLight
    {
        public override string Description =>
            "Convenient helmet that also provides a portable light source and keeps your hands free. Requires disposable batteries.";

        public override uint DurabilityMax => 800;

        public override bool IsHairVisible => false;

        public override string Name => "Miner helmet";

        protected override void PrepareDefense(DefenseDescription defense)
        {
            defense.Set(
                impact: 0.45,
                kinetic: 0.40,
                explosion: 0.40,
                heat: 0.25,
                cold: 0.15,
                chemical: 0.25,
                radiation: 0.20,
                psi: 0.0);

            // normal value override, we don't want it to be affected by armor multiplier later
            defense.Psi = 0.30 / defense.Multiplier;
        }

        protected override void PrepareProtoItemEquipmentHeadWithLight(
            ItemLightConfig lightConfig,
            ItemFuelConfig fuelConfig)
        {
            lightConfig.Color = LightColors.OilLamp;
            lightConfig.ScreenOffset = (20, -3);
            lightConfig.Size = 17;

            fuelConfig.FuelCapacity = 1000; // >10 minutes
            fuelConfig.FuelAmountInitial = 0;
            fuelConfig.FuelUsePerSecond = 1;
            fuelConfig.FuelProtoItemsList.AddAll<IProtoItemFuelElectricity>();
        }

        protected override ReadOnlySoundPreset<ItemSound> PrepareSoundPresetItem()
        {
            return ItemsSoundPresets.ItemGeneric.Clone()
                                    .Replace(ItemSound.Use,          "Items/Equipment/UseLight")
                                    .Replace(ItemSound.CannotSelect, "Items/Equipment/UseLight");
        }

        protected override void PrepareHints(List<string> hints)
        {
            base.PrepareHints(hints);

            var key = ClientInputManager.GetKeyForButton(GameButton.HeadEquipmentLightToggle);
            hints.Add(string.Format(ItemHints.HelmetLightAndNightVision, InputKeyNameHelper.GetKeyText(key)));
        }
    }
}