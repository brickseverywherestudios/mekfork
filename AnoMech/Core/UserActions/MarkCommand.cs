using System;
using System.Collections.Generic;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.UserActions;

internal enum MarkFamily { Attack, Bind, Ignore }

internal enum MarkTargetKind { Target, MouseOver, Self, PartySlot, Name }

// Slot is 1-8 for <1>-<8>; Name is the text the game left in place of a placeholder it resolved.
internal readonly record struct MarkTarget(MarkTargetKind Kind, int Slot = 0, string Name = "");

// A specific sign, the next free sign of a family (as /mk attack picks), or neither: "off".
internal readonly record struct MarkRequest(Sign? Sign, MarkFamily? Next, MarkTarget Target)
{
    public bool Off => Sign is null && Next is null;
}

// The game's /marking (/mk) command line: "/mk <sign> [target]".
internal static class MarkCommand
{
    private static readonly Dictionary<string, Sign> Signs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attack1"] = Sign.Attack1, ["attack2"] = Sign.Attack2, ["attack3"] = Sign.Attack3, ["attack4"] = Sign.Attack4,
        ["attack5"] = Sign.Attack5, ["attack6"] = Sign.Attack6, ["attack7"] = Sign.Attack7, ["attack8"] = Sign.Attack8,
        ["bind1"] = Sign.Bind1, ["bind2"] = Sign.Bind2, ["bind3"] = Sign.Bind3,
        ["ignore1"] = Sign.Ignore1, ["ignore2"] = Sign.Ignore2,
        ["square"] = Sign.Square, ["circle"] = Sign.Circle, ["cross"] = Sign.Cross, ["triangle"] = Sign.Triangle,
    };

    private static readonly Dictionary<string, MarkFamily> Families = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attack"] = MarkFamily.Attack, ["bind"] = MarkFamily.Bind, ["ignore"] = MarkFamily.Ignore,
    };

    public static bool TryParse(string text, out MarkRequest request)
    {
        request = default;
        var words = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || !(words[0].Equals("/mk", StringComparison.OrdinalIgnoreCase)
                                  || words[0].Equals("/marking", StringComparison.OrdinalIgnoreCase)))
            return false;

        Sign? sign = null;
        MarkFamily? next = null;
        if (Signs.TryGetValue(words[1], out var s)) sign = s;
        else if (Families.TryGetValue(words[1], out var f)) next = f;
        else if (!words[1].Equals("off", StringComparison.OrdinalIgnoreCase)) return false;

        if (ParseTarget(words.Length > 2 ? string.Join(' ', words[2..]) : null) is not { } target) return false;
        request = new MarkRequest(sign, next, target);
        return true;
    }

    // Null for a placeholder this can't stand in for (<f>, <tt>, ...): the command is left alone.
    private static MarkTarget? ParseTarget(string? word)
    {
        if (word is null) return new MarkTarget(MarkTargetKind.Target);
        switch (word.ToLowerInvariant())
        {
            case "<t>" or "<target>": return new MarkTarget(MarkTargetKind.Target);
            case "<mo>" or "<mouse>": return new MarkTarget(MarkTargetKind.MouseOver);
            case "<me>": return new MarkTarget(MarkTargetKind.Self);
        }
        if (word.Length == 3 && word[0] == '<' && word[2] == '>' && word[1] is >= '1' and <= '8')
            return new MarkTarget(MarkTargetKind.PartySlot, word[1] - '0');
        if (word.StartsWith('<')) return null;
        return new MarkTarget(MarkTargetKind.Name, Name: word);
    }
}
