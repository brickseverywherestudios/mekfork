using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.P4CrystallizeTime;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class FruP4CrystallizeTimeSeatDebuffTests
{
    [Test]
    public void EverySeatGetsTheDebuffItWasAssignedAndTheDealStaysWhole()
    {
        var overrides = new FruP4CrystallizeTimeStateOverrides();
        overrides.SeatDebuffs[RegenHealer] = CrystallizeDebuff.RedAero;
        overrides.SeatDebuffs[ShieldHealer] = CrystallizeDebuff.RedIce;
        overrides.SeatDebuffs[MainTank] = CrystallizeDebuff.BlueDarkness;
        overrides.SeatDebuffs[CasterDps] = CrystallizeDebuff.RedAero;

        WithSeats(() =>
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var state = new FruP4CrystallizeTimeState(new Rng(seed), MeleeDpsA, overrides);
                Assert.Multiple(() =>
                {
                    Assert.That(state.Debuff(RegenHealer), Is.EqualTo(CrystallizeDebuff.RedAero), $"seed {seed}");
                    Assert.That(state.Debuff(ShieldHealer), Is.EqualTo(CrystallizeDebuff.RedIce), $"seed {seed}");
                    Assert.That(state.Debuff(MainTank), Is.EqualTo(CrystallizeDebuff.BlueDarkness), $"seed {seed}");
                    Assert.That(state.Debuff(CasterDps), Is.EqualTo(CrystallizeDebuff.RedAero), $"seed {seed}");
                    foreach (var debuff in Enum.GetValues<CrystallizeDebuff>())
                        Assert.That(state.Holders(debuff).Count(), Is.EqualTo(FruP4CrystallizeTimeStateOverrides.Holders(debuff)), $"seed {seed}: {debuff}");
                });
            }
        });
    }

    [Test]
    public void SeatsLeftOnAutoStillRollEveryDebuff()
    {
        var overrides = new FruP4CrystallizeTimeStateOverrides();
        overrides.SeatDebuffs[OffTank] = CrystallizeDebuff.BlueWater;

        WithSeats(() =>
        {
            var seen = Enumerable.Range(0, 200)
                .Select(seed => new FruP4CrystallizeTimeState(new Rng(seed), MeleeDpsA, overrides).Debuff(MeleeDpsB))
                .ToHashSet();
            Assert.That(seen, Is.EquivalentTo(Enum.GetValues<CrystallizeDebuff>().Where(d => d != CrystallizeDebuff.BlueWater)));
        });
    }

    [Test]
    public void AThirdRedIceIsAConflict()
    {
        var overrides = new FruP4CrystallizeTimeStateOverrides();
        overrides.SeatDebuffs[MainTank] = CrystallizeDebuff.RedIce;
        overrides.SeatDebuffs[OffTank] = CrystallizeDebuff.RedIce;

        WithSeats(() =>
        {
            Assert.That(overrides.Validate().Any, Is.False);
            overrides.SeatDebuffs[RegenHealer] = CrystallizeDebuff.RedIce;
            Assert.That(overrides.Validate().Problems, Has.Count.EqualTo(1));
        });
    }

    private static void WithSeats(Action test)
    {
        PerRole.ForceSeats = true;
        try { test(); }
        finally { PerRole.ForceSeats = false; }
    }
}
