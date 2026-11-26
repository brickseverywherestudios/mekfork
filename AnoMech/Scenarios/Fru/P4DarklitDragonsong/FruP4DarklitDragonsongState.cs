using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P4DarklitDragonsong;

// Four players chained in a loop (a tank, a healer and two DPS, linked as a bowtie, box or
// hourglass), Dark Water on one chained and one free player, Spirit Taker on a random player and
// the Usurper's wing on a random side.
//
// LineupOrder is the NA lineup the chains and waters are rolled against: 0-3 chained
// (NW, NE, SE, SW), 4-7 free.
public sealed class FruP4DarklitDragonsongState
{
    public const float ChainsAt = 21.9f;
    public const float CursesAt = 28.9f;
    public const float CursesEndAt = 42.9f;
    public const float TowersAt = 32.2f;
    public const float DarkWaterAt = 40.15f;
    public const float TowerRadius = 4f;
    public static readonly Vector3 OracleCenterSpot = new(0, 0, 5);

    private static readonly PartyRole[] LineupDps = [PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];
    private static readonly PartyRole[] BaitDps = [PartyRole.CasterDps, PartyRole.PhysRangedDps, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];

    private readonly PartyRole[] links;

    public PartyRole ChainedTank { get; }
    public PartyRole ChainedHealer { get; }
    public IReadOnlyList<PartyRole> DpsOrder { get; }
    public int Shape { get; }
    public IReadOnlyList<PartyRole> LineupOrder { get; }
    public int TetherWaterIndex { get; }
    public int FreeWaterIndex { get; }
    public PartyRole SpiritTakerTarget { get; }
    public bool EastWing { get; }
    // Where the Oracle's last jump lands, set as it is cast; null before her first jump. Jumps
    // counts them, so a peer can tell a fresh landing from one it already has.
    public Vector3? OracleLanding { get; set; }
    public int OracleJumps { get; set; }
    // The tank the strat sends to bait Somber Dance. Set by the host's Ai, the only client that
    // knows which tanks are bot-driven; null when the strat doesn't choose.
    public PartyRole? SomberDanceTank { get; set; }

    public FruP4DarklitDragonsongState(Rng rng, FruP4DarklitDragonsongStateOverrides overrides)
    {
        var tank = ChainedTank = overrides.ChainedTank ?? rng.NextTankRole();
        var healer = ChainedHealer = overrides.ChainedHealer ?? rng.NextHealerRole();
        var dps = overrides.DpsOrder?.ToArray() ?? rng.Shuffle(LineupDps);
        DpsOrder = dps;
        var shape = Shape = overrides.Shape ?? rng.Next(3);
        TetherWaterIndex = overrides.TetherWaterIndex ?? rng.Next(4);
        FreeWaterIndex = overrides.FreeWaterIndex ?? rng.Next(4);
        SpiritTakerTarget = overrides.SpiritTakerTarget ?? rng.NextRole();
        EastWing = overrides.EastWing ?? rng.NextBool();

        var pair = LineupDps.Where(r => r == dps[0] || r == dps[1]).ToArray();
        var free = BaitDps.Where(r => r != dps[0] && r != dps[1]).ToArray();
        links = shape switch
        {
            0 => [healer, pair[1], tank, pair[0]],
            1 => [healer, tank, pair[1], pair[0]],
            _ => [healer, tank, pair[0], pair[1]],
        };
        PartyRole[] lineup = [healer, tank, pair[1], pair[0], (PartyRole)(5 - (int)healer), (PartyRole)(1 - (int)tank), free[1], free[0]];
        if (shape == 1) (lineup[1], lineup[2]) = (lineup[2], lineup[1]);
        if (shape == 2) (lineup[1], lineup[3]) = (lineup[3], lineup[1]);
        LineupOrder = lineup;
    }

    // A peer's copy of the host's rolls; null if the message doesn't describe a possible run.
    public static FruP4DarklitDragonsongState? FromNetworkReplay(PartyRole chainedTank, PartyRole chainedHealer, IReadOnlyList<PartyRole> dpsOrder,
        int shape, int tetherWaterIndex, int freeWaterIndex, PartyRole spiritTakerTarget, bool eastWing, PartyRole? somberDanceTank)
    {
        if (!chainedTank.IsTank() || chainedHealer is not (PartyRole.RegenHealer or PartyRole.ShieldHealer)) return null;
        if (dpsOrder.Count != LineupDps.Length || !LineupDps.All(dpsOrder.Contains)) return null;
        if (shape is < 0 or > 2 || tetherWaterIndex is < 0 or > 3 || freeWaterIndex is < 0 or > 3) return null;
        if (!Enum.IsDefined(spiritTakerTarget) || somberDanceTank is { } somber && !somber.IsTank()) return null;
        return new FruP4DarklitDragonsongState(Rng.Detached, new FruP4DarklitDragonsongStateOverrides
        {
            ChainedTank = chainedTank,
            ChainedHealer = chainedHealer,
            DpsOrder = dpsOrder.ToList(),
            Shape = shape,
            TetherWaterIndex = tetherWaterIndex,
            FreeWaterIndex = freeWaterIndex,
            SpiritTakerTarget = spiritTakerTarget,
            EastWing = eastWing,
        })
        { SomberDanceTank = somberDanceTank };
    }

    public PartyRole TetherWater => LineupOrder[TetherWaterIndex];
    public PartyRole FreeWater => LineupOrder[4 + FreeWaterIndex];
    public PartyRole[] Waters => [TetherWater, FreeWater];

    public (PartyRole, PartyRole) ChainLink(int index) => (links[index], links[(index + 1) % 4]);
}
