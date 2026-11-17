// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.TypingIndicator;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Ghost;
using Content.Client.Mind;
using Content.Client.Roles;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Combat;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Starlight.CollectiveMind; // Goobstation - Starlight collective mind port
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Decals;
using Content.Shared.Input;
using Content.Shared.Radio;
using Content.Shared.Roles.RoleCodeword;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Graphics.RSI;


namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController : UIController
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _manager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

    [UISystemDependency] private readonly ExamineSystem? _examine = default;
    [UISystemDependency] private readonly GhostSystem? _ghost = default;
    [UISystemDependency] private readonly CollectiveMindSystem? _collectiveMind = default!; // Goobstation - Starlight collective mind port
    [UISystemDependency] private readonly TypingIndicatorSystem? _typingIndicator = default;
    [UISystemDependency] private readonly ChatSystem? _chatSys = default;
    [UISystemDependency] private readonly PsionicChatUpdateSystem? _psionic = default!; //Nyano - Summary: makes the psionic chat available.
    [UISystemDependency] private readonly TransformSystem? _transform = default;
    [UISystemDependency] private readonly MindSystem? _mindSystem = default!;
    [UISystemDependency] private readonly RoleCodewordSystem? _roleCodewordSystem = default!;

    private static readonly ProtoId<ColorPalettePrototype> ChatNamePalette = "ChatNames";
    private string[] _chatNameColors = default!;
    private bool _chatNameColorsEnabled;

    private ISawmill _sawmill = default!;

    public static readonly Dictionary<char, ChatSelectChannel> PrefixToChannel = new()
    {
        {SharedChatSystem.LocalPrefix, ChatSelectChannel.Local},
        {SharedChatSystem.WhisperPrefix, ChatSelectChannel.Whisper},
        {SharedChatSystem.ConsolePrefix, ChatSelectChannel.Console},
        {SharedChatSystem.LOOCPrefix, ChatSelectChannel.LOOC},
        {SharedChatSystem.OOCPrefix, ChatSelectChannel.OOC},
        {SharedChatSystem.EmotesPrefix, ChatSelectChannel.Emotes},
        {SharedChatSystem.EmotesAltPrefix, ChatSelectChannel.Emotes},
        {SharedChatSystem.AdminPrefix, ChatSelectChannel.Admin},
        {SharedChatSystem.RadioCommonPrefix, ChatSelectChannel.Radio},
        {SharedChatSystem.DeadPrefix, ChatSelectChannel.Dead},
        {SharedChatSystem.TelepathicPrefix, ChatSelectChannel.Telepathic}, //Nyano - Summary: adds the telepathic prefix =.
        {SharedChatSystem.CollectiveMindPrefix, ChatSelectChannel.CollectiveMind} // Goobstation - Starlight collective mind port
    };

    public static readonly Dictionary<ChatSelectChannel, char> ChannelPrefixes = new()
    {
        {ChatSelectChannel.Local, SharedChatSystem.LocalPrefix},
        {ChatSelectChannel.Whisper, SharedChatSystem.WhisperPrefix},
        {ChatSelectChannel.Console, SharedChatSystem.ConsolePrefix},
        {ChatSelectChannel.LOOC, SharedChatSystem.LOOCPrefix},
        {ChatSelectChannel.OOC, SharedChatSystem.OOCPrefix},
        {ChatSelectChannel.Emotes, SharedChatSystem.EmotesPrefix},
        {ChatSelectChannel.Admin, SharedChatSystem.AdminPrefix},
        {ChatSelectChannel.Radio, SharedChatSystem.RadioCommonPrefix},
        {ChatSelectChannel.Dead, SharedChatSystem.DeadPrefix},
        {ChatSelectChannel.Telepathic, SharedChatSystem.TelepathicPrefix }, //Nyano - Summary: associates telepathic with =.
        {ChatSelectChannel.CollectiveMind, SharedChatSystem.CollectiveMindPrefix} // Goobstation - Starlight collective mind port
    };

    /// <summary>
    ///     The max amount of chars allowed to fit in a single speech bubble.
    /// </summary>
    private const int SingleBubbleCharLimit = 100;

    /// <summary>
    ///     Base queue delay each speech bubble has.
    /// </summary>
    private const float BubbleDelayBase = 0.2f;

    /// <summary>
    ///     Factor multiplied by speech bubble char length to add to delay.
    /// </summary>
    private const float BubbleDelayFactor = 0.8f / SingleBubbleCharLimit;

    /// <summary>
    ///     The max amount of speech bubbles over a single entity at once.
    /// </summary>
    private const int SpeechBubbleCap = 4;

    private LayoutContainer _speechBubbleRoot = default!;

    /// <summary>
    ///     Speech bubbles that are currently visible on screen.
    ///     We track them to push them up when new ones get added.
    /// </summary>
    private readonly Dictionary<EntityUid, List<SpeechBubble>> _activeSpeechBubbles =
        new();

    /// <summary>
    ///     Speech bubbles that are to-be-sent because of the "rate limit" they have.
    /// </summary>
    private readonly Dictionary<EntityUid, SpeechBubbleQueueData> _queuedSpeechBubbles
        = new();

    private readonly HashSet<ChatBox> _chats = new();
    private CombatPanelWindow? _combatPanelWindow;
    private bool _combatPanelModeEnabled;
    private TimeSpan _combatPanelRefreshTimer;
    private TimeSpan _medicalRefreshTimer;
    private EntityUid? _pendingMedicalUseUser;
    private EntityUid? _pendingMedicalUseItem;
    private float _pendingMedicalUseDelay;
    private EntityUid? _pendingAmmoReloadUser;
    private EntityUid? _pendingAmmoReloadItem;
    private EntityUid? _pendingAmmoReloadGun;
    private float _pendingAmmoReloadDelay;
    private EntityUid? _healthDollEntity;
    private Dictionary<TargetBodyPart, WoundableSeverity>? _lastBodyStatus;
    private static readonly EntProtoId HealthDollPrototype = "AlertSpriteView";

    public bool CombatPanelModeEnabled => _combatPanelModeEnabled;

    public bool ToggleCombatPanelMode()
    {
        _combatPanelModeEnabled = !_combatPanelModeEnabled;
        return _combatPanelModeEnabled;
    }
    public IReadOnlySet<ChatBox> Chats => _chats;

    /// <summary>
    ///     The max amount of characters an entity can send in one message
    /// </summary>
    public int MaxMessageLength => _config.GetCVar(CCVars.ChatMaxMessageLength);

    /// <summary>
    /// For currently disabled chat filters,
    /// unread messages (messages received since the channel has been filtered out).
    /// </summary>
    private readonly Dictionary<ChatChannel, int> _unreadMessages = new();


    // Goobstation - Chat Pings
    /// <summary>
    /// Used for Goobstation's - Chat Pings
    /// </summary>
    private TimeSpan LastHighlightTime = TimeSpan.Zero;

    // TODO add a cap for this for non-replays
    public readonly List<(GameTick Tick, ChatMessage Msg)> History = new();

    // Maintains which channels a client should be able to filter (for showing in the chatbox)
    // and select (for attempting to send on).
    // This may not always actually match with what the server will actually allow them to
    // send / receive on, it is only what the user can select in the UI. For example,
    // if a user is silenced from speaking for some reason this may still contain ChatChannel.Local, it is left up
    // to the server to handle invalid attempts to use particular channels and not send messages for
    // channels the user shouldn't be able to hear.
    //
    // Note that Command is an available selection in the chatbox channel selector,
    // which is not actually a chat channel but is always available.
    public ChatSelectChannel CanSendChannels { get; private set; }
    public ChatChannel FilterableChannels { get; private set; }
    public ChatSelectChannel SelectableChannels { get; private set; }
    private ChatSelectChannel PreferredChannel { get; set; } = ChatSelectChannel.OOC;

    public event Action<ChatSelectChannel>? CanSendChannelsChanged;
    public event Action<ChatChannel>? FilterableChannelsChanged;
    public event Action<ChatSelectChannel>? SelectableChannelsChanged;
    public event Action<ChatChannel, int?>? UnreadMessageCountsUpdated;
    public event Action<ChatMessage>? MessageAdded;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
        _admin.AdminStatusUpdated += UpdateChannelPermissions; // Goobstation - Starlight collective mind port
        _manager.PermissionsUpdated += UpdateChannelPermissions;
        _player.LocalPlayerAttached += OnAttachedChanged;
        _player.LocalPlayerDetached += OnAttachedChanged;
        _state.OnStateChanged += StateChanged;
        _net.RegisterNetMessage<MsgChatMessage>(OnChatMessage);
        _net.RegisterNetMessage<MsgDeleteChatMessagesBy>(OnDeleteChatMessagesBy);
        SubscribeNetworkEvent<DamageForceSayEvent>(OnDamageForceSay);
        _config.OnValueChanged(CCVars.ChatEnableColorName, (value) => { _chatNameColorsEnabled = value; });
        _chatNameColorsEnabled = _config.GetCVar(CCVars.ChatEnableColorName);

        _speechBubbleRoot = new LayoutContainer();

        UpdateChannelPermissions();

        _input.SetInputCommand(ContentKeyFunctions.FocusChat,
            InputCmdHandler.FromDelegate(_ => FocusChat()));

        _input.SetInputCommand(ContentKeyFunctions.FocusLocalChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Local)));

        _input.SetInputCommand(ContentKeyFunctions.FocusEmote,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Emotes)));

        _input.SetInputCommand(ContentKeyFunctions.FocusWhisperChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Whisper)));

        _input.SetInputCommand(ContentKeyFunctions.FocusLOOC,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.LOOC)));

        _input.SetInputCommand(ContentKeyFunctions.FocusOOC,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.OOC)));

        _input.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Admin)));

        _input.SetInputCommand(ContentKeyFunctions.FocusRadio,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Radio)));

        _input.SetInputCommand(ContentKeyFunctions.FocusDeadChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Dead)));

        _input.SetInputCommand(ContentKeyFunctions.FocusConsoleChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Console)));

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(true)));

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(false)));

        // Goobstation - Starlight collective mind port
        _input.SetInputCommand(ContentKeyFunctions.FocusCollectiveMindChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.CollectiveMind)));

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        var nameColors = _prototypeManager.Index(ChatNamePalette).Colors.Values.ToArray();
        _chatNameColors = new string[nameColors.Length];
        for (var i = 0; i < nameColors.Length; i++)
        {
            _chatNameColors[i] = nameColors[i].ToHex();
        }

        _config.OnValueChanged(CCVars.ChatWindowOpacity, OnChatWindowOpacityChanged);

        InitializeHighlights();
    }

    public void OnScreenLoad()
    {
        SetMainChat(true);

        var viewportContainer = UIManager.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");
        SetSpeechBubbleRoot(viewportContainer);

        SetChatWindowOpacity(_config.GetCVar(CCVars.ChatWindowOpacity));
    }

    public void OnScreenUnload()
    {
        SetMainChat(false);
    }

    private void OnChatWindowOpacityChanged(float opacity)
    {
        SetChatWindowOpacity(opacity);
    }

    private void SetChatWindowOpacity(float opacity)
    {
        var chatBox = UIManager.ActiveScreen?.GetWidget<ChatBox>() ?? UIManager.ActiveScreen?.GetWidget<ResizableChatBox>();

        var panel = chatBox?.ChatWindowPanel;
        if (panel is null)
            return;

        Color color;
        if (panel.PanelOverride is StyleBoxFlat styleBoxFlat)
            color = styleBoxFlat.BackgroundColor;
        else if (panel.TryGetStyleProperty<StyleBox>(PanelContainer.StylePropertyPanel, out var style)
                 && style is StyleBoxFlat propStyleBoxFlat)
            color = propStyleBoxFlat.BackgroundColor;
        else
            color = Color.FromHex("#25252ADD");

        panel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = color.WithAlpha(opacity)
        };
    }

    public void SetMainChat(bool setting)
    {
        if (UIManager.ActiveScreen == null)
        {
            return;
        }

        ChatBox chatBox;
        string? chatSizeRaw;

        switch (UIManager.ActiveScreen)
        {
            case DefaultGameScreen defaultScreen:
                chatBox = defaultScreen.ChatBox;
                chatSizeRaw = _config.GetCVar(CCVars.DefaultScreenChatSize);
                SetChatSizing(chatSizeRaw, defaultScreen, setting);
                break;
            case SeparatedChatGameScreen separatedScreen:
                chatBox = separatedScreen.ChatBox;
                chatSizeRaw = _config.GetCVar(CCVars.SeparatedScreenChatSize);
                SetChatSizing(chatSizeRaw, separatedScreen, setting);
                break;
            default:
                // this could be better?
                var maybeChat = UIManager.ActiveScreen.GetWidget<ChatBox>();

                chatBox = maybeChat ?? throw new Exception("Cannot get chat box in screen!");

                break;
        }

        chatBox.Main = setting;
    }

    private void SetChatSizing(string sizing, InGameScreen screen, bool setting)
    {
        if (!setting)
        {
            screen.OnChatResized -= StoreChatSize;
            return;
        }

        screen.OnChatResized += StoreChatSize;

        if (string.IsNullOrEmpty(sizing))
        {
            return;
        }

        var split = sizing.Split(",");

        var chatSize = new Vector2(
            float.Parse(split[0], CultureInfo.InvariantCulture),
            float.Parse(split[1], CultureInfo.InvariantCulture));


        screen.SetChatSize(chatSize);
    }

    private void StoreChatSize(Vector2 size)
    {
        if (UIManager.ActiveScreen == null)
        {
            throw new Exception("Cannot get active screen!");
        }

        var stringSize =
            $"{size.X.ToString(CultureInfo.InvariantCulture)},{size.Y.ToString(CultureInfo.InvariantCulture)}";
        switch (UIManager.ActiveScreen)
        {
            case DefaultGameScreen _:
                _config.SetCVar(CCVars.DefaultScreenChatSize, stringSize);
                break;
            case SeparatedChatGameScreen _:
                _config.SetCVar(CCVars.SeparatedScreenChatSize, stringSize);
                break;
            default:
                // do nothing
                return;
        }

        _config.SaveToFile();
    }

    private void FocusChat()
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.Focus();
            break;
        }
    }

    private void FocusChannel(ChatSelectChannel channel)
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.Focus(channel);
            break;
        }
    }

    private void CycleChatChannel(bool forward)
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.CycleChatChannel(forward);
            break;
        }
    }

    private void StateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            PreferredChannel = ChatSelectChannel.Local;
        }

        UpdateChannelPermissions();
    }

    public void SetSpeechBubbleRoot(LayoutContainer root)
    {
        _speechBubbleRoot.Orphan();
        root.AddChild(_speechBubbleRoot);
        LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        _speechBubbleRoot.SetPositionLast();
    }

    private void OnAttachedChanged(EntityUid uid)
    {
        UpdateChannelPermissions();

        UpdateAutoFillHighlights();
    }

    private void AddSpeechBubble(ChatMessage msg, SpeechBubble.SpeechType speechType)
    {
        var ent = EntityManager.GetEntity(msg.SenderEntity);

        if (!EntityManager.EntityExists(ent))
        {
            _sawmill.Debug("Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
            return;
        }

        EnqueueSpeechBubble(ent, msg, speechType);
    }

    private void CreateSpeechBubble(EntityUid entity, SpeechBubbleData speechData)
    {
        var bubble =
            SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity);

        bubble.OnDied += SpeechBubbleDied;

        if (_activeSpeechBubbles.TryGetValue(entity, out var existing))
        {
            // Push up existing bubbles above the mob's head.
            foreach (var existingBubble in existing)
            {
                existingBubble.VerticalOffset += bubble.ContentSize.Y;
            }
        }
        else
        {
            existing = new List<SpeechBubble>();
            _activeSpeechBubbles.Add(entity, existing);
        }

        existing.Add(bubble);
        _speechBubbleRoot.AddChild(bubble);

        if (existing.Count > SpeechBubbleCap)
        {
            // Get the next speech bubble to fade
            // Any speech bubbles before it are already fading
            var last = existing[^(SpeechBubbleCap + 1)];
            last.FadeNow();
        }
    }

    private void SpeechBubbleDied(EntityUid entity, SpeechBubble bubble)
    {
        RemoveSpeechBubble(entity, bubble);
    }

    private void EnqueueSpeechBubble(EntityUid entity, ChatMessage message, SpeechBubble.SpeechType speechType)
    {
        // Don't enqueue speech bubbles for other maps. TODO: Support multiple viewports/maps?
        if (EntityManager.GetComponent<TransformComponent>(entity).MapID != _eye.CurrentEye.Position.MapId)
            return;

        if (!_queuedSpeechBubbles.TryGetValue(entity, out var queueData))
        {
            queueData = new SpeechBubbleQueueData();
            _queuedSpeechBubbles.Add(entity, queueData);
        }

        queueData.MessageQueue.Enqueue(new SpeechBubbleData(message, speechType));
    }

    public void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble)
    {
        bubble.Dispose();

        var list = _activeSpeechBubbles[entityUid];
        list.Remove(bubble);

        if (list.Count == 0)
        {
            _activeSpeechBubbles.Remove(entityUid);
        }
    }

    private void UpdateChannelPermissions()
    {
        CanSendChannels = default;
        FilterableChannels = default;

        // Can always send console stuff.
        CanSendChannels |= ChatSelectChannel.Console;

        // can always send/recieve OOC
        CanSendChannels |= ChatSelectChannel.OOC;
        CanSendChannels |= ChatSelectChannel.LOOC;
        FilterableChannels |= ChatChannel.OOC;
        FilterableChannels |= ChatChannel.LOOC;

        // can always hear server (nobody can actually send server messages).
        FilterableChannels |= ChatChannel.Server;

        if (_state.CurrentState is GameplayStateBase)
        {
            // can always hear local / radio / emote / notifications when in the game
            FilterableChannels |= ChatChannel.Local;
            FilterableChannels |= ChatChannel.Whisper;
            FilterableChannels |= ChatChannel.Radio;
            FilterableChannels |= ChatChannel.Emotes;
            FilterableChannels |= ChatChannel.Notifications;

            // Can only send local / radio / emote when attached to a non-ghost entity.
            // TODO: this logic is iffy (checking if controlling something that's NOT a ghost), is there a better way to check this?
            if (_ghost is not {IsGhost: true})
            {
                CanSendChannels |= ChatSelectChannel.Local;
                CanSendChannels |= ChatSelectChannel.Whisper;
                CanSendChannels |= ChatSelectChannel.Radio;
                CanSendChannels |= ChatSelectChannel.Emotes;
            }
        }

        // Only ghosts and admins can send / see deadchat.
        if (_admin.HasFlag(AdminFlags.Admin) || _ghost is {IsGhost: true})
        {
            FilterableChannels |= ChatChannel.Dead;
            CanSendChannels |= ChatSelectChannel.Dead;
        }

        // only admins can see / filter asay
        if (_admin.HasFlag(AdminFlags.Adminchat))
        {
            FilterableChannels |= ChatChannel.Admin;
            FilterableChannels |= ChatChannel.AdminAlert;
            FilterableChannels |= ChatChannel.AdminChat;
            FilterableChannels |= ChatChannel.CollectiveMind; // Goobstation - Starlight collective mind port
            CanSendChannels |= ChatSelectChannel.Admin;
            FilterableChannels |= ChatChannel.Telepathic; //Nyano - Summary: makes admins able to see psionic chat.
        }

        // Nyano - Summary: - Begin modified code block to add telepathic as a channel for a psionic user.
        if (_psionic != null && _psionic.IsPsionic)
        {
            FilterableChannels |= ChatChannel.Telepathic;
            CanSendChannels |= ChatSelectChannel.Telepathic;
        }
        // /Nyano - End modified code block
        // Goobstation - Starlight collective mind port
        if (_collectiveMind != null && _collectiveMind.IsCollectiveMind)
        {
            FilterableChannels |= ChatChannel.CollectiveMind;
            CanSendChannels |= ChatSelectChannel.CollectiveMind;
        }

        SelectableChannels = CanSendChannels;

        // Necessary so that we always have a channel to fall back to.
        DebugTools.Assert((CanSendChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((FilterableChannels & ChatChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((SelectableChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");

        // let our chatbox know all the new settings
        CanSendChannelsChanged?.Invoke(CanSendChannels);
        FilterableChannelsChanged?.Invoke(FilterableChannels);
        SelectableChannelsChanged?.Invoke(SelectableChannels);
    }

    public void ClearUnfilteredUnreads(ChatChannel channels)
    {
        foreach (var channel in _unreadMessages.Keys.ToArray())
        {
            if ((channels & channel) == 0)
                continue;

            _unreadMessages[channel] = 0;
            UnreadMessageCountsUpdated?.Invoke(channel, 0);
        }
    }

    public override void FrameUpdate(FrameEventArgs delta)
    {
        UpdateQueuedSpeechBubbles(delta);

        // Let the pickup request settle before sending the interaction. Sending both in
        // the same frame can make the server start the do-after while the item is still
        // inside its storage container, which immediately interrupts the treatment.
        if (_pendingMedicalUseItem is { } pendingItem && _pendingMedicalUseUser is { } pendingUser)
        {
            _pendingMedicalUseDelay -= delta.DeltaSeconds;
            if (_pendingMedicalUseDelay <= 0f)
            {
                _pendingMedicalUseItem = null;
                _pendingMedicalUseUser = null;
                if (_ent.EntityExists(pendingUser) && _ent.EntityExists(pendingItem)
                    && _ent.System<HandsSystem>().IsHolding(pendingUser, pendingItem))
                {
                    var coordinates = _ent.GetComponent<TransformComponent>(pendingUser).Coordinates;
                    _ent.System<SharedInteractionSystem>().InteractUsing(
                        pendingUser,
                        pendingItem,
                        pendingUser,
                        coordinates,
                        checkCanUse: false);
                }
            }
        }

        if (_pendingAmmoReloadItem is { } pendingAmmo && _pendingAmmoReloadUser is { } reloadUser
            && _pendingAmmoReloadGun is { } reloadGun)
        {
            _pendingAmmoReloadDelay -= delta.DeltaSeconds;
            if (_pendingAmmoReloadDelay <= 0f)
            {
                _pendingAmmoReloadItem = null;
                _pendingAmmoReloadUser = null;
                _pendingAmmoReloadGun = null;
                if (_ent.EntityExists(reloadUser) && _ent.EntityExists(pendingAmmo)
                    && _ent.EntityExists(reloadGun)
                    && _ent.System<HandsSystem>().IsHolding(reloadUser, pendingAmmo))
                {
                    var coordinates = _ent.GetComponent<TransformComponent>(reloadUser).Coordinates;
                    _ent.System<SharedInteractionSystem>().InteractUsing(
                        reloadUser,
                        pendingAmmo,
                        reloadGun,
                        coordinates,
                        checkCanUse: false);
                    _combatPanelWindow?.ConsumeTurn();
                }
            }
        }

        if (_combatPanelWindow is not { IsOpen: true, Target: { } target } || !_ent.EntityExists(target))
            return;

        _combatPanelRefreshTimer += TimeSpan.FromSeconds(delta.DeltaSeconds);
        _medicalRefreshTimer += TimeSpan.FromSeconds(delta.DeltaSeconds);
        if (_combatPanelRefreshTimer >= TimeSpan.FromSeconds(0.5))
        {
            _combatPanelRefreshTimer = TimeSpan.Zero;
            UpdateCombatPanelHealth(target);
            UpdateCombatPanelActions();
        }

        if (_medicalRefreshTimer >= TimeSpan.FromSeconds(2))
        {
            _medicalRefreshTimer = TimeSpan.Zero;
            UpdateCombatPanelMedicalItems(target);
            UpdateCombatPanelAmmunition();
        }
    }

    private void UpdateQueuedSpeechBubbles(FrameEventArgs delta)
    {
        // Update queued speech bubbles.
        if (_queuedSpeechBubbles.Count == 0 || _examine == null)
        {
            return;
        }

        foreach (var (entity, queueData) in _queuedSpeechBubbles.ShallowClone())
        {
            if (!EntityManager.EntityExists(entity))
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            queueData.TimeLeft -= delta.DeltaSeconds;
            if (queueData.TimeLeft > 0)
            {
                continue;
            }

            if (queueData.MessageQueue.Count == 0)
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            var msg = queueData.MessageQueue.Dequeue();

            queueData.TimeLeft += BubbleDelayBase + msg.Message.Message.Length * BubbleDelayFactor;

            // We keep the queue around while it has 0 items. This allows us to keep the timer.
            // When the timer hits 0 and there's no messages left, THEN we can clear it up.
            CreateSpeechBubble(entity, msg);
        }

        var player = _player.LocalEntity;
        var predicate = static (EntityUid uid, (EntityUid compOwner, EntityUid? attachedEntity) data)
            => uid == data.compOwner || uid == data.attachedEntity;
        var playerPos = player != null
            ? _eye.CurrentEye.Position
            : MapCoordinates.Nullspace;

        var occluded = player != null && _examine.IsOccluded(player.Value);

        foreach (var (ent, bubs) in _activeSpeechBubbles)
        {
            if (EntityManager.Deleted(ent))
            {
                SetBubbles(bubs, false);
                continue;
            }

            if (ent == player)
            {
                SetBubbles(bubs, true);
                continue;
            }

            var otherPos = _transform?.GetMapCoordinates(ent) ?? MapCoordinates.Nullspace;

            if (occluded && !_examine.InRangeUnOccluded(
                    playerPos,
                    otherPos, 0f,
                    (ent, player), predicate))
            {
                SetBubbles(bubs, false);
                continue;
            }

            SetBubbles(bubs, true);
        }
    }

    private void SetBubbles(List<SpeechBubble> bubbles, bool visible)
    {
        foreach (var bubble in bubbles)
        {
            bubble.Visible = visible;
        }
    }

    public ChatSelectChannel MapLocalIfGhost(ChatSelectChannel channel)
    {
        if (channel == ChatSelectChannel.Local && _ghost is {IsGhost: true})
            return ChatSelectChannel.Dead;

        return channel;
    }

    private bool TryGetRadioChannel(string text, out RadioChannelPrototype? radioChannel)
    {
        radioChannel = null;
        return _player.LocalEntity is EntityUid { Valid: true } uid
           && _chatSys != null
           && _chatSys.TryProcessRadioMessage(uid, text, out _, out radioChannel, quiet: true);
    }

    // Goobstation - Starlight collective mind port
    private bool TryGetCollectiveMind(string text, out CollectiveMindPrototype? collectiveMind)
    {
        collectiveMind = null;
        return _player.LocalEntity is { Valid: true } uid
               && _chatSys != null
               && _chatSys.TryProccessCollectiveMindMessage(uid, text, out _, out collectiveMind, quiet: true);
    }

    public void UpdateSelectedChannel(ChatBox box)
    {
        // <Goobstation> - Starlight collective mind port
        var (prefixChannel, _, radioChannel, collectiveMind) = SplitInputContents(box.ChatInput.Input.Text.ToLower());

        switch (prefixChannel)
        {
            case ChatSelectChannel.None:
                box.ChatInput.ChannelSelector.UpdateChannelSelectButton(box.SelectedChannel, null, null);
                break;
            case ChatSelectChannel.CollectiveMind:
                box.ChatInput.ChannelSelector.UpdateChannelSelectButton(prefixChannel, null, collectiveMind);
                break;
            default:
                box.ChatInput.ChannelSelector.UpdateChannelSelectButton(prefixChannel, radioChannel, null);
                break;
        }
        // </Goobstation>
    }

    // Goobstation - Starlight collective mind port
    public (ChatSelectChannel chatChannel, string text, RadioChannelPrototype? radioChannel, CollectiveMindPrototype? collectiveMind) SplitInputContents(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return (ChatSelectChannel.None, text, null, null); // Goobstation - Starlight collective mind port

        // We only cut off prefix only if it is not a radio or local channel, which both map to the same /say command
        // because ????????

        ChatSelectChannel chatChannel;
        if (TryGetRadioChannel(text, out var radioChannel))
            chatChannel = ChatSelectChannel.Radio;
        else
            chatChannel = PrefixToChannel.GetValueOrDefault(text[0]);

        if ((CanSendChannels & chatChannel) == 0)
            return (ChatSelectChannel.None, text, null, null); // Goobstation - Starlight collective mind port

        if (chatChannel == ChatSelectChannel.Radio)
            return (chatChannel, text, radioChannel, null); // Goobstation - Starlight collective mind port

        // Goobstation - Starlight collective mind port
        if (TryGetCollectiveMind(text, out var collectiveMind) && chatChannel == ChatSelectChannel.CollectiveMind)
            return (chatChannel, text, radioChannel, collectiveMind);

        if (chatChannel == ChatSelectChannel.Local)
        {
            if (_ghost?.IsGhost != true)
                return (chatChannel, text, null, null); // Goobstation - Starlight collective mind port
            else
                chatChannel = ChatSelectChannel.Dead;
        }

        return (chatChannel, text[1..].TrimStart(), null, null); // Goobstation - Starlight collective mind port
    }

    public void SendMessage(ChatBox box, ChatSelectChannel channel)
    {
        _typingIndicator?.ClientSubmittedChatText();

        var text = box.ChatInput.Input.Text;
        box.ChatInput.Input.Clear();
        box.ChatInput.Input.ReleaseKeyboardFocus();
        UpdateSelectedChannel(box);

        if (string.IsNullOrWhiteSpace(text))
            return;

        (var prefixChannel, text, var _, var _) = SplitInputContents(text); // Goobstation - Starlight collective mind port

        // Check if message is longer than the character limit
        if (text.Length > MaxMessageLength)
        {
            var locWarning = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", MaxMessageLength));
            box.AddLine(locWarning, Color.Orange);
            return;
        }

        if (prefixChannel != ChatSelectChannel.None)
            channel = prefixChannel;
        else if (channel == ChatSelectChannel.Radio)
        {
            // radio must have prefix as it goes through the say command.
            text = $";{text}";
        }

        _manager.SendMessage(text, prefixChannel == 0 ? channel : prefixChannel);
    }

    private void OnDamageForceSay(DamageForceSayEvent ev, EntitySessionEventArgs _)
    {
        var chatBox = UIManager.ActiveScreen?.GetWidget<ChatBox>() ?? UIManager.ActiveScreen?.GetWidget<ResizableChatBox>();
        if (chatBox == null)
            return;

        var msg = chatBox.ChatInput.Input.Text.TrimEnd();
        // Don't send on OOC/LOOC obviously!

        // we need to handle selected channel
        // and prefix-channel separately..
        var allowedChannels = ChatSelectChannel.Local | ChatSelectChannel.Whisper;
        if ((chatBox.SelectedChannel & allowedChannels) == ChatSelectChannel.None)
            return;

        // none can be returned from this if theres no prefix,
        // so we allow it in that case (assuming the previous check will have exited already if its an invalid channel)
        var prefixChannel = SplitInputContents(msg).chatChannel;
        if (prefixChannel != ChatSelectChannel.None && (prefixChannel & allowedChannels) == ChatSelectChannel.None)
            return;

        if (_player.LocalSession?.AttachedEntity is not { } ent
            || !EntityManager.TryGetComponent<DamageForceSayComponent>(ent, out var forceSay))
            return;

        if (string.IsNullOrWhiteSpace(msg))
            return;

        var modifiedText = ev.Suffix != null
            ? Loc.GetString(forceSay.ForceSayMessageWrap,
                ("message", msg), ("suffix", ev.Suffix))
            : Loc.GetString(forceSay.ForceSayMessageWrapNoSuffix,
                ("message", msg));

        chatBox.ChatInput.Input.SetText(modifiedText);
        chatBox.ChatInput.Input.ForceSubmitText();
    }

    private void OnChatMessage(MsgChatMessage message)
    {
        var msg = message.Message;
        ProcessChatMessage(msg);

        if ((msg.Channel & ChatChannel.AdminRelated) == 0 ||
            _config.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replayRecording.RecordClientMessage(msg);
        }
    }

    public void ProcessChatMessage(ChatMessage msg, bool speechBubble = true)
    {
        // color the name unless it's something like "the old man"
        if ((msg.Channel == ChatChannel.Local || msg.Channel == ChatChannel.Whisper) && _chatNameColorsEnabled)
        {
            var grammar = _ent.GetComponentOrNull<GrammarComponent>(_ent.GetEntity(msg.SenderEntity));
            if (grammar != null && grammar.ProperNoun == true)
                msg.WrappedMessage = SharedChatSystem.InjectTagInsideTag(msg, "Name", "color", GetNameColor(SharedChatSystem.GetStringInsideTag(msg, "Name")));
        }

        // Color any codewords for minds that have roles that use them
        if (_player.LocalUser != null && _mindSystem != null && _roleCodewordSystem != null)
        {
            if (_mindSystem.TryGetMind(_player.LocalUser.Value, out var mindId) && _ent.TryGetComponent(mindId, out RoleCodewordComponent? codewordComp))
            {
                foreach (var (_, codewordData) in codewordComp.RoleCodewords)
                {
                    foreach (string codeword in codewordData.Codewords)
                        msg.WrappedMessage = SharedChatSystem.InjectTagAroundString(msg, codeword, "color", codewordData.Color.ToHex());
                }
            }
        }

        #region Goobstation - Highlight chat sounds/pings!
        // Goobstation - Highlight chat sounds/pings!
        // Color any words chosen by the client and check for highlights
        var hadHighlight = false;

        // Skip highlight check if this is a message from the local player
        if (_player.LocalSession?.AttachedEntity is not { } localEntity ||
            msg.SenderEntity != _ent.GetNetEntity(localEntity))
        {
            foreach (var highlight in _highlights)
            {
                var newMessage = SharedChatSystem.InjectTagAroundString(msg, highlight, "color", _highlightsColor);
                if (newMessage != msg.WrappedMessage)
                {
                    hadHighlight = true;
                    msg.WrappedMessage = newMessage;
                }
            }

            var currentTime = _timing.CurTime;

            // Only play sound if enough time has passed since the last highlight
            // This avoids playing multiple pings in less than a second
            if (hadHighlight && (currentTime - LastHighlightTime).TotalMilliseconds >= 500)
            {
                LastHighlightTime = currentTime;
                PlayHighlightSound();
            }
        }
        // Goobstation end
        #endregion

        // Log all incoming chat to repopulate when filter is un-toggled
        if (!msg.HideChat)
        {
            History.Add((_timing.CurTick, msg));
            MessageAdded?.Invoke(msg);

            if (!msg.Read)
            {
                _sawmill.Debug($"Message filtered: {msg.Channel}: {msg.Message}");
                if (!_unreadMessages.TryGetValue(msg.Channel, out var count))
                    count = 0;

                count += 1;
                _unreadMessages[msg.Channel] = count;
                UnreadMessageCountsUpdated?.Invoke(msg.Channel, count);
            }
        }

        // Local messages that have an entity attached get a speech bubble.
        if (!speechBubble || msg.SenderEntity == default)
            return;

        switch (msg.Channel)
        {
            case ChatChannel.Local:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Whisper:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Whisper);
                break;

            case ChatChannel.Dead:
                if (_ghost is not {IsGhost: true})
                    break;

                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Emotes:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                break;

            case ChatChannel.LOOC:
                if (_config.GetCVar(CCVars.LoocAboveHeadShow))
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Looc);
                break;
        }
    }

    public void OnDeleteChatMessagesBy(MsgDeleteChatMessagesBy msg)
    {
        // This will delete messages from an entity even if different players were the author.
        // Usages of the erase admin verb should be rare enough that this does not matter.
        // Otherwise the client would need to know that one entity has multiple author players,
        // or the server would need to track when and which entities a player sent messages as.
        History.RemoveAll(h => h.Msg.SenderKey == msg.Key || msg.Entities.Contains(h.Msg.SenderEntity));
        Repopulate();
    }

    public void RegisterChat(ChatBox chat)
    {
        _chats.Add(chat);
    }

    public void UnregisterChat(ChatBox chat)
    {
        _chats.Remove(chat);
    }

    public void OpenCombatPanel(EntityUid target)
    {
        if (_combatPanelWindow == null)
        {
            _combatPanelWindow = UIManager.CreateWindow<CombatPanelWindow>();
            _combatPanelWindow.EmoteRequested += message =>
                _manager.SendMessage(message, ChatSelectChannel.Emotes);
        }

        if (_combatPanelWindow.IsOpen)
            _combatPanelWindow.Close();

        var targetName = Identity.Name(target, _ent);
        _combatPanelWindow.SetTarget(target, targetName);
        UpdateCombatPanelActions();

        _lastBodyStatus = null;
        _combatPanelRefreshTimer = TimeSpan.Zero;
        _medicalRefreshTimer = TimeSpan.Zero;
        UpdateCombatPanelHealth(target);
        UpdateCombatPanelMedicalItems(target);
        UpdateCombatPanelAmmunition();
        _combatPanelWindow.Open();
    }

    private void UpdateCombatPanelActions()
    {
        if (_combatPanelWindow == null || _player.LocalEntity == null)
            return;

        // Re-read the active hand while the panel is open so weapon changes are reflected.
        var heldItem = _ent.System<HandsSystem>().GetActiveHandEntity();
        var hasGun = heldItem is { } gun && _ent.HasComponent<GunComponent>(gun);
        var hasMeleeWeapon = !hasGun
            && heldItem is { } melee && _ent.HasComponent<MeleeWeaponComponent>(melee);

        if (heldItem is { } held)
        {
            var heldName = Identity.Name(held, _ent);
            if (_ent.TryGetComponent<BallisticAmmoProviderComponent>(held, out var ballistic))
                _combatPanelWindow.SetWeaponInfo($"В руках: {heldName}\nЗаряды: {ballistic.Count}/{ballistic.Capacity}");
            else if (_ent.TryGetComponent<BasicEntityAmmoProviderComponent>(held, out var basic))
                _combatPanelWindow.SetWeaponInfo($"В руках: {heldName}\nЗаряды: {basic.Count ?? 0}/{basic.Capacity ?? 0}");
            else if (hasGun)
                _combatPanelWindow.SetWeaponInfo($"В руках: {heldName}\nОгнестрельное оружие");
            else if (hasMeleeWeapon)
                _combatPanelWindow.SetWeaponInfo($"В руках: {heldName}\nОружие ближнего боя");
            else
                _combatPanelWindow.SetWeaponInfo($"В руках: {heldName}");
        }
        else
            _combatPanelWindow.SetWeaponInfo("В руках: пусто");

        _combatPanelWindow.SetAvailableActions(hasGun, hasMeleeWeapon);
    }

    private void UpdateCombatPanelHealth(EntityUid target)
    {
        if (_combatPanelWindow == null || !_ent.EntityExists(target))
            return;

        var status = _ent.TryGetComponent<MobStateComponent>(target, out var mobState)
            ? mobState.CurrentState switch
            {
                MobState.Alive => "Жив",
                MobState.Critical => "Критическое состояние",
                MobState.Dead => "Мёртв",
                _ => "Неизвестно",
            }
            : "Неизвестно";
        var totalDamage = _ent.TryGetComponent<DamageableComponent>(target, out var damageable)
            ? damageable.TotalDamage.ToString()
            : "Нет данных";

        _combatPanelWindow.SetHealthAnalyzer(status, totalDamage);

        if (!_ent.TryGetComponent<TargetingComponent>(target, out var targeting))
        {
            if (_healthDollEntity is { } missingTargetDoll && _ent.EntityExists(missingTargetDoll))
                _ent.QueueDeleteEntity(missingTargetDoll);
            _healthDollEntity = null;
            _lastBodyStatus = null;
            _combatPanelWindow.SetHealthDoll(null);
            return;
        }

        var bodyStatus = targeting.BodyStatus;
        if (_lastBodyStatus != null && _lastBodyStatus.SequenceEqual(bodyStatus))
            return;

        _lastBodyStatus = new Dictionary<TargetBodyPart, WoundableSeverity>(bodyStatus);
        if (_healthDollEntity is { } oldDoll && _ent.EntityExists(oldDoll))
            _ent.QueueDeleteEntity(oldDoll);

        _healthDollEntity = _ent.Spawn(HealthDollPrototype);
        if (!_ent.TryGetComponent<SpriteComponent>(_healthDollEntity.Value, out var sprite))
            return;

        var spriteSystem = _ent.System<SpriteSystem>();
        // Append layers after any layers already defined by the prototype. Starting at
        // zero can leave the final body-part layer (RightFoot) without its scale applied.
        var layer = sprite.AllLayers.Count;
        // TargetBodyPart is a flags enum; Enum.GetValues also returns composite masks
        // (Hands, Arms, All, etc.) that do not have individual RSI files.
        foreach (var bodyPart in SharedTargetingSystem.GetValidParts())
        {
            var severity = bodyStatus.TryGetValue(bodyPart, out var currentSeverity)
                ? currentSeverity
                : WoundableSeverity.Perfect;
            var enumName = Enum.GetName(typeof(TargetBodyPart), bodyPart) ?? "Unknown";
            var baseRsiPath = new ResPath($"/Textures/_Shitmed/Interface/Targeting/Status/{enumName.ToLowerInvariant()}.rsi");
            var rsi = new SpriteSpecifier.Rsi(baseRsiPath, $"{enumName.ToLowerInvariant()}_{(int) severity}");
            sprite.AddLayer(spriteSystem.Frame0(rsi));
            sprite.LayerSetScale(layer, new Vector2(3f, 3f));
            layer++;
        }

        _combatPanelWindow.SetHealthDoll(_healthDollEntity);
    }

    private void UpdateCombatPanelMedicalItems(EntityUid target)
    {
        if (_combatPanelWindow == null
            || _player.LocalEntity is not { } user
            || !_ent.TryGetComponent<InventoryComponent>(user, out var inventory))
        {
            _combatPanelWindow?.SetMedicalItems(Array.Empty<(string Name, Action Use)>());
            return;
        }

        var items = new List<(string Name, Action Use)>();
        var pendingStorage = new Queue<EntityUid>();
        var visited = new HashSet<EntityUid>();
        var slots = _ent.System<InventorySystem>().GetSlotEnumerator((user, inventory));
        while (slots.NextItem(out var item))
            AddMedicalItem(item);

        while (pendingStorage.TryDequeue(out var storageEntity))
        {
            if (!_ent.TryGetComponent<StorageComponent>(storageEntity, out var storage))
                continue;

            foreach (var storedItem in storage.Container.ContainedEntities)
                AddMedicalItem(storedItem);
        }

        _combatPanelWindow.SetMedicalItems(items);

        void AddMedicalItem(EntityUid item)
        {
            if (!visited.Add(item) || !_ent.EntityExists(item))
                return;

            var itemName = Identity.Name(item, _ent);
            if (IsMedicalSupply(itemName.ToLowerInvariant()))
            {
                var capturedItem = item;
                items.Add((itemName, () =>
                {
                    if (!_ent.EntityExists(user) || !_ent.EntityExists(capturedItem))
                        return;

                    // Medical supplies are used on the player who opened the RP panel.
                    // If the item is in storage, request pickup first and defer interaction
                    // until the hand state has had time to synchronize with the server.
                    var hands = _ent.System<HandsSystem>();
                    if (!hands.IsHolding(user, capturedItem)
                        && !hands.TryPickupAnyHand(user, capturedItem, checkActionBlocker: false, animate: false))
                        return;

                    _pendingMedicalUseUser = user;
                    _pendingMedicalUseItem = capturedItem;
                    _pendingMedicalUseDelay = 0.2f;
                }));
            }

            if (_ent.HasComponent<StorageComponent>(item))
                pendingStorage.Enqueue(item);
        }
    }

    private void UpdateCombatPanelAmmunition()
    {
        if (_combatPanelWindow == null
            || _player.LocalEntity is not { } user
            || !_ent.TryGetComponent<InventoryComponent>(user, out var inventory))
        {
            _combatPanelWindow?.SetAmmunitionItems(Array.Empty<(string Name, string Details, Action Use)>());
            return;
        }

        var heldGun = _ent.System<HandsSystem>().GetActiveHandEntity();
        var canReload = heldGun is { } gun && _ent.HasComponent<GunComponent>(gun);
        var items = new List<(string Name, string Details, Action Use)>();
        var pendingStorage = new Queue<EntityUid>();
        var visited = new HashSet<EntityUid>();
        var slots = _ent.System<InventorySystem>().GetSlotEnumerator((user, inventory));
        while (slots.NextItem(out var item))
            AddAmmunitionItem(item);

        while (pendingStorage.TryDequeue(out var storageEntity))
        {
            if (!_ent.TryGetComponent<StorageComponent>(storageEntity, out var storage))
                continue;

            foreach (var storedItem in storage.Container.ContainedEntities)
                AddAmmunitionItem(storedItem);
        }

        _combatPanelWindow.SetAmmunitionItems(items);

        void AddAmmunitionItem(EntityUid item)
        {
            if (!visited.Add(item) || !_ent.EntityExists(item))
                return;

            var itemName = Identity.Name(item, _ent);
            var normalizedName = itemName.ToLowerInvariant();
            var isAmmo = _ent.HasComponent<CartridgeAmmoComponent>(item)
                || _ent.HasComponent<BallisticAmmoProviderComponent>(item)
                || _ent.HasComponent<BasicEntityAmmoProviderComponent>(item);
            var isThrowable = normalizedName.Contains("гранат")
                || normalizedName.Contains("молотов")
                || normalizedName.Contains("molotov")
                || normalizedName.Contains("grenade")
                || normalizedName.Contains("зажигатель");

            if (isAmmo || isThrowable)
            {
                var amount = _ent.TryGetComponent<StackComponent>(item, out var stack)
                    ? stack.Count
                    : _ent.TryGetComponent<BallisticAmmoProviderComponent>(item, out var boxAmmo)
                        ? boxAmmo.Count
                        : _ent.TryGetComponent<BasicEntityAmmoProviderComponent>(item, out var basicAmmo)
                            ? basicAmmo.Count ?? 1
                            : 1;
                var capturedItem = item;
                var capturedGun = heldGun;
                var details = isAmmo
                    ? $"{amount} шт. — {(canReload ? "Зарядить" : "нужно оружие в руке")}"
                    : $"{amount} шт. — расходник";
                items.Add((itemName, details, () =>
                {
                    if (!_ent.EntityExists(user) || !_ent.EntityExists(capturedItem))
                        return;

                    var hands = _ent.System<HandsSystem>();
                    if (!hands.IsHolding(user, capturedItem)
                        && !hands.TryPickupAnyHand(user, capturedItem, checkActionBlocker: false, animate: false))
                        return;

                    if (isAmmo && canReload && capturedGun is { } targetGun
                        && _ent.EntityExists(targetGun))
                    {
                        _pendingAmmoReloadUser = user;
                        _pendingAmmoReloadItem = capturedItem;
                        _pendingAmmoReloadGun = targetGun;
                        _pendingAmmoReloadDelay = 0.2f;
                    }
                }));
            }

            if (_ent.HasComponent<StorageComponent>(item))
                pendingStorage.Enqueue(item);
        }
    }

    private static bool IsMedicalSupply(string itemName)
    {
        return itemName.Contains("бинт")
            || itemName.Contains("шприц")
            || itemName.Contains("медипен")
            || itemName.Contains("мазь")
            || itemName.Contains("ожог")
            || itemName.Contains("ушиб")
            || itemName.Contains("brutepack")
            || itemName.Contains("gauze")
            || itemName.Contains("ointment")
            || itemName.Contains("medipen")
            || itemName.Contains("syringe")
            || itemName.Contains("bruise");
    }

    public ChatSelectChannel GetPreferredChannel()
    {
        return MapLocalIfGhost(PreferredChannel);
    }

    public void NotifyChatTextChange()
    {
        _typingIndicator?.ClientChangedChatText();
    }

    public void NotifyChatFocus(bool isFocused)
    {
        _typingIndicator?.ClientChangedChatFocus(isFocused);
    }

    public void Repopulate()
    {
        foreach (var chat in _chats)
        {
            chat.Repopulate();
        }
    }

    /// <summary>
    /// Returns the chat name color for a mob
    /// </summary>
    /// <param name="name">Name of the mob</param>
    /// <returns>Hex value of the color</returns>
    public string GetNameColor(string name)
    {
        var colorIdx = Math.Abs(name.GetHashCode() % _chatNameColors.Length);
        return _chatNameColors[colorIdx];
    }

    private readonly record struct SpeechBubbleData(ChatMessage Message, SpeechBubble.SpeechType Type);

    private sealed class SpeechBubbleQueueData
    {
        /// <summary>
        ///     Time left until the next speech bubble can appear.
        /// </summary>
        public float TimeLeft { get; set; }

        public Queue<SpeechBubbleData> MessageQueue { get; } = new();
    }
}