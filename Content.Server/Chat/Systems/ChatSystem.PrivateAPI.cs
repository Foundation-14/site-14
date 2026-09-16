using System.Linq;
using Content.Shared._SCP.Guestbook.Events; // SCP-Foundation
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem // SCP-Foundation modifed
{
    private void SendEntitySpeak(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, originalMessage);

        if (message.Length == 0)
            return;

        var speech = GetSpeechVerb(source, message);

        // get the entity's apparent name (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            if (nameEv.SpeechVerb != null && ProtoMan.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);

        var verbString = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));
        var escapedMessage = FormattedMessage.EscapeText(message);
        var locKey = speech.Bold
            ? "chat-manager-entity-say-bold-wrap-message"
            : "chat-manager-entity-say-wrap-message";

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            var listener = session.AttachedEntity;
            var listenerName = listener is { Valid: true }
                ? GetGuestbookNameOr(source, listener.Value, name)
                : name;

            var listenerWrapped = Loc.GetString(locKey,
                ("entityName", listenerName),
                ("verb", verbString),
                ("fontType", speech.FontId),
                ("fontSize", speech.FontSize),
                ("message", escapedMessage));

            _chatManager.ChatMessageToOne(ChatChannel.Local, message, listenerWrapped, source, entHideChat, session.Channel);
        }

        var wrappedMessage = Loc.GetString(locKey,
            ("entityName", name),
            ("verb", verbString),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", escapedMessage));

        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Local, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeEvent(source, message, null, null);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private void SendEntityWhisper( // SCP-Foundation modifed
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkupOrThrow(originalMessage));
        if (message.Length == 0)
            return;

        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

        // get the entity's name by visual identity (if no override provided).
        string nameIdentity = nameOverride ?? Identity.Name(source, EntityManager);
        // get the entity's name by voice (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }

        name = FormattedMessage.EscapeText(name);
        nameIdentity = FormattedMessage.EscapeText(nameIdentity);

        var escapedMessage = FormattedMessage.EscapeText(message);
        var escapedObf = FormattedMessage.EscapeText(obfuscatedMessage);

        var wrappedUnknownMessage = Loc.GetString("chat-manager-entity-whisper-unknown-wrap-message",
            ("message", escapedObf));

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue;

            var listenerName = GetGuestbookNameOr(source, listener, name);
            var listenerNameId = GetGuestbookNameOr(source, listener, nameIdentity);

            var listenerWrapped = Loc.GetString("chat-manager-entity-whisper-wrap-message",
                ("entityName", listenerName), ("message", escapedMessage));

            var listenerWrappedObf = Loc.GetString("chat-manager-entity-whisper-wrap-message",
                ("entityName", listenerNameId), ("message", escapedObf));

            if (data.Range <= WhisperClearRange || data.Observer)
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, message, listenerWrapped, source, false, session.Channel);
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, listenerWrappedObf, source, false, session.Channel);
            else
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedUnknownMessage, source, false, session.Channel);
        }

        var wrappedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", name), ("message", escapedMessage));
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Whisper, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeEvent(source, message, channel, obfuscatedMessage);
        RaiseLocalEvent(source, ev, true);

        if (!hideLog)
            if (originalMessage == message)
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source} as {name}: {originalMessage}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source}: {originalMessage}.");
            }
            else
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                        $"Whisper from {source} as {name}, original: {originalMessage}, transformed: {message}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                        $"Whisper from {source}, original: {originalMessage}, transformed: {message}.");
            }
    }

    protected override void SendEntityEmote( // SCP-Foundation modifed
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
        )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        string name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        var escapedAction = FormattedMessage.RemoveMarkupOrThrow(action);

        if (checkEmote &&
            !TryEmoteChatInput(source, action))
            return;

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            var listener = session.AttachedEntity;
            var listenerName = listener is { Valid: true }
                ? GetGuestbookNameOr(source, listener.Value, name)
                : name;

            var listenerWrapped = Loc.GetString("chat-manager-entity-me-wrap-message",
                ("entityName", listenerName),
                ("entity", ent),
                ("message", escapedAction));

            _chatManager.ChatMessageToOne(ChatChannel.Emotes, action, listenerWrapped, source, entHideChat, session.Channel);
        }

        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", escapedAction));

        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Emotes, action, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        if (!hideLog)
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source} as {name}: {action}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source}: {action}");
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled) return;
        }
        else if (!_loocEnabled) return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInVoiceRange(ChatChannel.LOOC, message, wrappedMessage, source, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, player.UserId);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {source}: {message}");
    }

    private void SendDeadChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        if (!_adminManager.IsAdmin(player) && !_deadChatEnabled)
            return;

        var clients = GetDeadChatClients();
        var playerName = Name(source);
        string wrappedMessage;
        if (_adminManager.IsAdmin(player))
        {
            wrappedMessage = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", player.Channel.UserName),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {source}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {source}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, wrappedMessage, source, hideChat, true, clients.ToList(), author: player.UserId);
    }

    // SCP-Foundation-start
    private string GetGuestbookNameOr(EntityUid source, EntityUid listener, string escapedFallback)
    {
        var ev = new IdentityViewerOverrideEvent(source);
        RaiseLocalEvent(listener, ref ev);
        return ev.Override is { } o ? FormattedMessage.EscapeText(o) : escapedFallback;
    }
    // SCP-Foundation-end
}
