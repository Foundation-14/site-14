using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared._SCP.SCP268;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._SCP.SCP268;

public sealed partial class SCP268VisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private SCP268VisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new SCP268VisionOverlay();

        SubscribeLocalEvent<SCP268Component, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SCP268Component, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_overlay != null)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnEquipped(EntityUid uid, SCP268Component component, GotEquippedEvent args)
    {
        if (!args.SlotFlags.HasFlag(SlotFlags.HEAD))
            return;

        if (args.EquipTarget != _playerManager.LocalEntity)
            return;

        _overlay.SetIntensity(1f);
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnUnequipped(EntityUid uid, SCP268Component component, GotUnequippedEvent args)
    {
        if (args.EquipTarget != _playerManager.LocalEntity)
            return;

        _overlay.SetIntensity(0f);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (_playerManager.LocalEntity is { } player &&
            _inventory.TryGetSlotEntity(player, "head", out var hatUid) &&
            TryComp<SCP268Component>(hatUid, out var blindfold) &&
            blindfold.Wearer == player)
        {
            _overlay.SetIntensity(1f);
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
