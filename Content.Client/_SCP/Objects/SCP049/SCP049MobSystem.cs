using Content.Shared._SCP.SCP049;
using Content.Shared._SCP.SCP049.Components;
using Content.Shared._SCP.SCP049.SharedSystem;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ShowTarget049HudSystem : SharedSCP049System
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCPTarget049Component, GetStatusIconsEvent>(OnGetScpIcon);
    }

    private void OnGetScpIcon(Entity<SCPTarget049Component> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<SCPMob049Component>(ent))
            return;

        if (_prototype.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
