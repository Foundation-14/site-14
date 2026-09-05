namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.White, // SCP-Foundation
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.PaleGreen, // SCP-Foundation
            ChatChannel.OOC => Color.Orange, // SCP-Foundation
            ChatChannel.Dead => Color.MediumPurple,
            ChatChannel.Admin => Color.Gold, // SCP-Foundation
            ChatChannel.AdminAlert => Color.Gold, // SCP-Foundation
            ChatChannel.AdminChat => Color.DarkRed, // SCP-Foundation
            ChatChannel.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }
}
