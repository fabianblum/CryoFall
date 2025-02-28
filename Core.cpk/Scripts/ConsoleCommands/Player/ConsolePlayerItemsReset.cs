﻿// ReSharper disable CanExtractXamlLocalizableStringCSharp

namespace AtomicTorch.CBND.CoreMod.ConsoleCommands.Player
{
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.Systems.Console;
    using AtomicTorch.CBND.GameApi.Data.Characters;

    public class ConsolePlayerItemsReset : BaseConsoleCommand
    {
        public override string Description => "Removes all items from a given player character.";

        public override ConsoleCommandKinds Kind => ConsoleCommandKinds.ServerOperator;

        public override string Name => "player.items.reset";

        public string Execute([CurrentCharacterIfNull] ICharacter player = null)
        {
            foreach (var container in new[]
            {
                player.SharedGetPlayerContainerInventory(),
                player.SharedGetPlayerContainerEquipment(),
                player.SharedGetPlayerContainerHotbar(),
                player.SharedGetPlayerContainerHand()
            })
            {
                foreach (var item in container.Items.ToList())
                {
                    Server.Items.DestroyItem(item);
                }
            }

            return $"All items were removed from {player}.";
        }
    }
}