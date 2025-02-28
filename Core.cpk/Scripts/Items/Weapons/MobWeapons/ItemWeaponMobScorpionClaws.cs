﻿namespace AtomicTorch.CBND.CoreMod.Items.Weapons.MobWeapons
{
    using System.Collections.Generic;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Debuffs;
    using AtomicTorch.CBND.CoreMod.Items.Ammo;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Weapons;
    using AtomicTorch.GameEngine.Common.Helpers;

    public class ItemWeaponMobScorpionClaws : ProtoItemMobWeaponMelee
    {
        public override double DamageApplyDelay => 0.15;

        public override double FireAnimationDuration => 0.4;

        public override double FireInterval => 1.5;

        protected override void PrepareProtoWeapon(
            out IEnumerable<IProtoItemAmmo> compatibleAmmoProtos,
            ref DamageDescription overrideDamageDescription)
        {
            // no ammo used
            compatibleAmmoProtos = null;

            var damageDistribution = new DamageDistribution()
                                     .Set(DamageType.Impact,   0.7)
                                     .Set(DamageType.Chemical, 0.3); // also uses poison or something :)

            overrideDamageDescription = new DamageDescription(
                damageValue: 25,
                armorPiercingCoef: 0.1,
                finalDamageMultiplier: 1,
                rangeMax: 1.2,
                damageDistribution: damageDistribution);
        }

        protected override void ServerOnSpecialEffect(ICharacter damagedCharacter, double damage)
        {
            if (RandomHelper.RollWithProbability(0.7))
            {
                damagedCharacter.ServerAddStatusEffect<StatusEffectBleeding>(intensity: 0.1);
                damagedCharacter.ServerAddStatusEffect<StatusEffectPain>(intensity: 0.4);
            }

            if (RandomHelper.RollWithProbability(0.25))
            {
                damagedCharacter.ServerAddStatusEffect<StatusEffectToxins>(intensity: 0.2);
            }

            if (RandomHelper.RollWithProbability(0.15))
            {
                damagedCharacter.ServerAddStatusEffect<StatusEffectBrokenLeg>(intensity: 1);
            }
        }
    }
}