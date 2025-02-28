﻿namespace AtomicTorch.CBND.CoreMod.UI.Controls.Game.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using AtomicTorch.CBND.CoreMod.ClientComponents.Input;
    using AtomicTorch.CBND.CoreMod.Helpers.Client;
    using AtomicTorch.CBND.CoreMod.Systems.Chat;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Chat.Data;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Menu;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Menu.Options.Data;
    using AtomicTorch.CBND.CoreMod.UI.Services;
    using AtomicTorch.CBND.GameApi.Data.Logic;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient;
    using AtomicTorch.GameEngine.Common.Client.MonoGame.UI;

    public partial class ChatPanel : BaseUserControl
    {
        private readonly Dictionary<BaseChatRoom, ChatRoomTab> chatRooms
            = new();

        private bool? isActive;

        private bool isTextBlockPressKeyToOpenChatTimeoutElapsed;

        private ClientInputContext openedChatInputContext;

        private TabControlCached tabControl;

        private ContentControl tabItemsHolder;

        private ScrollViewer tabsScrollViewer;

        private TextBlock textBlockPressKeyToOpenChat;

        public static ChatPanel Instance { get; private set; }

        public bool IsActive
        {
            get => this.isActive ?? false;
            private set
            {
                if (value && !CanOpen)
                {
                    value = false;
                }

                if (this.isActive == value)
                {
                    return;
                }

                if (value
                    && !ClientChatDisclaimerConfirmationHelper.IsChatAllowedForCurrentServer)
                {
                    // don't allow opening chat until player confirms the disclaimer
                    ClientChatDisclaimerConfirmationHelper.OpenDisclaimerDialogIfNecessary(
                        askAgain: true);
                    return;
                }

                this.isActive = value;
                this.Background = this.isActive.Value
                                      ? Brushes.Transparent
                                      : null;

                this.IsHitTestVisible
                    = this.tabControl.IsHitTestVisible
                          = this.isActive.Value;

                if (this.isActive.Value)
                {
                    // ReSharper disable once CanExtractXamlLocalizableStringCSharp
                    this.openedChatInputContext =
                        ClientInputContext.Start("Chat panel - intercept all other game input")
                                          .HandleAll(
                                              () =>
                                              {
                                                  if (ClientInputManager.IsButtonDown(
                                                      GameButton.CancelOrClose,
                                                      evenIfHandled: true))
                                                  {
                                                      this.IsActive = false;
                                                  }

                                                  ClientInputManager.ConsumeAllButtons();
                                              });

                    foreach (var chatRoomTab in this.chatRooms.Values)
                    {
                        chatRoomTab.ViewModelChatRoom.SetIsTabVisibleInPanel(true);
                        chatRoomTab.ChatRoomControl.IsActive = true;
                    }

                    ClientChatDisclaimerConfirmationHelper.OpenDisclaimerDialogIfNecessary();
                    this.RefreshTextBlockPressKeyToOpenVisibility();
                }
                else
                {
                    // stop catching input after a short delay to prevent firing weapon on chat close
                    var clientInputContext = this.openedChatInputContext;
                    this.openedChatInputContext = null;
                    ClientTimersSystem.AddAction(
                        delaySeconds: 0.1,
                        action: () => clientInputContext?.Stop());

                    foreach (var chatRoomTab in this.chatRooms.Values)
                    {
                        chatRoomTab.ViewModelChatRoom.SetIsTabVisibleInPanel(false);
                        chatRoomTab.ChatRoomControl.IsActive = false;
                    }

                    this.textBlockPressKeyToOpenChat.Visibility = Visibility.Collapsed;
                }

                this.tabsScrollViewer.ScrollToEnd();
            }
        }

        private static bool CanOpen
            => ClientCurrentCharacterHelper.Character is not null
               && ClientCurrentCharacterHelper.Character.IsInitialized
               && !ClientCurrentCharacterHelper.PublicState.IsDead
               && !ClientCurrentCharacterHelper.PrivateState.IsDespawned;

        public void Close()
        {
            this.IsActive = false;
        }

        public void Open()
        {
            this.IsActive = true;
        }

        public void OpenChat(BaseChatRoom chatRoom)
        {
            if (!this.chatRooms.TryGetValue(chatRoom, out var chatRoomTab))
            {
                return;
            }

            this.IsActive = true;
            chatRoomTab.TabItem.IsSelected = true;
        }

        public void RefreshState()
        {
            if (!this.isLoaded)
            {
                return;
            }

            this.textBlockPressKeyToOpenChat.Visibility = Visibility.Collapsed;

            this.tabItemsHolder.Visibility =
                ClientChatDisclaimerConfirmationHelper.IsNeedToDisplayDisclaimerForCurrentServer
                    ? Visibility.Hidden
                    : Visibility.Visible;

            if (ClientChatDisclaimerConfirmationHelper.IsChatAllowedForCurrentServer
                || ClientChatDisclaimerConfirmationHelper.IsNeedToDisplayDisclaimerForCurrentServer)
            {
                // display the chat panel when chat is allowed or when player didn't approved/disallowed chat usage yet
                this.Visibility = Visibility.Visible;
                return;
            }

            this.IsActive = false;
            this.Visibility = Visibility.Hidden;
        }

        public void SelectNextTab()
        {
            var items = this.tabControl.Items;
            var index = this.tabControl.SelectedIndex;
            var maxIndex = items.Count;

            do
            {
                index++;
                if (index >= maxIndex)
                {
                    index = 0;
                }

                if (((TabItem)items[index]).Visibility == Visibility.Visible)
                {
                    break;
                }
            }
            while (true);

            this.tabControl.SelectedIndex = index;
        }

        public void SelectPreviousTab()
        {
            var items = this.tabControl.Items;
            var index = this.tabControl.SelectedIndex;
            var maxIndex = items.Count;

            do
            {
                index--;
                if (index < 0)
                {
                    index = maxIndex - 1;
                }

                if (((TabItem)items[index]).Visibility == Visibility.Visible)
                {
                    break;
                }
            }
            while (true);

            this.tabControl.SelectedIndex = index;
        }

        public void SelectTab<TChatRoom>()
            where TChatRoom : BaseChatRoom
        {
            foreach (var chatRoomTab in this.chatRooms.Values)
            {
                if (chatRoomTab.ChatRoom is TChatRoom)
                {
                    this.tabControl.SelectedItem = chatRoomTab.TabItem;
                }
            }
        }

        protected override void InitControl()
        {
            this.tabControl = this.GetByName<TabControlCached>("TabControl");
            var visualRoot = (FrameworkElement)VisualTreeHelper.GetChild(this.tabControl, 0);
            this.tabsScrollViewer = visualRoot.GetByName<ScrollViewer>("TabsScrollViewer");
            this.tabItemsHolder = visualRoot.GetByName<ContentControl>("TabItemsHolder");
            this.textBlockPressKeyToOpenChat = visualRoot.GetByName<TextBlock>("TextBlockPressKeyToOpen");

            this.IsActive = false;
            Instance = this;
        }

        protected override void OnLoaded()
        {
            ChatSystem.ClientChatRoomAdded += this.ChatRoomAddedHandler;
            ChatSystem.ClientChatRoomRemoved += this.ChatRoomRemovedHandler;
            ChatSystem.ClientChatRoomMessageReceived += this.ClientChatRoomMessageReceivedHandler;
            ClientInputManager.ButtonKeyMappingUpdated += this.ClientInputManagerButtonKeyMappingUpdatedHandler;
            Api.Client.CurrentGame.ConnectionStateChanged += this.CurrentGameOnConnectionStateChangedHandler;
            Api.Client.UI.LayoutRoot.PreviewMouseDown += this.LayoutRootPreviewMouseButtonDownHandler;
            ClientUpdateHelper.UpdateCallback += this.Update;

            this.RefreshState();

            var allChatRoomHolders = new List<ILogicObject>();
            Api.GetProtoEntity<ChatRoomHolder>().GetAllGameObjects(allChatRoomHolders);

            foreach (var chatRoomHolder in allChatRoomHolders)
            {
                if (!chatRoomHolder.IsInitialized)
                {
                    continue;
                }

                var chatRoom = ChatSystem.SharedGetChatRoom(chatRoomHolder);
                this.ChatRoomAddedHandler(chatRoom);
            }
        }

        protected override void OnUnloaded()
        {
            ChatSystem.ClientChatRoomAdded -= this.ChatRoomAddedHandler;
            ChatSystem.ClientChatRoomRemoved -= this.ChatRoomRemovedHandler;
            ChatSystem.ClientChatRoomMessageReceived -= this.ClientChatRoomMessageReceivedHandler;
            ClientInputManager.ButtonKeyMappingUpdated -= this.ClientInputManagerButtonKeyMappingUpdatedHandler;
            Api.Client.CurrentGame.ConnectionStateChanged -= this.CurrentGameOnConnectionStateChangedHandler;
            Api.Client.UI.LayoutRoot.PreviewMouseDown -= this.LayoutRootPreviewMouseButtonDownHandler;
            ClientUpdateHelper.UpdateCallback -= this.Update;
        }

        private static int CompareTabs(TabItem tabA, TabItem tabB)
        {
            var chatRoomA = (ViewModelChatRoom)tabA.DataContext;
            var chatRoomB = (ViewModelChatRoom)tabB.DataContext;
            var orderA = GetTabOrder(chatRoomA);
            var orderB = GetTabOrder(chatRoomB);
            if (orderA != orderB)
            {
                return orderB.CompareTo(orderA);
            }

            static int GetTabOrder(ViewModelChatRoom viewModelChatRoom)
            {
                return viewModelChatRoom.ChatRoom switch
                {
                    ChatRoomGlobal  => 0,
                    ChatRoomLocal   => 1,
                    ChatRoomTrade   => 2,
                    ChatRoomParty   => 3,
                    ChatRoomFaction => 4,
                    _               => int.MaxValue
                };
            }

            var chatLogA = chatRoomA.ChatRoom.ChatLog;
            var chatLogB = chatRoomB.ChatRoom.ChatLog;
            var lastDateA = chatLogA.Count > 0 ? chatLogA.Last().UtcDate : DateTime.MinValue;
            var lastDateB = chatLogB.Count > 0 ? chatLogB.Last().UtcDate : DateTime.MinValue;
            return lastDateA.CompareTo(lastDateB);
        }

        private void ChatRoomAddedHandler(BaseChatRoom chatRoom)
        {
            if (chatRoom is ChatRoomPrivate privateChatRoom)
            {
                if (ClientChatBlockList.IsBlocked(privateChatRoom.CharacterA)
                    || ClientChatBlockList.IsBlocked(privateChatRoom.CharacterB))
                {
                    // chat room with blocked player - ignore it
                    return;
                }

                if (privateChatRoom.ClientIsUnreadByCurrentCharacter())
                {
                    this.RefreshTextBlockPressKeyToOpenVisibility();
                }
            }

            var viewModelChatRoom = new ViewModelChatRoom(
                chatRoom,
                callbackNeedTabSort:
                () => this.tabControl.SortTabs(CompareTabs),
                callbackPrivateChatRoomClosed:
                vm =>
                {
                    if (vm.IsSelected)
                    {
                        // selected private chat room closed - switch to global chat
                        this.SelectTab<ChatRoomGlobal>();
                    }
                });

            var chatRoomControl = new ChatRoomControl()
            {
                ViewModelChatRoom = viewModelChatRoom,
                ChatPanel = this
            };

            var tabItem = new TabItem()
            {
                Header = chatRoom.ClientGetTitle(),
                Content = chatRoomControl,
                DataContext = viewModelChatRoom
            };

            this.chatRooms.Add(chatRoom,
                               new ChatRoomTab(chatRoom, chatRoomControl, tabItem, viewModelChatRoom));

            this.tabControl.AddItem(tabItem);

            if (this.IsActive)
            {
                viewModelChatRoom.SetIsTabVisibleInPanel(true);
                chatRoomControl.IsActive = true;
            }
            else if (viewModelChatRoom.HasUnreadMessages)
            {
                viewModelChatRoom.SetIsTabVisibleInPanel(true);
            }

            if (chatRoom is ChatRoomGlobal)
            {
                tabItem.IsSelected = true;
            }

            this.tabControl.SortTabs(CompareTabs);
        }

        private void ChatRoomRemovedHandler(BaseChatRoom chatRoom)
        {
            if (!this.chatRooms.TryGetValue(chatRoom, out var chatRoomTab))
            {
                return;
            }

            this.chatRooms.Remove(chatRoom);
            this.tabControl.Remove(chatRoomTab.TabItem);

            chatRoomTab.TabItem.DataContext = null;
            chatRoomTab.ViewModelChatRoom.Dispose();
        }

        private void ClientChatRoomMessageReceivedHandler(BaseChatRoom chatroom, in ChatEntry chatentry)
        {
            this.RefreshTextBlockPressKeyToOpenVisibility();
        }

        private void ClientInputManagerButtonKeyMappingUpdatedHandler(IWrappedButton obj)
        {
            if (obj == WrappedButton<GameButton>.GetWrappedButton(GameButton.OpenChat))
            {
                this.UpdateTextBlockPressKeyToOpen();
            }
        }

        private void CurrentGameOnConnectionStateChangedHandler()
        {
            if (Api.Client.CurrentGame.ConnectionState == ConnectionState.Connected)
            {
                this.RefreshState();
            }
        }

        private void LayoutRootPreviewMouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (this.tabControl.IsMouseOver
                || (EmojiPickerPopupControl.Instance?.IsMouseOver ?? false))
            {
                // ignore
                return;
            }

            // close chats
            this.IsActive = false;
        }

        private void RefreshTextBlockPressKeyToOpenVisibility()
        {
            if (!this.isLoaded)
            {
                return;
            }

            if (this.isTextBlockPressKeyToOpenChatTimeoutElapsed)
            {
                this.textBlockPressKeyToOpenChat.Visibility = Visibility.Collapsed;
                return;
            }

            if (ClientChatDisclaimerConfirmationHelper.IsNeedToDisplayDisclaimerForCurrentServer)
            {
                this.textBlockPressKeyToOpenChat.Visibility = Visibility.Visible;
                this.UpdateTextBlockPressKeyToOpen();

                // auto hide after timeout
                ClientTimersSystem.AddAction(60,
                                             () =>
                                             {
                                                 if (ReferenceEquals(this, Instance))
                                                 {
                                                     this.isTextBlockPressKeyToOpenChatTimeoutElapsed = true;
                                                     this.RefreshTextBlockPressKeyToOpenVisibility();
                                                 }
                                             });
            }
            else
            {
                this.textBlockPressKeyToOpenChat.Visibility = Visibility.Collapsed;
            }
        }

        private void Update()
        {
            if (!this.IsActive
                || WindowsManager.OpenedWindowsCount > 0
                || !MainMenuOverlay.IsHidden)
            {
                return;
            }

            if (!CanOpen)
            {
                this.IsActive = false;
                return;
            }

            foreach (var entry in this.chatRooms)
            {
                entry.Value.ChatRoomControl.FocusInputIfNecessary();
            }
        }

        private void UpdateTextBlockPressKeyToOpen()
        {
            if (this.textBlockPressKeyToOpenChat.Visibility != Visibility.Visible)
            {
                return;
            }

            this.textBlockPressKeyToOpenChat.Text
                = string.Format(
                    CoreStrings.Chat_PressKeyToOpen_Format,
                    InputKeyNameHelper.GetKeyText(
                        ClientInputManager.GetKeyForButton(GameButton.OpenChat)));
        }

        private class ChatRoomTab
        {
            public ChatRoomTab(
                BaseChatRoom chatRoom,
                ChatRoomControl chatRoomControl,
                TabItem tabItem,
                ViewModelChatRoom viewModelChatRoom)
            {
                this.ChatRoom = chatRoom;
                this.ChatRoomControl = chatRoomControl;
                this.TabItem = tabItem;
                this.ViewModelChatRoom = viewModelChatRoom;
            }

            public BaseChatRoom ChatRoom { get; }

            public ChatRoomControl ChatRoomControl { get; }

            public TabItem TabItem { get; }

            public ViewModelChatRoom ViewModelChatRoom { get; }
        }
    }
}