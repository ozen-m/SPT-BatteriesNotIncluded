using System;
using System.Collections.Generic;

namespace BatteriesNotIncluded.Utils;

public static class ListExtensions
{
    public static void SwapRemove<T>(this List<T> list, T toRemove)
    {
        int index = list.IndexOf(toRemove);
        if (index == -1) throw new ArgumentException($"Element {toRemove} not found in list");

        list.SwapRemoveAt(index);
    }

    public static void SwapRemoveAt<T>(this List<T> list, int index)
    {
        int last = list.Count - 1;
        list[index] = list[last];
        list.RemoveAt(last);
    }
}
