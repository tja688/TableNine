using System;

public static class NineGridOuterRingUtility
{
    public static int Count => BoardSlotUtility.ClockwiseRing.Length;

    public static int GetSlotAt(int index)
    {
        var ring = BoardSlotUtility.ClockwiseRing;
        var safeIndex = ((index % ring.Length) + ring.Length) % ring.Length;
        return ring[safeIndex].Value;
    }

    public static bool IsOuterRingSlot(int slotNo)
    {
        var ring = BoardSlotUtility.ClockwiseRing;
        for (var i = 0; i < ring.Length; i++)
        {
            if (ring[i].Value == slotNo)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetNextClockwiseSlot(int slotNo)
    {
        var ring = BoardSlotUtility.ClockwiseRing;
        for (var i = 0; i < ring.Length; i++)
        {
            if (ring[i].Value == slotNo)
            {
                return ring[(i + 1) % ring.Length].Value;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(slotNo), slotNo, "Slot is not on the nine-grid outer ring.");
    }

    public static bool TryGetFirstEmptySlot(Func<int, bool> isOccupied, out int slotNo)
    {
        if (isOccupied == null)
        {
            throw new ArgumentNullException(nameof(isOccupied));
        }

        var ring = BoardSlotUtility.ClockwiseRing;
        for (var i = 0; i < ring.Length; i++)
        {
            var candidate = ring[i].Value;
            if (!isOccupied(candidate))
            {
                slotNo = candidate;
                return true;
            }
        }

        slotNo = 0;
        return false;
    }
}
