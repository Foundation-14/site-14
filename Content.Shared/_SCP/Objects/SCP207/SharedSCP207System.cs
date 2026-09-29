using Content.Shared._SCP.Objects.SCP207.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._SCP.Objects.SCP207;

public abstract class SharedSCP207System : EntitySystem
{
    public static readonly EntProtoId Effect = "StatusEffectScp207";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP207Component, StatusEffectRelayedEvent<TryingToSleepEvent>>(OnTryingToSleep);
    }

    private void OnTryingToSleep(Entity<SCP207Component> ent, ref StatusEffectRelayedEvent<TryingToSleepEvent> args)
    {
        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }
}
