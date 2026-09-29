using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._SCP.Objects.SCP207;
using Content.Shared._SCP.Objects.SCP207.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._SCP;

public sealed class SCP207Test : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [TestCase(5, false)]
    [TestCase(10, true)]
    public async Task ActivationThreshold(int dose, bool expected)
    {
        await AddAtmosphere();
        var bottle = await PlaceInHands("SCP207Bottle");
        await Server.WaitAssertion(() =>
        {
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(ToServer(bottle), "drink", out var solution), Is.True);
            solutions.SplitSolution(solution.Value, solution.Value.Comp.Solution.Volume - dose);
        });
        await UseInHand();
        await RunSeconds(1);
        await UseInHand();
        await RunSeconds(10);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<StatusEffectsSystem>().HasStatusEffect(SPlayer, SharedSCP207System.Effect), Is.EqualTo(expected));
        });
    }

    [Test]
    public async Task ProgressionTest()
    {
        await AddAtmosphere();
        await PlaceInHands("SCP207Bottle");
        await UseInHand();
        await RunSeconds(1);
        await UseInHand();
        await RunSeconds(10);

        var statuses = SEntMan.System<StatusEffectsSystem>();
        EntityUid effect = default;
        float speed = 0;
        await Server.WaitAssertion(() =>
        {
            Assert.That(statuses.TryGetStatusEffect(SPlayer, SharedSCP207System.Effect, out var uid), Is.True);
            effect = uid!.Value;
            Assert.That(SEntMan.GetComponent<StatusEffectComponent>(effect).EndEffectTime, Is.Null);
            Assert.That(SEntMan.System<SleepingSystem>().TrySleeping(SPlayer), Is.False);
            speed = SEntMan.GetComponent<MovementModStatusEffectComponent>(effect).WalkSpeedModifier;
            Assert.That(speed, Is.GreaterThan(1f));
        });

        await RunSeconds(70);
        await Server.WaitAssertion(() =>
        {
            Assert.That(statuses.TryGetStatusEffect(SPlayer, SharedSCP207System.Effect, out var uid), Is.True);
            Assert.That(uid, Is.EqualTo(effect));
            var progression = SEntMan.GetComponent<SCP207Component>(effect);
            var age = progression.Elapsed;
            statuses.TryUpdateStatusEffectDuration(SPlayer, SharedSCP207System.Effect);
            Assert.That(progression.Elapsed, Is.EqualTo(age), "Another dose must not reset progression.");
            Assert.That(SEntMan.GetComponent<MovementModStatusEffectComponent>(effect).WalkSpeedModifier,
                Is.GreaterThan(speed));

            // пизда
            progression.Elapsed = progression.DamageDelay + 1f;
        });

        await RunSeconds(2);
        await Server.WaitAssertion(() =>
        {
            var damage = SEntMan.System<DamageableSystem>().GetAllDamage(SPlayer);
            Assert.That(damage.DamageDict["Poison"].Float(), Is.GreaterThan(0));
            Assert.That(damage.DamageDict["Bloodloss"].Float(), Is.GreaterThan(0));
            Assert.That(statuses.TryRemoveStatusEffect(SPlayer, SharedSCP207System.Effect), Is.True);
        });
        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(statuses.HasStatusEffect(SPlayer, SharedSCP207System.Effect), Is.False);
            Assert.That(SEntMan.GetComponent<MovementSpeedModifierComponent>(SPlayer).WalkSpeedModifier, Is.EqualTo(1f).Within(0.01f));
            Assert.That(SEntMan.System<SleepingSystem>().TrySleeping(SPlayer), Is.True);
        });
    }
}
