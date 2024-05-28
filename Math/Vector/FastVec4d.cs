using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#nullable enable
namespace Vintagestory.API.MathTools
{
    using V256 = Vector256;
    /// <summary>
    /// Represents a vector of 4 floats, which is slightly easier for the processor than a vector
    /// of 3 floats. This is an immutable data type! Rather than changing individual fields,
    /// reassign the vector as a whole:
    /// <code>
    /// FastVec4d vector = new(0, 0, 0, 1);
    ///
    /// // WRONG:
    /// vector.X = 5; // Error: field X is readonly!
    ///
    /// // RIGHT:
    /// vector = vector with {
    ///     X = 5,
    /// };
    /// </code>
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 16)]
    public readonly struct FastVec4d : IFastVec4<FastVec4d, SIMDVec4d, double>
    {
        // Zero is a property rather than a field because it's always easier for the processor to
        // obtain a zero directly than it is to load it from memory. Since these are transient
        // values in registers rather than bytes stored in memory, we return the raw SIMD vector
        // type in order to avoid the compiler thinking it has to emit conversions.
        public static SIMDVec4d Zero => SIMDVec4d.Zero;
        public static SIMDVec4d AllBitsSet => SIMDVec4d.AllBitsSet;
        public static readonly FastVec4d One = Fill(1);
        public static readonly FastVec4d NegativeOne = -One;
        public static readonly FastVec4d MaxValue = Fill(double.MaxValue);
        public static readonly FastVec4d MinValue = Fill(double.MinValue);
        public static readonly FastVec4d UnitX = (1, 0, 0, 0);
        public static readonly FastVec4d UnitY = (0, 1, 0, 0);
        public static readonly FastVec4d UnitZ = (0, 0, 1, 0);
        public static readonly FastVec4d UnitW = (0, 0, 0, 1);
        public static readonly FastVec4d MaskAll = AllBitsSet;
        public static readonly FastVec4d MaskX = MaskAll with { Y = 0, Z = 0, W = 0 };
        public static readonly FastVec4d MaskY = MaskAll with { X = 0, Z = 0, W = 0 };
        public static readonly FastVec4d MaskZ = MaskAll with { X = 0, Y = 0, W = 0 };
        public static readonly FastVec4d MaskXYZ = MaskAll with { W = 0 };
        public static readonly FastVec4d MaskW = MaskAll with { X = 0, Y = 0, Z = 0 };

        /// <summary>
        /// The vector as a four-component SIMD value, for math manipulations
        /// </summary>
        [FieldOffset(0)]
        public readonly SIMDVec4d SIMDVector;
        /// <summary>
        /// The vector as a four-component tuple
        /// </summary>
        [FieldOffset(0)]
        public readonly (double x, double y, double z, double w) Tuple;

        [FieldOffset(0)]
        private readonly double x_;
        [FieldOffset(8)]
        private readonly double y_;
        [FieldOffset(16)]
        private readonly double z_;
        [FieldOffset(24)]
        private readonly double w_;

        /// <summary>
        /// The first component of the vector (X)
        /// </summary>
        public readonly double X { get => x_; init => x_ = value; }
        /// <summary>
        /// The second component of the vector (Y)
        /// </summary>
        public readonly double Y { get => y_; init => y_ = value; }
        /// <summary>
        /// The third component of the vector (Z)
        /// </summary>
        public readonly double Z { get => z_; init => z_ = value; }
        /// <summary>
        /// The fourth component of the vector (W)
        /// </summary>
        public readonly double W { get => w_; init => w_ = value; }

        public readonly double R { get => x_; init => x_ = value; }
        public readonly double G { get => y_; init => y_ = value; }
        public readonly double B { get => z_; init => z_ = value; }
        public readonly double A { get => w_; init => w_ = value; }

        public ref readonly double this[int component] => ref MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x_), 4)[component];
        public int Count => 4;

        #region Constructors and conversions
        // Each of these three primary constructors (bare component, tuple, and Vector256) set the fields for one of the
        // three views of this union. Unsafe.SkipInit is used on the fields belonging to the other views of the union
        // so that the compiler doesn't zero them out before setting the data. Since the actual data assignment is done as
        // a standard C# assignment, this allows the data path to remain visible to the optimizer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4d Of(double x, double y, double z, double w = 0) => V256.Create(x, y, z, w);
        public FastVec4d(double x, double y, double z, double w)
        {
            Unsafe.SkipInit(out SIMDVector);
            Unsafe.SkipInit(out Tuple);
            x_ = x;
            y_ = y;
            z_ = z;
            w_ = w;
        }

        public FastVec4d(in (double x, double y, double z, double w) tuple)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out SIMDVector);
            Tuple = tuple;
        }

        public FastVec4d(in SIMDVec4d vec128)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out Tuple);
            SIMDVector = vec128;
        }

        public static SIMDVec4d Fill(double fillValue)
        {
            return V256.Create(fillValue);
        }

        // Vector256.Create() does proper input checking
        public FastVec4d(ReadOnlySpan<double> components)
            : this(V256.Create(components))
        {
        }

        public FastVec4d(double[] components)
            : this(V256.Create(components))
        {
        }

        public FastVec4d(FastVec3d vec)
            : this((vec.X, vec.Y, vec.Z, 0f))
        {
        }

        public FastVec4d(Vec4d vec)
            : this((vec.X, vec.Y, vec.Z, vec.W))
        {
        }

        public FastVec4d(Vec3d vec)
            : this((vec.X, vec.Y, vec.Z, 0f))
        {
        }


        public static implicit operator SIMDVec4d(in FastVec4d self) => self.SIMDVector;
        public static implicit operator FastVec4d(in SIMDVec4d vec128) => FromVector256(ref Unsafe.AsRef(vec128));

        public static implicit operator FastVec4d(in (double x, double y, double z, double w) tuple) => new(tuple);
        public static implicit operator (double x, double y, double z, double w)(in FastVec4d self) => self.Tuple;

        public static implicit operator FastVec4d(in FastVec3d vec) => new(vec);
        public static explicit operator FastVec3d(in FastVec4d self) => new(self.X, self.Y, self.Z);

        public static explicit operator FastVec4d(in FastVec4f fvec) => (FastVec4d)fvec.SIMDVector.ToDouble();
        public static explicit operator FastVec4f(in FastVec4d dvec) => (FastVec4f)dvec.SIMDVector.ToFloat();

        public static explicit operator SIMDVec4f(in FastVec4d self) => Vector128.Narrow(self.SIMDVector.GetLower(), self.SIMDVector.GetUpper());
        public static explicit operator SIMDVec4i(in FastVec4d self) => Vector128.ConvertToInt32((SIMDVec4f)self);

        // Conversion from raw SIMD vectors of other types
        public static explicit operator FastVec4d(in SIMDVec4f vec128) => vec128.ToDouble();
        public static explicit operator FastVec4d(in SIMDVec4i vec128) => vec128.ToDouble();

        public readonly SIMDVec4i ToInt() => (SIMDVec4i)this;
        public readonly SIMDVec4f ToFloat() => (SIMDVec4f)this;

        // These methods reinterpret an existing referenced FastVec4d, Vector256, or 4-component tuple as one of the other kinds, without incurring a copy.
        // These should only be used when you have an existing hard reference to one of these types! Otherwise the implicit conversions above are better.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly SIMDVec4d AsVector256(ref FastVec4d self) => ref Unsafe.As<FastVec4d, SIMDVec4d>(ref self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly (double x, double y, double z, double w) AsTuple(ref FastVec4d self) => ref Unsafe.As<FastVec4d, (double x, double y, double z, double w)>(ref self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref FastVec4d FromVector256(ref SIMDVec4d vec) => ref Unsafe.As<SIMDVec4d, FastVec4d>(ref vec);
        // Conversion from tuple requires a copy because tuples don't have the alignment guarantees
        public static FastVec4d FromTuple(ref (double x, double y, double z, double w) tuple) => new(V256.LoadUnsafe(ref tuple.x));
        #endregion

        /// <summary>
        /// Allows the use of deconstruction syntax, i.e.:
        /// <code>
        /// FastVec4d vec4i = default;
        /// (double x, double y, double z, double w) = vec4i;
        /// </code>
        /// </summary>
        public void Deconstruct(out double x, out double y, out double z, out double w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public override bool Equals(object? obj) => obj is FastVec4d i && Equals(i);
        bool IEquatable<FastVec4d>.Equals(FastVec4d other) => Equals(other);
        public bool Equals(in FastVec4d other) => SIMDVector == other.SIMDVector;
        public override int GetHashCode() => SIMDVector.GetHashCode();

        public double Dot(in FastVec4d other) => SIMDVector.Dot(other.SIMDVector);
        public SIMDVec4d Abs() => SIMDVector.Abs();
        public double LengthSquared() => SIMDVector.Dot(SIMDVector);
        public double Length() => Math.Sqrt(LengthSquared());
        public double Distance(in FastVec4d other) => (other - this).Length();
        public double DistanceSq(in FastVec4d other) => (other - this).LengthSquared();

        public SIMDVec4d EqualTo(in SIMDVec4d other) => V256.Equals(SIMDVector, other);
        public bool EqualToAll(in SIMDVec4d other) => V256.EqualsAll(SIMDVector, other);
        public bool EqualToAny(in SIMDVec4d other) => V256.EqualsAny(SIMDVector, other);

        public SIMDVec4d GreaterThan(in SIMDVec4d other) => V256.GreaterThan(SIMDVector, other);
        public bool GreaterThanAll(in SIMDVec4d other) => V256.GreaterThanAll(SIMDVector, other);
        public bool GreaterThanAny(in SIMDVec4d other) => V256.GreaterThanAny(SIMDVector, other);
        public SIMDVec4d GreaterThanOrEqual(in SIMDVec4d other) => V256.GreaterThanOrEqual(SIMDVector, other);
        public bool GreaterThanOrEqualAll(in SIMDVec4d other) => V256.GreaterThanOrEqualAll(SIMDVector, other);
        public bool GreaterThanOrEqualAny(in SIMDVec4d other) => V256.GreaterThanOrEqualAny(SIMDVector, other);

        public SIMDVec4d LessThan(in SIMDVec4d other) => V256.LessThan(SIMDVector, other);
        public bool LessThanAll(in SIMDVec4d other) => V256.LessThanAll(SIMDVector, other);
        public bool LessThanAny(in SIMDVec4d other) => V256.LessThanAny(SIMDVector, other);
        public SIMDVec4d LessThanOrEqual(in SIMDVec4d other) => V256.LessThanOrEqual(SIMDVector, other);
        public bool LessThanOrEqualAll(in SIMDVec4d other) => V256.LessThanOrEqualAll(SIMDVector, other);
        public bool LessThanOrEqualAny(in SIMDVec4d other) => V256.LessThanOrEqualAny(SIMDVector, other);

        public SIMDVec4d AndNot(in SIMDVec4d other) => V256.AndNot(SIMDVector, other);

        public static SIMDVec4d Min(in FastVec4d left, in FastVec4d right) => Min(left.SIMDVector, right.SIMDVector);
        public static SIMDVec4d Max(in FastVec4d left, in FastVec4d right) => Max(left.SIMDVector, right.SIMDVector);
        public SIMDVec4d Min(in FastVec4d other) => Min(this, other);
        public SIMDVec4d Max(in FastVec4d other) => Max(other.SIMDVector);
        public SIMDVec4d Clamp(in FastVec4d min, in FastVec4d max) => SIMDVector.Clamp(min, max);
        public SIMDVec4d Clamp(double min, double max) => SIMDVector.Clamp(Fill(min), Fill(max));

        public SIMDVec4d ConditionalSelect(in SIMDVec4d valueIfOne, in SIMDVec4d valueIfZero) => V256.ConditionalSelect(SIMDVector, valueIfOne, valueIfZero);

        #region Operators
        public static bool operator ==(in FastVec4d left, in FastVec4d right) => left.SIMDVector == right.SIMDVector;
        public static bool operator !=(in FastVec4d left, in FastVec4d right) => !(left == right);
        public static bool operator ==(in FastVec4d left, in (double x, double y, double z, double w) right) => left.SIMDVector == right.ToSIMD();
        public static bool operator !=(in FastVec4d left, in (double x, double y, double z, double w) right) => !(left == right);
        public static SIMDVec4d operator +(in FastVec4d left, in FastVec4d right) => left.SIMDVector + right.SIMDVector;
        public static SIMDVec4d operator -(in FastVec4d left, in FastVec4d right) => left.SIMDVector - right.SIMDVector;
        public static SIMDVec4d operator *(in FastVec4d left, in FastVec4d right) => left.SIMDVector * right.SIMDVector;
        public static SIMDVec4d operator /(in FastVec4d left, in FastVec4d right) => left.SIMDVector / right.SIMDVector;
        public static SIMDVec4d operator +(in FastVec4d left, in (double x, double y, double z, double w) right) => left.SIMDVector + right.ToSIMD();
        public static SIMDVec4d operator -(in FastVec4d left, in (double x, double y, double z, double w) right) => left.SIMDVector - right.ToSIMD();
        public static SIMDVec4d operator *(in FastVec4d left, in (double x, double y, double z, double w) right) => left.SIMDVector * right.ToSIMD();
        public static SIMDVec4d operator /(in FastVec4d left, in (double x, double y, double z, double w) right) => left.SIMDVector / right.ToSIMD();
        public static SIMDVec4d operator +(in FastVec4d left, in (double x, double y, double z) right) => left.SIMDVector + right.ToSIMD(0);
        public static SIMDVec4d operator -(in FastVec4d left, in (double x, double y, double z) right) => left.SIMDVector - right.ToSIMD(0);
        public static SIMDVec4d operator *(in FastVec4d left, in (double x, double y, double z) right) => left.SIMDVector * right.ToSIMD(1);
        public static SIMDVec4d operator /(in FastVec4d left, in (double x, double y, double z) right) => left.SIMDVector / right.ToSIMD(1);
        public static SIMDVec4d operator *(in FastVec4d left, double right) => left.SIMDVector * right;
        public static SIMDVec4d operator /(in FastVec4d left, double right) => left * (1 / right);
        // unary - and +
        public static SIMDVec4d operator -(in FastVec4d value) => -value.SIMDVector;
        public static SIMDVec4d operator +(in FastVec4d value) => value.SIMDVector;
        #endregion

        public override string ToString() => $"x={X}, y={Y}, z={Z}, w={W}";

        #region IVec3 implemementation
        int IVec3.XAsInt => (int)X;
        int IVec3.YAsInt => (int)Y;
        int IVec3.ZAsInt => (int)Z;
        double IVec3.XAsDouble => X;
        double IVec3.YAsDouble => Y;
        double IVec3.ZAsDouble => Z;
        float IVec3.XAsFloat => (float)X;
        float IVec3.YAsFloat => (float)Y;
        float IVec3.ZAsFloat => (float)Z;
        Vec3i IVec3.AsVec3i => new((int)X, (int)Y, (int)Z);
        #endregion
    }
}
