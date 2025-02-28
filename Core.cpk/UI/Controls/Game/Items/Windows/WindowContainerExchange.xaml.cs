﻿namespace AtomicTorch.CBND.CoreMod.UI.Controls.Game.Items.Windows
{
    using AtomicTorch.CBND.CoreMod.ClientComponents.StaticObjects;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Items.Windows.Data;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Client.MonoGame.UI;

    public partial class WindowContainerExchange : BaseUserControlWithWindow
    {
        private IItemsContainer itemsContainer;

        private ViewModelWindowContainerExchange viewModel;

        public static WindowContainerExchange Show(
            IItemsContainer itemsContainer,
            SoundUI soundOpen = null,
            SoundUI soundClose = null,
            bool isAutoClose = false)
        {
            var instance = new WindowContainerExchange();
            instance.Setup(itemsContainer, soundOpen, soundClose);
            Api.Client.UI.LayoutRootChildren.Add(instance);

            if (isAutoClose)
            {
                ((IWorldObject)itemsContainer.Owner)
                    .ClientSceneObject
                    .AddComponent<ClientComponentAutoCloseWindow>()
                    .Setup(instance,
                           closeCheckFunc: () => itemsContainer.OccupiedSlotsCount == 0);
            }

            return instance;
        }

        public void Close()
        {
            this.CloseWindow();
        }

        protected override void InitControlWithWindow()
        {
            // TODO: redone this to cached window when NoesisGUI implement proper Storyboard.Completed triggers
            this.Window.IsCached = false;
        }

        protected override void WindowClosing()
        {
            this.DestroyViewModel();
        }

        protected override void WindowOpening()
        {
            this.RefreshViewModel();
        }

        private void DestroyViewModel()
        {
            if (this.viewModel is null)
            {
                return;
            }

            this.DataContext = null;
            this.viewModel.Dispose();
            this.viewModel = null;
        }

        private void RefreshViewModel()
        {
            if (this.WindowState != GameWindowState.Opened
                && this.WindowState != GameWindowState.Opening
                || this.itemsContainer is null)
            {
                return;
            }

            if (this.viewModel is not null)
            {
                if (this.viewModel.ViewModel.Container == this.itemsContainer)
                {
                    // already displaying window for this container
                    return;
                }

                this.DestroyViewModel();
            }

            this.viewModel = new ViewModelWindowContainerExchange(this.itemsContainer, this.Close);
            this.DataContext = this.viewModel;
        }

        private void Setup(IItemsContainer container, SoundUI soundOpen, SoundUI soundClose)
        {
            this.itemsContainer = container;

            if (!ReferenceEquals(soundOpen,     null)
                || !ReferenceEquals(soundClose, null))
            {
                var window = this.GetByName<WindowMenuWithInventory>("WindowMenuWithInventory");
                if (!ReferenceEquals(soundOpen, null))
                {
                    window.SoundOpening = soundOpen;
                }

                if (!ReferenceEquals(soundClose, null))
                {
                    window.SoundClosing = soundClose;
                }
            }

            this.RefreshViewModel();
        }
    }
}