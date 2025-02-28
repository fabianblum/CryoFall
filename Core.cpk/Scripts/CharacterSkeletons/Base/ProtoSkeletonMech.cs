﻿namespace AtomicTorch.CBND.CoreMod.CharacterSkeletons
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.CharacterSkeletons.Mech;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Extensions;
    using AtomicTorch.GameEngine.Common.Helpers;
    using AtomicTorch.GameEngine.Common.Primitives;

    public abstract class ProtoSkeletonMech : ProtoCharacterSkeleton
    {
        // cache singleton instance for human footsteps sound
        private static readonly Lazy<ReadOnlySoundPreset<GroundSoundMaterial>> MechFootstepsSoundPreset
            = new(() =>
                  {
                      const string localSoundsFolderPath = "Skeletons/Mech/Footsteps/";

                      var preset = new SoundPreset<GroundSoundMaterial>();
                      foreach (var enumValue in EnumExtensions.GetValues<GroundSoundMaterial>())
                      {
                          // use solid sound for every ground type
                          var soundFileName = GroundSoundMaterial.Solid;
                          preset.Add(enumValue, localSoundsFolderPath + soundFileName);
                      }

                      var readOnlySoundPreset = preset.ToReadOnly();
                      //this.VerifySoundPreset(readOnlySoundPreset);
                      return readOnlySoundPreset;
                  });

        private static readonly SoundResource SoundResourceMovement
            = new("Objects/Vehicles/Mech/Movement");

        public override double DefaultMoveSpeed => 1.5;

        public override bool HasMoveStartAnimations => true;

        public override Vector2D InventoryOffset => (55, 55);

        public override double InventoryScale => 0.6;

        public override float OrientationDownExtraAngle => 35;

        public override float OrientationThresholdDownHorizontalFlipDeg => 25;

        public override float OrientationThresholdDownToUpFlipDeg => 45;

        public override float OrientationThresholdUpHorizontalFlipDeg => 20;

        public override string SlotNameItemInHand => "TurretLeft";

        public override SoundResource SoundResourceAimingProcess { get; }
            = new("Objects/Vehicles/Mech/Aiming");

        public override double SpeedMultiplier => 1;

        public override double WorldScale => 0.15;

        protected abstract float AnimationVerticalMovemementSpeedMultiplier { get; }

        protected override RangeDouble FootstepsPitchVariationRange { get; }
            = new(0.98, 1.02);

        protected override RangeDouble FootstepsVolumeVariationRange { get; }
            = new(0.95, 1);

        protected virtual double ServoDriveVolume => 0.3;

        // TODO: use proper sounds folder
        //protected override string SoundsFolderPath => "Skeletons/Mech";
        protected override string SoundsFolderPath => "Skeletons/Human";

        public override void OnSkeletonCreated(IComponentSkeleton skeleton)
        {
            base.OnSkeletonCreated(skeleton);

            if (skeleton.SceneObject.AttachedWorldObject is ICharacter characterPilot)
            {
                skeleton.SceneObject
                        .AddComponent<ComponentSkeletonMechAimingSoundManager>()
                        .Setup(skeleton, characterPilot, this.SoundResourceAimingProcess);
            }

            skeleton.AnimationEvent += SkeletonOnAnimationEventFootstepMovement;

            // little offset to ensure mech can properly behind a grass
            // cannot make it further without making it to z-fight with a player character
            skeleton.DrawOrderOffsetY = -0.3;

            skeleton.SetDefaultMixDuration(0.3f);

            // disable mix for these animations
            DisableMix("RunUp");
            DisableMix("RunDown");
            DisableMix("RunSide");
            DisableMix("RunSideBackward");

            var verticalSpeedMultiplier = this.AnimationVerticalMovemementSpeedMultiplier;
            skeleton.SetAnimationDefaultSpeed("RunUp",        verticalSpeedMultiplier);
            skeleton.SetAnimationDefaultSpeed("RunUpStart",   verticalSpeedMultiplier);
            skeleton.SetAnimationDefaultSpeed("RunDown",      verticalSpeedMultiplier);
            skeleton.SetAnimationDefaultSpeed("RunDownStart", verticalSpeedMultiplier);

            void DisableMix(string primaryName)
            {
                var startName = primaryName + "Start";
                var startAbortName = startName + "Abort";
                var minMix = 0.05f;
                skeleton.SetMixDuration(startName,      minMix);
                skeleton.SetMixDuration(startAbortName, minMix);
                skeleton.SetMixDuration("Idle",         startName,      minMix);
                skeleton.SetMixDuration(startName,      primaryName,    0);
                skeleton.SetMixDuration(startName,      startAbortName, minMix);
                skeleton.SetMixDuration(startAbortName, "Idle",         minMix);

                skeleton.SetAnimationDefaultSpeed(startAbortName, 1.3f);
            }
        }

        protected override ReadOnlySoundPreset<GroundSoundMaterial> PrepareSoundPresetFootsteps()
        {
            return MechFootstepsSoundPreset.Value;
        }

        [SuppressMessage("ReSharper", "CanExtractXamlLocalizableStringCSharp")]
        private static void SkeletonOnAnimationEventFootstepMovement(
            ICharacter character,
            IComponentSkeleton skeleton,
            SkeletonEventData e)
        {
            if (e.EventName != "MovementStart")
            {
                return;
            }

            var protoSkeleton = character.GetClientState<BaseCharacterClientState>().CurrentProtoSkeleton
                                    as ProtoSkeletonMech;

            if (protoSkeleton is null)
            {
                return;
            }

            // use some pitch variation
            var pitch = RandomHelper.Range(protoSkeleton.FootstepsPitchVariationRange.From,
                                           protoSkeleton.FootstepsPitchVariationRange.To);

            var volume = protoSkeleton.ServoDriveVolume;
            // apply some volume variation
            volume *= RandomHelper.Range(protoSkeleton.FootstepsVolumeVariationRange.From,
                                         protoSkeleton.FootstepsVolumeVariationRange.To);
            // apply constant volume multiplier
            volume *= SoundConstants.VolumeFootstepsMultiplier;

            Client.Audio.PlayOneShot(
                SoundResourceMovement,
                character,
                volume: (float)(volume),
                pitch: (float)pitch);
        }
    }
}