﻿namespace AtomicTorch.CBND.CoreMod.Editor.Tools.EditorToolStaticObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.Editor.Data;
    using AtomicTorch.CBND.CoreMod.Editor.Scripts;
    using AtomicTorch.CBND.CoreMod.Editor.Tools.Base;
    using AtomicTorch.CBND.CoreMod.Editor.Tools.Brushes;
    using AtomicTorch.CBND.CoreMod.StaticObjects;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Loot;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Minerals;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Props;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.ConstructionSite;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Floors;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Vegetation;
    using AtomicTorch.CBND.GameApi.Data;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.Scripting.Network;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class EditorToolStaticObjects : BaseEditorTool<EditorToolStaticObjectsItem>
    {
        private readonly Dictionary<IProtoEntity, WeakReference<ViewModelEditorToolItemStaticObject>>
            viewModelsDictionary = new();

        public EditorToolStaticObjects()
        {
            if (IsServer)
            {
                return;
            }

            Api.Client.World.ObjectEnterScope += this.WorldObjectEnterScopeHandler;
            Api.Client.World.ObjectLeftScope += this.WorldObjectLeftScopeHandler;
        }

        public override string Name => "Static Objects tool";

        public override int Order => 30;

        public override BaseEditorActiveTool Activate(EditorToolStaticObjectsItem item)
        {
            if (item is null)
            {
                return null;
            }

            return new EditorActiveToolObjectBrush(
                item.ProtoStaticObject,
                tilePositions => this.ClientPlaceStaticObject(tilePositions, item.ProtoStaticObject));
        }

        public override ViewModelEditorToolItem CreateItemViewModel(BaseEditorToolItem item)
        {
            var entry = (EditorToolStaticObjectsItem)item;
            var viewModel = new ViewModelEditorToolItemStaticObject(entry);
            this.viewModelsDictionary[entry.ProtoStaticObject]
                = new WeakReference<ViewModelEditorToolItemStaticObject>(viewModel);
            return viewModel;
        }

        protected override void SetupFilters(List<EditorToolItemFilter<EditorToolStaticObjectsItem>> filters)
        {
            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Vegetation",
                    this.GetFilterTexture("Vegetation"),
                    _ => _.ProtoStaticObject is IProtoObjectVegetation));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Resources",
                    this.GetFilterTexture("Resources"),
                    _ => _.ProtoStaticObject is IProtoObjectMineral));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Structures",
                    this.GetFilterTexture("Structures"),
                    _ => _.ProtoStaticObject is IProtoObjectStructure protoStructure
                         && !(protoStructure is IProtoObjectFloor)));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Props",
                    this.GetFilterTexture("Props"),
                    _ => _.ProtoStaticObject is ProtoObjectProp));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Floors",
                    this.GetFilterTexture("Floors"),
                    _ => _.ProtoStaticObject is IProtoObjectFloor));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Loot",
                    this.GetFilterTexture("Loot"),
                    _ => _.ProtoStaticObject is IProtoObjectLoot));

            filters.Add(
                new EditorToolItemFilter<EditorToolStaticObjectsItem>(
                    "Misc",
                    this.GetFilterTexture("Misc"),
                    o => !(o.ProtoStaticObject is IProtoObjectStructure)
                         && !(o.ProtoStaticObject is IProtoObjectVegetation)
                         && !(o.ProtoStaticObject is IProtoObjectMineral)
                         && !(o.ProtoStaticObject is ProtoObjectProp)
                         && !(o.ProtoStaticObject is IProtoObjectLoot)));
        }

        protected override void SetupItems(List<EditorToolStaticObjectsItem> items)
        {
            this.viewModelsDictionary.Clear();
            var protoStaticWorldObjects = Api.FindProtoEntities<IProtoStaticWorldObject>();

            foreach (var prototype in protoStaticWorldObjects
                                      .OrderBy(t => t.Kind)
                                      .ThenBy(t => t.ShortId))
            {
                if (!(prototype is ObjectGroundItemsContainer)
                    && !(prototype is ProtoObjectConstructionSite)
                    && !(prototype is ObjectCorpse))
                {
                    var item = new EditorToolStaticObjectsItem(prototype);
                    items.Add(item);
                }
            }
        }

        private void ClientPlaceStaticObject(
            List<Vector2Ushort> tilePositions,
            IProtoStaticWorldObject protoStaticWorldObject)
        {
            var tilePosition = tilePositions[0];
            if (Client.World.GetTile(tilePosition)
                      .StaticObjects.Any(so => so.ProtoStaticWorldObject == protoStaticWorldObject))
            {
                return;
            }

            EditorClientActionsHistorySystem.DoAction(
                $"Place object \"{protoStaticWorldObject.Name}\"",
                onDo: () => this.CallServer(
                          _ => _.ServerRemote_PlaceStaticObject(protoStaticWorldObject, tilePosition)),
                onUndo: () => this.CallServer(_ => _.ServerRemote_Destroy(protoStaticWorldObject, tilePosition)),
                canGroupWithPreviousAction: true);
        }

        private ITextureResource GetFilterTexture(string textureName)
        {
            return new TextureResource($"{this.ToolTexturesPath}Filters/{textureName}.png");
        }

        [RemoteCallSettings(DeliveryMode.Default,
                            timeInterval: 0,
                            clientMaxSendQueueSize: byte.MaxValue)]
        private void ServerRemote_Destroy(IProtoStaticWorldObject protoStaticWorldObject, Vector2Ushort tilePosition)
        {
            var worldService = Server.World;
            var tile = worldService.GetTile(tilePosition);
            foreach (var staticObject in tile.StaticObjects
                                             .Where(so => so.ProtoStaticWorldObject == protoStaticWorldObject)
                                             .ToList())
            {
                worldService.DestroyObject(staticObject);
            }
        }

        [RemoteCallSettings(DeliveryMode.Default,
                            timeInterval: 0,
                            clientMaxSendQueueSize: byte.MaxValue)]
        private void ServerRemote_PlaceStaticObject(
            IProtoStaticWorldObject protoStaticWorldObject,
            Vector2Ushort tilePosition)
        {
            if (!protoStaticWorldObject.CheckTileRequirements(tilePosition, character: null, logErrors: true))
            {
                // cannot place here
                return;
            }

            var worldObject = Server.World.CreateStaticWorldObject(protoStaticWorldObject, tilePosition);
            if (protoStaticWorldObject is IProtoObjectVegetation protoVegetation)
            {
                protoVegetation.ServerSetFullGrown(worldObject);
            }
        }

        private void WorldObjectEnterScopeHandler(IGameObjectWithProto obj)
        {
            if (this.viewModelsDictionary.TryGetValue(obj.ProtoGameObject, out var weakReference)
                && weakReference.TryGetTarget(out var item)
                && !item.IsDisposed)
            {
                item.Count++;
            }
        }

        private void WorldObjectLeftScopeHandler(IGameObjectWithProto obj)
        {
            if (this.viewModelsDictionary.TryGetValue(obj.ProtoGameObject, out var weakReference)
                && weakReference.TryGetTarget(out var item)
                && !item.IsDisposed)
            {
                item.Count--;
            }
        }
    }
}