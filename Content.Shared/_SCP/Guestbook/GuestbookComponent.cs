using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.Guestbook.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GuestbookComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, GuestbookEntry> Entries = new();

    [DataField, AutoNetworkedField]
    public string? VisibleAdjectiveLocId;

    [DataField, AutoNetworkedField]
    public bool UseRandomAdjective;

    [DataField, AutoNetworkedField]
    public ProtoId<LocalizedDatasetPrototype> AdjectiveDataset = "GuestbookAdjectives";
}

[Serializable, NetSerializable]
public struct GuestbookEntry
{
    public string RealName;
    public string GivenName;

    public GuestbookEntry(string realName, string givenName)
    {
        RealName = realName;
        GivenName = givenName;
    }
}
