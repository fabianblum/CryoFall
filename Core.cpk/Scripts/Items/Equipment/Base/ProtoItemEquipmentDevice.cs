﻿namespace AtomicTorch.CBND.CoreMod.Items.Equipment
{
    using System.Collections.Generic;
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.State;

    /// <summary>
    /// Item prototype for device equipment.
    /// </summary>
    public abstract class ProtoItemEquipmentDevice
        <TPrivateState,
         TPublicState,
         TClientState>
        : ProtoItemEquipment
          <TPrivateState,
              TPublicState,
              TClientState>,
          IProtoItemEquipmentDevice
        where TPrivateState : BasePrivateState, IItemWithDurabilityPrivateState, new()
        where TPublicState : BasePublicState, new()
        where TClientState : BaseClientState, new()
    {
        public const string NotificationCannotUseDirectly_Message =
            "All devices are used automatically when equipped in the device slot.";

        public const string NotificationCannotUseDirectly_Title = "Cannot use directly";

        public sealed override EquipmentType EquipmentType => EquipmentType.Device;

        public abstract bool OnlySingleDeviceOfThisProtoAppliesEffect { get; }

        public override bool RequireEquipmentTexturesFemale => false;

        public override bool RequireEquipmentTexturesMale => false;

        protected override double DefenseMultiplier => 0;

        public override void ServerOnItemDamaged(IItem item, double damageApplied)
        {
            // no durability degradation
        }

        public override bool SharedCanApplyEffects(IItem item, IItemsContainer containerEquipment)
        {
            if (!this.OnlySingleDeviceOfThisProtoAppliesEffect)
            {
                return true;
            }

            var firstEquippedDevice = containerEquipment.GetItemsOfProto(this)
                                                        .FirstOrDefault();
            return item == firstEquippedDevice;
        }

        protected sealed override bool ClientItemUseFinish(ClientItemData data)
        {
            NotificationSystem.ClientShowNotification(
                NotificationCannotUseDirectly_Title,
                NotificationCannotUseDirectly_Message,
                NotificationColor.Bad,
                this.Icon);
            return false;
        }

        protected sealed override void ClientItemUseStart(ClientItemData data)
        {
        }

        protected override string GenerateIconPath()
        {
            return "Items/Devices/" + this.GetType().Name;
        }

        protected sealed override void PrepareDefense(DefenseDescription defense)
        {
            // no defenses
        }

        protected override void PrepareHints(List<string> hints)
        {
            base.PrepareHints(hints);
            hints.Add(ItemHints.EquipIntoDeviceSlots);
        }

        protected override byte[] SharedGetCompatibleContainerSlotsIds()
        {
            // allow placing devices in these slots
            return new byte[]
            {
                (byte)EquipmentType.Device,
                (byte)EquipmentType.Device + 1,
                (byte)EquipmentType.Device + 2,
                (byte)EquipmentType.Device + 3,
                (byte)EquipmentType.Device + 4
            };
        }
    }

    /// <summary>
    /// Item prototype for device equipment.
    /// </summary>
    public abstract class ProtoItemEquipmentDevice
        : ProtoItemEquipmentDevice
            <ItemWithDurabilityPrivateState,
                EmptyPublicState,
                EmptyClientState>
    {
    }
}