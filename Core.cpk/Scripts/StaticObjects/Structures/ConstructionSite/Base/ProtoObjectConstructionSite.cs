﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.ConstructionSite
{
    using System;
    using AtomicTorch.CBND.CoreMod.ClientComponents.StaticObjects;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Doors;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Walls;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Deconstruction;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects.Bars;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Primitives;

    public abstract class ProtoObjectConstructionSite
        : ProtoObjectStructure
            <StructurePrivateState,
                ConstructionSitePublicState,
                StaticObjectClientState>
    {
        // Faster decay speed for blueprints outside the land claim areas to prevent world overcrowding with the blueprints.
        public const double DecaySpeedMultiplier = 4;

        private static readonly Lazy<RenderingMaterial> BlueprintMaterial
            = new(() =>
                  {
                      var material = RenderingMaterial.Create(new EffectResource("ConstructionBlueprint"));
                      material.EffectParameters
                              .Set("ColorAdd",      new Vector4(0,     0.2f, 0.3f, 0))
                              .Set("ColorMultiply", new Vector4(0.75f, 0.8f, 1f,   0.667f));
                      return material;
                  });

        private static readonly Lazy<RenderingMaterial> BlueprintMaterialMouseOver
            = new(() =>
                  {
                      var material = RenderingMaterial.Create(new EffectResource("ConstructionBlueprintHover"));
                      material.EffectParameters
                              .Set("ColorAdd",      new Vector4(0,     0.2f, 0.3f, 0))
                              .Set("ColorMultiply", new Vector4(0.75f, 0.8f, 1f,   1.0f));
                      return material;
                  });

        public override string InteractionTooltipText => InteractionTooltipTexts.Deconstruct;

        public override bool IsActivatesRaidblockOnDestroy => false;

        public override bool IsRelocatable => false;

        public sealed override double ServerUpdateIntervalSeconds => 0.25;

        /// <summary>
        /// The construction site doesn't have structure points max itself.
        /// Please use SharedGetStructurePointsMax method instead.
        /// </summary>
        public sealed override float StructurePointsMax => 0;

        public static IProtoObjectStructure SharedGetConstructionProto(IStaticWorldObject staticWorldObject)
        {
            return staticWorldObject.ProtoWorldObject is ProtoObjectConstructionSite
                       ? GetPublicState(staticWorldObject).ConstructionProto
                       : null;
        }

        public static bool SharedIsConstructionOf(IStaticWorldObject staticWorldObject, IProtoStaticWorldObject proto)
        {
            return staticWorldObject.ProtoWorldObject is ProtoObjectConstructionSite
                   && GetPublicState(staticWorldObject).ConstructionProto == proto;
        }

        public static bool SharedIsConstructionOf(IStaticWorldObject staticWorldObject, Type prototype)
        {
            return staticWorldObject.ProtoWorldObject is ProtoObjectConstructionSite
                   && prototype.IsInstanceOfType(SharedGetConstructionProto(staticWorldObject));
        }

        public override string ClientGetTitle(IWorldObject worldObject)
        {
            var publicState = GetPublicState((IStaticWorldObject)worldObject);
            return publicState.ConstructionProto.Name;
        }

        public override StaticObjectLayoutReadOnly GetLayout(IStaticWorldObject worldObject)
        {
            return GetPublicState(worldObject)
                   .ConstructionProto?
                   .Layout
                   ?? this.Layout;
        }

        public override IConstructionStageConfigReadOnly GetStructureActiveConfig(IStaticWorldObject staticWorldObject)
        {
            if (staticWorldObject.ProtoGameObject != this)
            {
                throw new Exception($"{staticWorldObject} is not {this}");
            }

            return GetPublicState(staticWorldObject)
                   .ConstructionProto
                   .ConfigBuild;
        }

        public override void ServerApplyDecay(IStaticWorldObject worldObject, double deltaTime)
        {
            // decay with an increased speed (please note that decay applies only to the buildings outside the land claim areas)
            base.ServerApplyDecay(worldObject, DecaySpeedMultiplier * deltaTime);
        }

        public override bool SharedCanInteract(ICharacter character, IStaticWorldObject worldObject, bool writeToLog)
        {
            return this.SharedIsInsideCharacterInteractionArea(character, worldObject, writeToLog);
        }

        public override Vector2D SharedGetObjectCenterWorldOffset(IWorldObject worldObject)
        {
            if (worldObject is null)
            {
                return (0.5, 0.5);
            }

            var constructionProto = GetPublicState((IStaticWorldObject)worldObject).ConstructionProto;
            try
            {
                return constructionProto.SharedGetObjectCenterWorldOffset(null);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                return constructionProto.Layout.Center;
            }
        }

        public override float SharedGetStructurePointsMax(IStaticWorldObject worldObject)
        {
            return GetPublicState(worldObject)
                   .ConstructionProto?
                   .StructurePointsMaxForConstructionSite
                   ?? 0;
        }

        public override bool SharedIsInsideCharacterInteractionArea(
            ICharacter character,
            IStaticWorldObject worldObject,
            bool writeToLog,
            CollisionGroup requiredCollisionGroup = null)
        {
            return ConstructionSystem.CheckCanInteractForConstruction(character,
                                                                      worldObject,
                                                                      writeToLog,
                                                                      checkRaidblock: true);
        }

        protected override ITextureResource ClientCreateIcon()
        {
            return new TextureResource("Icons/IconConstructionSite");
        }

        protected override void ClientDeinitializeStructure(IStaticWorldObject gameObject)
        {
            base.ClientDeinitializeStructure(gameObject);
            SharedWallConstructionRefreshHelper.SharedRefreshNeighborObjects(gameObject.OccupiedTile,
                                                                             isDestroy: true);
        }

        protected override void ClientInitialize(ClientInitializeData data)
        {
            // do not call base implementation
            //base.ClientInitialize(data);

            StructureLandClaimIndicatorManager.ClientInitialize(data.GameObject);

            var worldObject = data.GameObject;
            var publicState = data.PublicState;

            var protoStructure = publicState.ConstructionProto;
            var blueprint = new ClientBlueprintRenderer(
                worldObject.ClientSceneObject,
                isConstructionSite: true,
                // not used
                centerOffset: default);

            protoStructure.ClientSetupBlueprint(worldObject.OccupiedTile, blueprint);
            blueprint.SpriteRenderer.DrawOrder = DrawOrder.Default;
            blueprint.SpriteRenderer.RenderingMaterial = BlueprintMaterial.Value;
            foreach (var spriteRenderer in blueprint.SceneObject.FindComponents<IComponentSpriteRenderer>())
            {
                spriteRenderer.RenderingMaterial = BlueprintMaterial.Value;
            }

            data.ClientState.Renderer = blueprint.SpriteRenderer;

            ClientConstructionSiteOutlineHelper.CreateOutlineRenderer(worldObject, protoStructure);

            if (protoStructure is IProtoObjectWall
                || protoStructure is IProtoObjectDoor)
            {
                SharedWallConstructionRefreshHelper.SharedRefreshNeighborObjects(
                    data.GameObject.OccupiedTile,
                    isDestroy: false);
            }

            var sceneObject = worldObject.ClientSceneObject;
            sceneObject.AddComponent<ClientComponentAutoDisplayConstructionSiteStructurePointsBar>()
                       .Setup(worldObject,
                              structurePointsMax: this.SharedGetStructurePointsMax(worldObject));
        }

        protected override void ClientInteractFinish(ClientObjectData data)
        {
            DeconstructionSystem.ClientTryAbortAction();
        }

        protected override void ClientInteractStart(ClientObjectData data)
        {
            DeconstructionSystem.ClientTryStartAction();
        }

        protected override void ClientObserving(ClientObjectData data, bool isObserving)
        {
            if (isObserving)
            {
                if (data.ClientState?.Renderer is { } renderer)
                {
                    renderer.RenderingMaterial = BlueprintMaterialMouseOver.Value;
                }

                ConstructionSystem.ClientTryStartAction(allowReplacingCurrentConstructionAction: false);
            }
            else
            {
                if (data.ClientState?.Renderer is { } renderer)
                {
                    renderer.RenderingMaterial = BlueprintMaterial.Value;
                }
            }
        }

        protected override ReadOnlySoundPreset<ObjectSound> PrepareSoundPresetObject()
        {
            return ObjectsSoundsPresets.ObjectConstructionSite;
        }

        protected override void ServerOnReturnItemsFromDeconstructionStage(
            IStaticWorldObject worldObject,
            ICharacter byCharacter,
            float oldStructurePoints,
            float newStructurePoints)
        {
            var configBuild = this.GetStructureActiveConfig(worldObject);
            configBuild.ServerReturnRequiredItems(byCharacter);
        }

        protected override void ServerUpdate(ServerUpdateData data)
        {
            var publicState = data.PublicState;
            var protoStructure = publicState.ConstructionProto;

            if (protoStructure is null)
            {
                // incorrect construction site! destroy it
                Server.World.DestroyObject(data.GameObject);
                return;
            }

            if (publicState.StructurePointsCurrent < protoStructure.StructurePointsMaxForConstructionSite)
            {
                // construction is not completed yet
                return;
            }

            // construction completed!
            var worldObject = data.GameObject;
            var byCharacter = publicState.LastBuildActionDoneByCharacter;
            ConstructionPlacementSystem.ServerReplaceConstructionSiteWithStructure(
                worldObject,
                protoStructure,
                byCharacter);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            var constructionProto = data.PublicState.ConstructionProto;
            if (constructionProto is null)
            {
                return;
            }

            var physicsBody = data.PhysicsBody;
            constructionProto.SharedCreatePhysicsConstructionBlueprint(physicsBody);
        }

        protected override bool SharedIsAllowedObjectToInteractThrough(IWorldObject worldObject)
        {
            return true;
        }
    }
}