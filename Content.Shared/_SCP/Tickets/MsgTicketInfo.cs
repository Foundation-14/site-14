using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.Tickets;

public sealed class MsgTicketInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public float Balance;
    public float Modifier;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Balance = buffer.ReadSingle();
        Modifier = buffer.ReadSingle();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Balance);
        buffer.Write(Modifier);
    }
}
