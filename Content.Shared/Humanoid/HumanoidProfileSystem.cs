using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.Enums; // SCP-Foundation
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Voice = profile.Voice;
        ent.Comp.Sex = profile.Sex;
        Dirty(ent);

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, profile.Voice);
        RaiseLocalEvent(ent, ref voiceChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }
    }

    // SCP-Foundation-start
    public Gender GetGender(EntityUid uid, HumanoidProfileComponent? profile = null)
    {
        if (!Resolve(uid, ref profile, logMissing: false))
            return Gender.Epicene;
        return profile.Gender;
    }

    public int GetAge(EntityUid uid, HumanoidProfileComponent? profile = null)
    {
        if (!Resolve(uid, ref profile, logMissing: false))
            return 18;
        return profile.Age;
    }

    public ProtoId<SpeciesPrototype> GetSpecies(EntityUid uid, HumanoidProfileComponent? profile = null)
    {
        if (!Resolve(uid, ref profile, logMissing: false))
            return HumanoidCharacterProfile.DefaultSpecies;
        return profile.Species;
    }
    // SCP-Foundation-end

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args) // SCP-Foundation modifed
    {
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        var overrideEv = new _SCP.Guestbook.Events.IdentityViewerOverrideEvent(ent.Owner);
        RaiseLocalEvent(args.Examiner, ref overrideEv);

        if (overrideEv.Override is { } overriddenName)
        {
            args.PushText(Loc.GetString("humanoid-appearance-component-examine-named",
                ("name", overriddenName),
                ("age", age),
                ("species", species)));
            return;
        }

        var identity = Identity.Entity(ent, EntityManager, args.Examiner);
        args.PushText(Loc.GetString("humanoid-appearance-component-examine",
            ("user", identity),
            ("age", age),
            ("species", species)));
    }

    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (ProtoMan.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!ProtoMan.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
