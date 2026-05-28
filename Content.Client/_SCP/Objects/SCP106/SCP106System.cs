using Content.Shared._SCP.SCP106.Components;
using Content.Shared._SCP.SCP106;
using Robust.Client.GameObjects;

namespace Content.Client._SCP.SCP106;

public sealed partial class SCP106System : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP106Component, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<SCP106TrapComponent, AppearanceChangeEvent>(OnPortalAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, SCP106Component component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, SCP106Visuals.Teleported, out var teleported, args.Component))
        {
            if (teleported)
                args.Sprite.LayerSetState(0, component.TeleportedState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }

    private void OnPortalAppearanceChange(EntityUid uid, SCP106TrapComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, SCP106PortalVisuals.ExitState, out var exit, args.Component))
        {
            if (exit)
                args.Sprite.LayerSetState(0, component.ExitState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
