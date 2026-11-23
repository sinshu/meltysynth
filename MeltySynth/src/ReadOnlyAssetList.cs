// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.Collections;
using System.Collections.Generic;

namespace MeltySynth
{
    // Unlike Array.AsReadOnly, expose no ICollection.SyncRoot path to the owned array.
    // Prepared once; getters/indexing and internal array-based synthesis do not copy.
    internal sealed class ReadOnlyAssetList<T> : IReadOnlyList<T>
    {
        private readonly T[] items;

        internal ReadOnlyAssetList(T[] items) => this.items = items;

        public int Count => items.Length;
        public T this[int index] => (uint)index < (uint)items.Length
            ? items[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
