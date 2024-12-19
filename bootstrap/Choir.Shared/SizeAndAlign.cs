﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Choir;

public readonly struct Align : IEquatable<Align>, IComparable<Align>
{
    public static readonly Align ByteAligned = new();

    public static int AlignPadding(int value, int align)
    {
        Debug.Assert(align > 0);
        return (align - (value % align)) % align;
    }

    public static long AlignPadding(long value, long align)
    {
        Debug.Assert(align > 0);
        return (align - (value % align)) % align;
    }

    public static ulong AlignPadding(ulong value, ulong align)
    {
        return (align - (value % align)) % align;
    }

    public static int AlignTo(int value, int align) => value + AlignPadding(value, align);
    public static long AlignTo(long value, long align) => value + AlignPadding(value, align);
    public static ulong AlignTo(ulong value, ulong align) => value + AlignPadding(value, align);

    public static Align ForBits(int bits) => ForBytes(AlignTo(bits, 8) / 8);
    public static Align ForBytes(int bytes) => AssumeAligned((int)BitOperations.RoundUpToPowerOf2((uint)bytes));
    public static Align AssumeAligned(int powerOfTwo) => powerOfTwo == 0 ? ByteAligned : new(powerOfTwo);
    //public static Align Of<T>() where T : struct => new(Marshal.SizeOf<T>());

    public static Align Min(Align a, Align b) => a < b ? a : b;
    public static Align Max(Align a, Align b) => a > b ? a : b;

    public static explicit operator Align(int powerOfTwo) => new(powerOfTwo);
    public static explicit operator int(Align align) => align.Bytes;

    public static bool operator ==(Align a, Align b) => a.Equals(b);
    public static bool operator !=(Align a, Align b) => !a.Equals(b);
    public static bool operator <=(Align a, Align b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Align a, Align b) => a.CompareTo(b) >= 0;
    public static bool operator <(Align a, Align b) => a.CompareTo(b) < 0;
    public static bool operator >(Align a, Align b) => a.CompareTo(b) > 0;

    private readonly int _shiftAmount;

    public int Bytes => 1 << _shiftAmount;
    public int Bits => 8 * Bytes;

    public Align Previous
    {
        get
        {
            Debug.Assert(_shiftAmount != 0);
            return new(_shiftAmount - 1);
        }
    }

    public Align(int powerOfTwo)
    {
        Debug.Assert(powerOfTwo > 0, "Alignment must not be 0");
        Debug.Assert(BitOperations.IsPow2(powerOfTwo), "Alignment must be a power of 2");
        _shiftAmount = BitOperations.Log2((uint)powerOfTwo);
        Debug.Assert(_shiftAmount < 64);
    }

    public override int GetHashCode() => _shiftAmount.GetHashCode();
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Align that && Equals(that);
    public bool Equals(Align that) => _shiftAmount == that._shiftAmount;
    public int CompareTo(Align that) => _shiftAmount.CompareTo(that._shiftAmount);
}

public readonly struct Size : IEquatable<Size>, IComparable<Size>
{
    public static Size FromBits(int bits) => new(bits);
    public static Size FromBytes(int bytes) => new(bytes * 8);

    public static bool operator ==(Size a, Size b) => a.Equals(b);
    public static bool operator !=(Size a, Size b) => !a.Equals(b);
    public static bool operator <=(Size a, Size b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Size a, Size b) => a.CompareTo(b) >= 0;
    public static bool operator <(Size a, Size b) => a.CompareTo(b) < 0;
    public static bool operator >(Size a, Size b) => a.CompareTo(b) > 0;

    public static Size operator +(Size lhs, Size rhs) => new(lhs._value + rhs._value);
    public static Size operator -(Size lhs, Size rhs) => new(lhs._value - rhs._value);
    public static Size operator *(Size lhs, int rhs) => new(lhs._value * rhs);
    public static Size operator *(int lhs, Size rhs) => new(rhs._value * lhs);

    private readonly int _value;

    public int Bits => _value;
    public int Bytes => Align.AlignTo(_value, 8) / 8;

    private Size(int value)
    {
        _value = value;
    }

    public Size AlignedTo(Align align) => FromBytes(Align.AlignTo(Bytes, align.Bytes));
    public Size AlignedTo(Size align) => FromBits(Align.AlignTo(Bits, align._value));

    public override int GetHashCode() => _value.GetHashCode();
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Size that && Equals(that);
    public bool Equals(Size that) => _value == that._value;
    public int CompareTo(Size that) => _value.CompareTo(that._value);
}
