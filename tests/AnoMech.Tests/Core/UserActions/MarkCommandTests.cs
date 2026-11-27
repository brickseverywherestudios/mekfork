using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class MarkCommandTests
{
    [TestCase("/mk triangle <t>", Sign.Triangle)]
    [TestCase("/marking attack1 <t>", Sign.Attack1)]
    [TestCase("/MK Bind3 <t>", Sign.Bind3)]
    [TestCase("  /mk   ignore2   <t> ", Sign.Ignore2)]
    [TestCase("/mk attack8 <t>", Sign.Attack8)]
    public void ASpecificSignIsRead(string text, Sign sign)
    {
        Assert.That(MarkCommand.TryParse(text, out var request), Is.True);
        Assert.That(request.Sign, Is.EqualTo(sign));
        Assert.That(request.Target.Kind, Is.EqualTo(MarkTargetKind.Target));
    }

    [TestCase("/mk attack <t>", MarkFamily.Attack)]
    [TestCase("/mk bind <t>", MarkFamily.Bind)]
    [TestCase("/mk ignore <t>", MarkFamily.Ignore)]
    public void AFamilyPicksTheNextFreeSign(string text, MarkFamily family)
    {
        Assert.That(MarkCommand.TryParse(text, out var request), Is.True);
        Assert.That(request.Sign, Is.Null);
        Assert.That(request.Next, Is.EqualTo(family));
    }

    [Test]
    public void OffClearsTheTarget()
    {
        Assert.That(MarkCommand.TryParse("/mk off <mo>", out var request), Is.True);
        Assert.That(request.Off, Is.True);
        Assert.That(request.Target.Kind, Is.EqualTo(MarkTargetKind.MouseOver));
    }

    [TestCase("/mk triangle", MarkTargetKind.Target, 0, "")]
    [TestCase("/mk triangle <target>", MarkTargetKind.Target, 0, "")]
    [TestCase("/mk triangle <mo>", MarkTargetKind.MouseOver, 0, "")]
    [TestCase("/mk triangle <me>", MarkTargetKind.Self, 0, "")]
    [TestCase("/mk triangle <3>", MarkTargetKind.PartySlot, 3, "")]
    [TestCase("/mk triangle Alice Example", MarkTargetKind.Name, 0, "Alice Example")]
    public void TheTargetIsRead(string text, MarkTargetKind kind, int slot, string name)
    {
        Assert.That(MarkCommand.TryParse(text, out var request), Is.True);
        Assert.That(request.Target, Is.EqualTo(new MarkTarget(kind, slot, name)));
    }

    [TestCase("/mk")]
    [TestCase("/mk sparkle <t>")]
    [TestCase("/mk triangle <f>")]
    [TestCase("/mk triangle <9>")]
    [TestCase("/say /mk triangle <t>")]
    [TestCase("/marker triangle <t>")]
    public void AnythingElseIsLeftAlone(string text)
        => Assert.That(MarkCommand.TryParse(text, out _), Is.False);
}
