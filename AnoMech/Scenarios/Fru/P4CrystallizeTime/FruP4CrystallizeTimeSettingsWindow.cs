using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;
using static AnoMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeStateOverrides;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

public sealed class FruP4CrystallizeTimeSettingsWindow
{
    private static readonly (string Label, CrystallizeDebuff[]? Debuffs)[] PlayerChoices =
    [
        ("Random", null),
        ("Red + Ice", [CrystallizeDebuff.RedIce]),
        ("Red + Aero", [CrystallizeDebuff.RedAero]),
        ("Blue + Darkness", [CrystallizeDebuff.BlueDarkness]),
        ("Blue + Ice / Water / Eruption", [CrystallizeDebuff.BlueIce, CrystallizeDebuff.BlueWater, CrystallizeDebuff.BlueEruption]),
    ];

    private int selected;

    public FruP4CrystallizeTimeStateOverrides Overrides { get; } = new();

    // Solo, the player's own pick sits in this panel; a host assigns every seat from the
    // Multiplayer window instead.
    public void Draw()
    {
        if (ImGui.Button("Auto")) Reset();
        if (SettingsGrid.Begin("##frup4crystallize"))
        {
            if (!PerRole.SeatsActive)
            {
                SettingsGrid.Row("Your debuffs:");
                SettingsGrid.ItemWidth(300);
                var labels = Array.ConvertAll(PlayerChoices, c => c.Label);
                if (ImGui.Combo("##ctplayer", ref selected, labels, labels.Length)) Select(selected);
            }

            var slow = Overrides.SlowNorthwest;
            SettingsGrid.Row("Slowed hourglasses:");
            if (ImGui.RadioButton("Auto##ctslow", slow == null)) Overrides.SlowNorthwest = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("NW + SE##ctslow", slow == true)) Overrides.SlowNorthwest = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("NE + SW##ctslow", slow == false)) Overrides.SlowNorthwest = false;

            var corner = Overrides.Corner;
            SettingsGrid.Row("Last safe corner:");
            if (ImGui.RadioButton("Auto##ctcorner", corner == null)) Overrides.Corner = null;
            foreach (var choice in Enum.GetValues<CrystallizeCorner>())
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{choice}##ctcorner", corner == choice)) Overrides.Corner = choice;
            }
            SettingsGrid.End();
        }
    }

    // Every seat at once. A debuff every one of whose holders is already taken by another seat
    // is greyed out, so the host can't ask for a party the fight can't deal.
    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.SeatDebuffs.Clear();
        if (SettingsGrid.Begin("##frup4crystallizeplayers"))
        {
            foreach (var role in PerRole.All)
            {
                SettingsGrid.Row($"{SettingsGrid.RoleLabel(role)}{Occupant(role)}:");
                SettingsGrid.ItemWidth(220);
                DrawSeatDebuff(role);
            }
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void DrawSeatDebuff(PartyRole role)
    {
        var current = Overrides.SeatDebuffs[role];
        if (!ImGui.BeginCombo($"##ctseat{(int)role}", current is { } chosen ? Label(chosen) : "Auto")) return;
        if (ImGui.Selectable("Auto", current == null)) Overrides.SeatDebuffs[role] = null;
        foreach (var debuff in Enum.GetValues<CrystallizeDebuff>())
        {
            var full = PerRole.All.Count(r => r != role && Overrides.SeatDebuffs[r] == debuff) >= Holders(debuff);
            var flags = full ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;
            if (ImGui.Selectable($"{Label(debuff)}{(full ? " (taken)" : "")}##ct{debuff}", current == debuff, flags))
                Overrides.SeatDebuffs[role] = debuff;
        }
        ImGui.EndCombo();
    }

    private static string Occupant(PartyRole role)
    {
        if (Plugin.Multiplayer is not { } mp) return "";
        return mp.Session.ClaimedBy.TryGetValue(role, out var peerId)
            ? peerId == mp.MyPeerId ? " (you)" : $" ({mp.Session.NameOf(peerId)})"
            : " (bot)";
    }

    private void Select(int index)
    {
        selected = index;
        Overrides.PlayerDebuffs = PlayerChoices[index].Debuffs;
    }

    // Solo's own pick only: a host's seat assignments are kept apart.
    private void Reset()
    {
        Select(0);
        Overrides.SlowNorthwest = null;
        Overrides.Corner = null;
    }
}
