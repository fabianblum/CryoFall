﻿namespace AtomicTorch.CBND.CoreMod.Items.Weapons.MobWeapons
{
    using System.Collections.Generic;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Debuffs;
    using AtomicTorch.CBND.CoreMod.Items.Ammo;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Weapons;
    using AtomicTorch.GameEngine.Common.Helpers;

    public class ItemWeaponMobSnakeBiteStrong : ProtoItemMobWeaponMelee
    {
        public override double DamageApplyDelay => 0.125;

        public override double FireAnimationDuration => 0.5;

        public override double FireInterval => 1.5;

        protected override WeaponFireTracePreset PrepareFireTracePreset()
        {
            return WeaponFireTracePresets.NoWeapon;
        }

        protected override void PrepareProtoWeapon(
            out IEnumerable<IProtoItemAmmo> compatibleAmmoProtos,
            ref DamageDescription overrideDamageDescription)
        {
            // no ammo used
            compatibleAmmoProtos = null;

            var damageDistribution = new DamageDistribution()
                                     .Set(DamageType.Impact,   0.5)
                                     .Set(DamageType.Chemical, 0.5); // uses poison or something :)

            overrideDamageDescription = new DamageDescription(
                damageValue: 18,
                armorPiercingCoef: 0,
                finalDamageMultiplier: 1.25,
                rangeMax: 1.5,
                damageDistribution: damageDistribution);
        }

        protected override ReadOnlySoundPreset<ObjectMaterial> PrepareSoundPresetHit()
        {
            return MaterialHitsSoundPresets.MeleeNoWeapon;
        }

        protected override void ServerOnSpecialEffect(ICharacter damagedCharacter, double damage)
        {
            if (RandomHelper.RollWithProbability(0.85))
            {
                damagedCharacter.ServerAddStatusEffect<StatusEffectToxins>(intensity: 0.25);
            }
        }
    }
}