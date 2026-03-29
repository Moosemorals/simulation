// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.terrain;

namespace uk.osric.sim.tests;

public sealed class TorusBehaviourTests {
    [Test]
    public void Constructor_WithNonPositiveSize_Throws() {
        Assert.Multiple(() => {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Torus<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Torus<int>(-8));
        });
    }

    [Test]
    public void Constructor_WithNonPowerOfTwoSize_Throws() {
        Assert.Throws<ArgumentException>(() => _ = new Torus<int>(6));
    }

    [Test]
    public void DataConstructor_WithNonPowerOfTwoSize_Throws() {
        int[] data = new int[36];

        Assert.Throws<ArgumentException>(() => _ = new Torus<int>(data, 6));
    }

    [Test]
    public void DataConstructor_WithMismatchedLength_Throws() {
        int[] data = new int[15];

        Assert.Throws<ArgumentException>(() => _ = new Torus<int>(data, 4));
    }

    [Test]
    public void Index_ForPowerOfTwoWrap_MatchesModuloWrappingForNegativeAndLargeCoordinates() {
        const int wrap = 8;

        Assert.Multiple(() => {
            Assert.That(Torus<int>.Index(-1, -1, wrap), Is.EqualTo(ExpectedIndex(-1, -1, wrap)));
            Assert.That(Torus<int>.Index(-9, 15, wrap), Is.EqualTo(ExpectedIndex(-9, 15, wrap)));
            Assert.That(Torus<int>.Index(16, -17, wrap), Is.EqualTo(ExpectedIndex(16, -17, wrap)));
            Assert.That(Torus<int>.Index(int.MinValue, int.MaxValue, wrap), Is.EqualTo(ExpectedIndex(int.MinValue, int.MaxValue, wrap)));
        });
    }

    [Test]
    public void Indexer_WrapsNegativeCoordinatesToEquivalentCell() {
        Torus<int> torus = new(8);

        torus[-1, -1] = 42;

        Assert.Multiple(() => {
            Assert.That(torus[7, 7], Is.EqualTo(42));
            Assert.That(torus[-9, 15], Is.EqualTo(42));
            Assert.That(torus[0, 0], Is.EqualTo(0));
        });
    }

    [Test]
    public void DataConstructor_UsesProvidedBackingStorageWithToroidalIndexing() {
        int[] data = new int[64];
        Torus<int> torus = new(data, 8);

        torus[-1, 0] = 99;

        Assert.Multiple(() => {
            Assert.That(data[7], Is.EqualTo(99));
            Assert.That(torus[7, 0], Is.EqualTo(99));
        });
    }

    private static int ExpectedIndex(int x, int y, int wrap) {
        int wrappedX = Wrap(x, wrap);
        int wrappedY = Wrap(y, wrap);
        return (wrappedY * wrap) + wrappedX;
    }

    private static int Wrap(int coordinate, int size) {
        int wrapped = coordinate % size;
        if (wrapped < 0) {
            wrapped += size;
        }

        return wrapped;
    }
}
