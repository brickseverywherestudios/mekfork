using System.Text.Json;
using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Network;
using static AnoMech.Tests.TestWire;

namespace AnoMech.Tests;

public class PlayerLookTests
{
    private static PlayerLook Hyur()
    {
        var customize = new byte[PlayerLook.CustomizeLength];
        customize[0] = 1;
        customize[1] = 0;
        customize[4] = 2;
        var equipment = Enumerable.Range(0, PlayerLook.EquipmentSlots).Select(i => (ulong)(6000 + i) | 1UL << 16).ToArray();
        return new PlayerLook(customize, equipment, 0x0001_0001_0201UL, 0);
    }

    [Test]
    public void ARealLookPassesAndIsCopied()
    {
        var look = Hyur();
        var checkedLook = NetGuard.Look(look);
        Assert.That(checkedLook, Is.Not.Null);
        Assert.That(checkedLook!.SameAs(look), Is.True);
        Assert.That(checkedLook.Customize, Is.Not.SameAs(look.Customize));
    }

    [TestCase(0, 0, 1)]
    [TestCase(9, 0, 17)]
    [TestCase(1, 2, 1)]
    [TestCase(1, 0, 3)]
    public void AnImpossibleBodyIsDropped(int race, int sex, int tribe)
    {
        var look = Hyur();
        look.Customize[0] = (byte)race;
        look.Customize[1] = (byte)sex;
        look.Customize[4] = (byte)tribe;
        Assert.That(NetGuard.Look(look), Is.Null);
    }

    [Test]
    public void AMisshapenLookIsDropped()
    {
        var look = Hyur();
        Assert.That(NetGuard.Look(look with { Customize = new byte[3] }), Is.Null);
        Assert.That(NetGuard.Look(look with { Equipment = new ulong[11] }), Is.Null);
        Assert.That(NetGuard.Look(null), Is.Null);
    }

    [Test]
    public void ASeatWearsItsPlayersLookAndABotStaysAStandIn()
    {
        var preset = PartyPresets.Standard[(int)PartyRole.MainTank];
        var look = Hyur();
        Assert.That(new NetworkSeat("Tank", 0, look).Dress(preset).Look, Is.SameAs(look));
        Assert.That(new NetworkSeat("Tank", 0).Dress(preset).Look, Is.Null);
        Assert.That(preset.Look, Is.Null);
    }

    [Test]
    public void AHelloCarriesItsLook()
    {
        var id = RelayWire.PeerId(RelayWire.NewSecret());
        var hello = new HelloMessage(id, "Player", "1", "x", Look: Hyur());
        var received = (HelloMessage)Receive(JsonSerializer.SerializeToUtf8Bytes<MpMessage>(hello), false, id);
        Assert.That(received.Look!.SameAs(hello.Look), Is.True);
    }

    [Test]
    public void TheLobbyCarriesEveryLook()
    {
        var session = new MultiplayerSession();
        var peer = Guid.NewGuid();
        session.Looks[peer] = Hyur();
        var bytes = JsonSerializer.SerializeToUtf8Bytes<MpMessage>(session.ToMessage());
        var copy = new MultiplayerSession();
        copy.ApplyLobbyState((LobbyStateMessage)JsonSerializer.Deserialize<MpMessage>(bytes)!);
        Assert.That(copy.LookOf(peer)!.SameAs(session.LookOf(peer)), Is.True);
    }
}
