using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using Lumina.Excel.Sheets;

namespace AnoMech.Core.Native.Implementations;

internal static unsafe class PlayerLooks
{
    // EquipmentModelId: model, variant, dye, second dye; the bytes above are padding.
    private const ulong EquipmentModelMask = 0xFF_FFFF_FFFF;
    private const ulong GearModelMask = 0xFF_FFFF;
    private const ulong WeaponModelMask = 0xFFFF_FFFF_FFFF;

    // Main thread only (ObjectTable).
    public static PlayerLook? ReadLocal()
    {
        if (Plugin.ObjectTable.LocalPlayer is not { } player || player.Address == 0) return null;
        var draw = &((Character*)player.Address)->DrawData;
        var customize = new byte[PlayerLook.CustomizeLength];
        var source = (byte*)&draw->CustomizeData;
        for (var i = 0; i < customize.Length; i++) customize[i] = source[i];
        var equipment = new ulong[PlayerLook.EquipmentSlots];
        for (var i = 0; i < equipment.Length; i++) equipment[i] = draw->EquipmentModelIds[i].Value & EquipmentModelMask;
        if (draw->IsHatHidden) equipment[(int)DrawDataContainer.EquipmentSlot.Head] = 0;
        var hideWeapons = draw->IsWeaponHidden;
        return new PlayerLook(customize, equipment,
            hideWeapons ? 0 : draw->WeaponData[0].ModelId.Value,
            hideWeapons ? 0 : draw->WeaponData[1].ModelId.Value);
    }

    // A model the Item sheet doesn't know is left out rather than handed to the client.
    public static void Write(SpawnPlayerPacket* packet, PlayerLook look)
    {
        var common = (byte*)packet + 0x20;
        var customize = common + 0x252;
        for (var i = 0; i < PlayerLook.CustomizeLength; i++) customize[i] = look.Customize[i];

        var models = (LegacyEquipmentModelId*)(common + 0x1FC);
        var secondDyes = common + 0x224;
        for (var i = 0; i < PlayerLook.EquipmentSlots; i++)
        {
            var value = look.Equipment[i] & EquipmentModelMask;
            if (value == 0 || !KnownGear.Contains(value & GearModelMask)) continue;
            models[i].Id = (ushort)value;
            models[i].Variant = (byte)(value >> 16);
            models[i].Stain = (byte)(value >> 24);
            secondDyes[i] = (byte)(value >> 32);
        }

        if (IsKnownWeapon(look.MainHand)) packet->Common.MainhandWeaponModel.Value = look.MainHand;
        if (IsKnownWeapon(look.OffHand)) packet->Common.OffhandWeaponModel.Value = look.OffHand;
    }

    private static bool IsKnownWeapon(ulong value) => value != 0 && KnownWeapons.Contains(value & WeaponModelMask);

    private static HashSet<ulong>? knownGear;
    private static HashSet<ulong>? knownWeapons;
    private static HashSet<ulong> KnownGear => knownGear ?? LoadModels().Gear;
    private static HashSet<ulong> KnownWeapons => knownWeapons ?? LoadModels().Weapons;

    private static (HashSet<ulong> Gear, HashSet<ulong> Weapons) LoadModels()
    {
        var gear = new HashSet<ulong>();
        var weapons = new HashSet<ulong>();
        foreach (var item in Plugin.DataManager.GetExcelSheet<Item>())
        {
            foreach (var model in (ulong[])[item.ModelMain, item.ModelSub])
            {
                if (model == 0) continue;
                weapons.Add(model & WeaponModelMask);
                gear.Add((model & 0xFFFF) | ((model >> 16) & 0xFF) << 16);
            }
        }
        knownGear = gear;
        knownWeapons = weapons;
        return (gear, weapons);
    }
}
