using System.Collections.Generic;
using NUnit.Framework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class NineGridOuterRingUtilityEditModeTests
{
    [Test]
    public void GetNextClockwiseSlot_Follows_Design_Outer_Ring_Order()
    {
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(1), Is.EqualTo(2));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(2), Is.EqualTo(3));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(3), Is.EqualTo(6));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(6), Is.EqualTo(9));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(9), Is.EqualTo(8));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(8), Is.EqualTo(7));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(7), Is.EqualTo(4));
        Assert.That(NineGridOuterRingUtility.GetNextClockwiseSlot(4), Is.EqualTo(1));
    }

    [Test]
    public void TryGetFirstEmptySlot_Uses_Design_Outer_Ring_Order()
    {
        var occupied = new HashSet<int> { 1, 2, 3 };

        var found = NineGridOuterRingUtility.TryGetFirstEmptySlot(occupied.Contains, out var slotNo);

        Assert.That(found, Is.True);
        Assert.That(slotNo, Is.EqualTo(6));
    }

    [Test]
    public void TryGetFirstEmptySlot_Returns_False_When_Outer_Ring_Is_Full()
    {
        var occupied = new HashSet<int> { 1, 2, 3, 4, 6, 7, 8, 9 };

        var found = NineGridOuterRingUtility.TryGetFirstEmptySlot(occupied.Contains, out var slotNo);

        Assert.That(found, Is.False);
        Assert.That(slotNo, Is.EqualTo(0));
    }
}
