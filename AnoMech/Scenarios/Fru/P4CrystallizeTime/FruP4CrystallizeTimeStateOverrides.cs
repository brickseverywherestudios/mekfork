using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

// User-forced rolls for FruP4CrystallizeTimeState; null leaves a field randomized.
public sealed class FruP4CrystallizeTimeStateOverrides
{
    // Solo: the player gets one of these.
    public IReadOnlyList<CrystallizeDebuff>? PlayerDebuffs { get; set; }

    // Hosting: each seat's own debuff; null leaves the seat to the roll.
    public PerRoleSetting<CrystallizeDebuff> SeatDebuffs { get; set; } = new();

    public bool? SlowNorthwest { get; set; }
    public CrystallizeCorner? Corner { get; set; }

    // Every role's debuff, overriding PlayerDebuffs: two of each red, one of each blue.
    public IReadOnlyDictionary<PartyRole, CrystallizeDebuff>? Debuffs { get; set; }
    public IReadOnlyList<PartyRole>? Quietus { get; set; }
    public PartyRole? SpiritTakerTarget { get; set; }

    public static int Holders(CrystallizeDebuff debuff) => FruP4CrystallizeTimeState.IsRed(debuff) ? 2 : 1;

    public static string Label(CrystallizeDebuff debuff) => debuff switch
    {
        CrystallizeDebuff.RedAero => "Red + Wind (Aero)",
        CrystallizeDebuff.RedIce => "Red + Ice",
        CrystallizeDebuff.BlueEruption => "Blue + Fire (Eruption)",
        CrystallizeDebuff.BlueIce => "Blue + Ice",
        CrystallizeDebuff.BlueDarkness => "Blue + Darkness",
        _ => "Blue + Water",
    };

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        foreach (var debuff in System.Enum.GetValues<CrystallizeDebuff>())
            conflicts.AtMost(Holders(debuff), PerRole.All.Where(r => SeatDebuffs[r] == debuff).ToList(), Label(debuff));
        return conflicts;
    }
}
