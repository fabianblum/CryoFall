﻿namespace AtomicTorch.CBND.CoreMod.Zones
{
    using System;
    using AtomicTorch.CBND.CoreMod.Characters.Mobs;
    using AtomicTorch.CBND.CoreMod.Helpers.Server;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Deposits;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Minerals;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Props.Road;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Special;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.LandClaim;
    using AtomicTorch.CBND.CoreMod.Systems.LandClaim;
    using AtomicTorch.CBND.CoreMod.Technologies.Tier3.XenoGeology;
    using AtomicTorch.CBND.CoreMod.Triggers;
    using AtomicTorch.CBND.GameApi.Data;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Data.Zones;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Helpers;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class SpawnDepositOilSeep : ProtoZoneSpawnScript
    {
        private double spawnRateMultiplier;

        // because this script called very rare we're increasing the spawn attempts count
        protected override double MaxSpawnAttemptsMultiplier => 300;

        protected override void PrepareZoneSpawnScript(Triggers triggers, SpawnList spawnList)
        {
            this.spawnRateMultiplier = MathHelper.Clamp(
                ServerRates.Get(
                    "Resources.PvP.DepositOilSeepNumberMultiplier",
                    defaultValue: 1.0,
                    @"This multiplier determines how many oil deposits should be present in each suitable biome.
                      E.g. to reduce the max number of oil seeps to half, change this to 0.5.
                      You can also completely disable the spawn of oil deposits by changing this to 0.
                      Please note: it's available only in PvP. (in PvE there is no need for deposits to capture). 
                      It's not possible to define a specific number as it depends on the biome and map size.
                      (allowed range: from 0.0 to 10.0)"),
                min: 0,
                max: 10);

            // this resource is not spawned on the world init
            triggers
                // trigger on time interval
                .Add(GetTrigger<TriggerTimeInterval>()
                         .Configure(
                             intervalFrom: TimeSpan.FromHours(0.5),
                             intervalTo: TimeSpan.FromHours(1)));

            var restrictionPresetPragmium = spawnList.CreateRestrictedPreset()
                                                     .Add<ObjectMineralPragmiumSource>();

            var restrictionDepletedDeposit = spawnList.CreateRestrictedPreset()
                                                      .Add<ObjectDepletedDeposit>();

            var restrictionInfiniteOilSeep = spawnList.CreateRestrictedPreset()
                                                      .Add<ObjectDepositOilSeepInfinite>();

            var presetOilSeep = spawnList.CreatePreset(interval: 183, padding: 1, useSectorDensity: false);
            presetOilSeep.SpawnLimitPerIteration = 1;
            presetOilSeep.AddExact<ObjectDepositOilSeep>()
                         .SetCustomPaddingWithSelf(79)
                         .SetCustomPaddingWith(restrictionInfiniteOilSeep, 59)
                         .SetCustomPaddingWith(restrictionDepletedDeposit, 49)
                         // ensure no spawn near Pragmium
                         .SetCustomPaddingWith(restrictionPresetPragmium,
                                               SpawnResourcePragmium.PaddingPragmiumWithOilDeposit)
                         // ensure no spawn near cliffs
                         .SetCustomCanSpawnCheckCallback(
                             (physicsSpace, position)
                                 => ServerCheckAnyTileCollisions(physicsSpace,
                                                                 // offset on object layout center
                                                                 new Vector2D(position.X + 1.5,
                                                                              position.Y + 1.5),
                                                                 radius: 7));

            // don't spawn close to roads
            var restrictionPresetRoads = spawnList.CreateRestrictedPreset()
                                                  .Add<ObjectPropRoadHorizontal>()
                                                  .Add<ObjectPropRoadVertical>();
            presetOilSeep.SetCustomPaddingWith(restrictionPresetRoads, 20);

            // special restriction preset for player land claims
            var restrictionPresetLandclaim = spawnList.CreateRestrictedPreset()
                                                      .Add<IProtoObjectLandClaim>();

            // Let's ensure that we don't spawn oil seep too close to players' buildings.
            // take size of the largest land claim area
            var paddingToLandClaimsSize = (int)LandClaimSystem.MaxLandClaimSizeWithGraceArea.Value;
            // add few extra tiles (as the objects are not 1*1 tile)
            paddingToLandClaimsSize += 6;

            presetOilSeep.SetCustomPaddingWith(restrictionPresetLandclaim, paddingToLandClaimsSize);
        }

        protected override void ServerOnObjectSpawned(IGameObjectWithProto spawnedObject)
        {
            // spawn some guardian mobs so it will be harder to claim this deposit
            var objectOilSeep = (IStaticWorldObject)spawnedObject;
            ServerMobSpawnHelper.ServerTrySpawnMobsCustom(
                protoMob: Api.GetProtoEntity<MobScorpion>(),
                countToSpawn: 2,
                excludeBounds: objectOilSeep.Bounds.Inflate(1),
                maxSpawnDistanceFromExcludeBounds: 2,
                noObstaclesCheckRadius: 1,
                maxAttempts: 200);
        }

        protected override int SharedCalculatePresetDesiredCount(
            ObjectSpawnPreset preset,
            IServerZone zone,
            int currentCount,
            int desiredCountByDensity)
        {
            // apply the server rate
            desiredCountByDensity = (int)Math.Round(desiredCountByDensity * this.spawnRateMultiplier,
                                                    MidpointRounding.AwayFromZero);
            
            if (Api.IsEditor)
            {
                return desiredCountByDensity;
            }

            // throttle spawn to ensure even distribution of spawned objects during specified hours since the startup
            const double spawnSpreadDurationHours = 48;

            var hoursSinceWorldCreation = Api.Server.Game.SecondsSinceWorldCreation / (60 * 60);

            // apply the timegate offset as there should be no deposits spawn until Xenogeology is available for research
            var timeGateHours = Api.GetProtoEntity<TechGroupXenogeologyT3>().TimeGatePvP / (60 * 60);
            if (timeGateHours > 0)
            {
                // as there is a timegate ensure the spawn could start immediately after it's expired without requiring any additional time
                timeGateHours -= spawnSpreadDurationHours / desiredCountByDensity;
                timeGateHours = Math.Max(0, timeGateHours);
                hoursSinceWorldCreation -= timeGateHours;
                hoursSinceWorldCreation = Math.Max(0, hoursSinceWorldCreation);
            }

            if (hoursSinceWorldCreation >= spawnSpreadDurationHours)
            {
                return desiredCountByDensity;
            }

            return (int)(desiredCountByDensity * hoursSinceWorldCreation / spawnSpreadDurationHours);
        }
    }
}