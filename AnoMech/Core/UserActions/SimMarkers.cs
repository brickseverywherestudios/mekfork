using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AnoMech.Core.UserActions;

// A head marker the player places with /mk inside a sim. The server would place it and tell the
// party; the firewall keeps that request from leaving, so the sim places it as the server would.
// A peer asks the host, whose marks already reach every client.
internal static unsafe class SimMarkers
{
    private const ulong NoObject = 0xE0000000;
    private const int SignCount = 17;

    private static readonly Sign[] Attacks = [Sign.Attack1, Sign.Attack2, Sign.Attack3, Sign.Attack4, Sign.Attack5, Sign.Attack6, Sign.Attack7, Sign.Attack8];
    private static readonly Sign[] Binds = [Sign.Bind1, Sign.Bind2, Sign.Bind3];
    private static readonly Sign[] Ignores = [Sign.Ignore1, Sign.Ignore2];

    public static void OnCommand(string text)
    {
        if (!MarkCommand.TryParse(text, out var request)) return;
        if (Plugin.Game is not { } game || !game.World.Map.IsInInstance) return;
        if (Resolve(game.World.Party, request.Target) is not { } role)
        {
            DiagnosticLog.Info($"[SimMarkers] \"{text}\" names no party member in the sim -- ignored.");
            return;
        }
        if (Plugin.Multiplayer is { SessionCode: not null, IsHost: false } mp)
        {
            mp.ReportMark(request.Sign, request.Next, role);
            return;
        }
        Apply(game.World.Party, request.Sign, request.Next, role);
    }

    // As the game does: a target carries one sign at a time, and asking for a sign (or a family) it
    // already carries takes it off.
    public static void Apply(SimParty party, Sign? sign, MarkFamily? next, PartyRole role)
    {
        if (party.Get(role) is not { } member) return;
        var controller = MarkingController.Instance();
        if (controller == null) return;
        var placed = new ulong[SignCount];
        for (var i = 0; i < SignCount; i++) placed[i] = (ulong)controller->Markers[i];

        var target = (ulong)member.GameObjectId;
        var held = Enum.GetValues<Sign>().Where(s => placed[(int)s] == target).ToList();
        var wanted = sign ?? (next is { } family ? HeldOrNextFree(placed, Family(family), held) : null);

        foreach (var s in held) party.ClearMark(s);
        var placing = wanted is { } w && !held.Contains(w);
        if (placing) party.Mark(wanted!.Value, member);
        DiagnosticLog.Info($"[SimMarkers] {role}: {(placing ? $"{wanted} on" : held.Count > 0 ? $"{string.Join(", ", held)} off" : "nothing to do")}.");
    }

    private static Sign[] Family(MarkFamily family) => family switch
    {
        MarkFamily.Attack => Attacks,
        MarkFamily.Bind => Binds,
        _ => Ignores,
    };

    private static Sign? HeldOrNextFree(ulong[] placed, Sign[] family, List<Sign> held)
    {
        foreach (var s in held)
            if (family.Contains(s)) return s;
        foreach (var s in family)
            if (placed[(int)s] is 0 or NoObject) return s;
        return null;
    }

    private static PartyRole? Resolve(SimParty party, MarkTarget target)
    {
        if (target.Kind == MarkTargetKind.PartySlot) return PartyListSlot(party, target.Slot);
        ulong? objectId = target.Kind switch
        {
            MarkTargetKind.Target => Plugin.TargetManager.Target?.GameObjectId,
            MarkTargetKind.MouseOver => Plugin.TargetManager.MouseOverTarget?.GameObjectId,
            MarkTargetKind.Self => Plugin.ObjectTable.LocalPlayer?.GameObjectId,
            _ => Plugin.ObjectTable.FirstOrDefault(o => o.Name.TextValue.Equals(target.Name, StringComparison.OrdinalIgnoreCase)
                                                        && RoleOf(party, o.GameObjectId) is not null)?.GameObjectId,
        };
        return objectId is { } id ? RoleOf(party, id) : null;
    }

    private static PartyRole? RoleOf(SimParty party, ulong objectId)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (party.Get(role) is { } member && (ulong)member.GameObjectId == objectId) return role;
        return null;
    }

    // The party list shows the player first, then everyone else in role order.
    private static PartyRole? PartyListSlot(SimParty party, int slot)
    {
        var order = Enum.GetValues<PartyRole>().Where(r => r != party.PlayerRole && party.Get(r) is not null).Prepend(party.PlayerRole).ToList();
        return slot >= 1 && slot <= order.Count ? order[slot - 1] : null;
    }
}
