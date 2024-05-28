using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#nullable enable
namespace Vintagestory.API.MathTools
{
    using V128 = Vector128;

    /// <summary>
    /// Represents a vector of 4 floats, which is slightly easier for the processor than a vector
    /// of 3 floats. This is an immutable data type! Rather than changing individual fields,
    /// reassign the vector as a whole:
    /// <code>
    /// FastVec4f vector = new(0, 0, 0, 1);
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
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 16)]
    public readonly struct FastVec4f : IFastVec4<FastVec4f, SIMDVec4f, float>
    {
        // Zero is a property rather than a field because it's always easier for the processor to
        // obtain a zero directly than it is to load it from memory
        public static FastVec4f Zero => SIMDVec4f.Zero;
        public static readonly FastVec4f One = Fill(1);
        public static readonly FastVec4f NegativeOne = -One;
        public static readonly FastVec4f MaxValue = Fill(float.MaxValue);
        public static readonly FastVec4f MinValue = Fill(float.MinValue);
        public static readonly FastVec4f UnitX = (1, 0, 0, 0);
        public static readonly FastVec4f UnitY = (0, 1, 0, 0);
        public static readonly FastVec4f UnitZ = (0, 0, 1, 0);
        public static readonly FastVec4f UnitW = (0, 0, 0, 1);

        /// <summary>
        /// The vector as a four-component SIMD value, for math manipulations
        /// </summary>
        [FieldOffset(0)]
        public readonly SIMDVec4f SIMDVector;
        /// <summary>
        /// The vector as a four-component tuple
        /// </summary>
        [FieldOffset(0)]
        public readonly (float x, float y, float z, float w) Tuple;

        [FieldOffset(0)]
        private readonly float x_;
        [FieldOffset(4)]
        private readonly float y_;
        [FieldOffset(8)]
        private readonly float z_;
        [FieldOffset(12)]
        private readonly float w_;

        /// <summary>
        /// The first component of the vector (X)
        /// </summary>
        public readonly float X { get => x_; init => x_ = value; }
        /// <summary>
        /// The second component of the vector (Y)
        /// </summary>
        public readonly float Y { get => y_; init => y_ = value; }
        /// <summary>
        /// The third component of the vector (Z)
        /// </summary>
        public readonly float Z { get => z_; init => z_ = value; }
        /// <summary>
        /// The fourth component of the vector (W)
        /// </summary>
        public readonly float W { get => w_; init => w_ = value; }

        public readonly float R { get => x_; init => x_ = value; }
        public readonly float G { get => y_; init => y_ = value; }
        public readonly float B { get => z_; init => z_ = value; }
        public readonly float A { get => w_; init => w_ = value; }

        public ref readonly float this[int component] => ref MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x_), 4)[component];
        public int Count => 4;

        #region Constructors and conversions
        // Each of these three primary constructors (bare component, tuple, and Vector128) set the fields for one of the
        // three views of this union. Unsafe.SkipInit is used on the fields belonging to the other views of the union
        // so that the compiler doesn't zero them out before setting the data. Since the actual data assignment is done as
        // a standard C# assignment, this allows the data path to remain visible to the optimizer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastVec4f New(float x, float y, float z, float w) => V128.Create(x, y, z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastVec4f(float x, float y, float z, float w)
        {
            Unsafe.SkipInit(out SIMDVector);
            Unsafe.SkipInit(out Tuple);
            x_ = x;
            y_ = y;
            z_ = z;
            w_ = w;
        }

        public FastVec4f(in (float x, float y, float z, float w) tuple)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out SIMDVector);
            Tuple = tuple;
        }

        public FastVec4f(in SIMDVec4f vec128)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out Tuple);
            SIMDVector = vec128;
        }

        public static SIMDVec4f Fill(float fillValue)
        {
            return V128.Create(fillValue);
        }

        // Vector128.Create() does proper input checking
        public FastVec4f(ReadOnlySpan<float> components)
            : this(V128.Create(components))
        {
        }

        public FastVec4f(float[] components)
            : this(V128.Create(components))
        {
        }

        public FastVec4f(FastVec3f vec)
            : this((vec.X, vec.Y, vec.Z, 0f))
        {
        }

        public FastVec4f(Vec4f vec)
            : this((vec.X, vec.Y, vec.Z, vec.W))
        {
        }

        public FastVec4f(Vec3f vec)
            : this((vec.X, vec.Y, vec.Z, 0f))
        {
        }


        public static implicit operator SIMDVec4f(in FastVec4f self) => self.SIMDVector;
        // A Vector128 can be safely reinterpreted as a FastVec4f, since both are immutable and both share alignment characteristics
        public static implicit operator FastVec4f(in SIMDVec4f vec128) => FromVector128(ref Unsafe.AsRef(vec128));

        public static implicit operator FastVec4f(in (float x, float y, float z, float w) tuple) => new(tuple);
        public static implicit operator (float x, float y, float z, float w)(in FastVec4f self) => self.Tuple;

        public static implicit operator FastVec4f(in FastVec3f vec) => new(vec);
        public static explicit operator FastVec3f(in FastVec4f self) => new(self.X, self.Y, self.Z);

        // Same type promotion rules as basic types: promote int ⇒ float ⇒ double is implicit, demote int ⇐ float ⇐ double is explicit
        public static implicit operator FastVec4f(in FastVec4i ivec) => V128.ConvertToSingle(ivec.SIMDVector);
        public static explicit operator FastVec4i(in FastVec4f fvec) => V128.ConvertToInt32(fvec.SIMDVector);

        // Float fastvecs may be implicitly promoted to double but require explicit demotion to int
        public static implicit operator SIMDVec4d(in FastVec4f self) => V128.Widen(self.SIMDVector) is (Vector128<double> lower, Vector128<double> upper)
                                                                              ? Vector256.Create(lower, upper) : throw new UnreachableException();
        public static explicit operator SIMDVec4i(in FastVec4f self) => V128.ConvertToInt32(self.SIMDVector);
        // Conversions (back) to FastVec
        public static explicit operator FastVec4f(in SIMDVec4d vec256) => V128.Narrow(vec256.GetLower(), vec256.GetUpper());
        public static explicit operator FastVec4f(in SIMDVec4i vec128) => V128.ConvertToSingle(vec128);

        public readonly SIMDVec4d ToDouble() => this;
        public readonly SIMDVec4i ToInt() => (SIMDVec4i)this;

        // These methods reinterpret an existing referenced FastVec4f, Vector128, or 4-component tuple as one of the other kinds, without incurring a copy.
        // These should only be used when you have an existing hard reference to one of these types! Otherwise the implicit conversions above are better.
        public static ref readonly SIMDVec4f AsVector128(ref FastVec4f self) => ref Unsafe.As<FastVec4f, SIMDVec4f>(ref self);
        public static ref readonly (float x, float y, float z, float w) AsTuple(ref FastVec4f self) => ref Unsafe.As<FastVec4f, (float x, float y, float z, float w)>(ref self);
        public static ref readonly FastVec4f FromVector128(ref SIMDVec4f vec) => ref Unsafe.As<SIMDVec4f, FastVec4f>(ref vec);
        // Conversion from tuple requires a copy because tuples don't have the alignment guarantees
        public static FastVec4f FromTuple(ref (float x, float y, float z, float w) tuple) => new(V128.LoadUnsafe(ref tuple.x));
        #endregion

        /// <summary>
        /// Allows the use of deconstruction syntax, i.e.:
        /// <code>
        /// FastVec4f vec4i = default;
        /// (float x, float y, float z, float w) = vec4i;
        /// </code>
        /// </summary>
        public void Deconstruct(out float x, out float y, out float z, out float w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public override bool Equals(object? obj) => obj is FastVec4f i && Equals(i);
        bool IEquatable<FastVec4f>.Equals(FastVec4f other) => Equals(other);
        public bool Equals(in FastVec4f other) => SIMDVector == other.SIMDVector;
        public override int GetHashCode() => SIMDVector.GetHashCode();

        public float Dot(in FastVec4f other) => SIMDVector.Dot(other.SIMDVector);
        public SIMDVec4f Abs() => SIMDVector.Abs();
        public float LengthSquared() => SIMDVector.Dot(SIMDVector);
        public double Length() => Math.Sqrt(LengthSquared());
        public double Distance(in FastVec4f other) => (other - this).Length();
        public double DistanceSq(in FastVec4f other) => (other - this).LengthSquared();

        public SIMDVec4f GreaterThan(in SIMDVec4f other) => V128.GreaterThan(SIMDVector, other);
        public bool GreaterThanAll(in SIMDVec4f other) => V128.GreaterThanAll(SIMDVector, other);
        public bool GreaterThanAny(in SIMDVec4f other) => V128.GreaterThanAny(SIMDVector, other);
        public SIMDVec4f GreaterThanOrEqual(in SIMDVec4f other) => V128.GreaterThanOrEqual(SIMDVector, other);
        public bool GreaterThanOrEqualAll(in SIMDVec4f other) => V128.GreaterThanOrEqualAll(SIMDVector, other);
        public bool GreaterThanOrEqualAny(in SIMDVec4f other) => V128.GreaterThanOrEqualAny(SIMDVector, other);

        public SIMDVec4f LessThan(in SIMDVec4f other) => V128.LessThan(SIMDVector, other);
        public bool LessThanAll(in SIMDVec4f other) => V128.LessThanAll(SIMDVector, other);
        public bool LessThanAny(in SIMDVec4f other) => V128.LessThanAny(SIMDVector, other);
        public SIMDVec4f LessThanOrEqual(in SIMDVec4f other) => V128.LessThanOrEqual(SIMDVector, other);
        public bool LessThanOrEqualAll(in SIMDVec4f other) => V128.LessThanOrEqualAll(SIMDVector, other);
        public bool LessThanOrEqualAny(in SIMDVec4f other) => V128.LessThanOrEqualAny(SIMDVector, other);

        public static SIMDVec4f Min(in FastVec4f left, in FastVec4f right) => SIMDVec.Min(left.SIMDVector, right.SIMDVector);
        public static SIMDVec4f Max(in FastVec4f left, in FastVec4f right) => SIMDVec.Max(left.SIMDVector, right.SIMDVector);
        public SIMDVec4f Min(in FastVec4f other) => Min(this, other);
        public SIMDVec4f Max(in FastVec4f other) => Max(other.SIMDVector);
        public SIMDVec4f Clamp(in FastVec4f min, in FastVec4f max) => SIMDVector.Clamp(min, max);
        public SIMDVec4f Clamp(float min, float max) => SIMDVector.Clamp(Fill(min), Fill(max));

        #region Operators
        public static bool operator ==(in FastVec4f left, in FastVec4f right) => left.SIMDVector == right.SIMDVector;
        public static bool operator !=(in FastVec4f left, in FastVec4f right) => !(left == right);
        public static bool operator ==(in FastVec4f left, in (float x, float y, float z, float w) right) => left.SIMDVector == right.ToSIMD();
        public static bool operator !=(in FastVec4f left, in (float x, float y, float z, float w) right) => !(left == right);
        public static SIMDVec4f operator +(in FastVec4f left, in FastVec4f right) => left.SIMDVector + right.SIMDVector;
        public static SIMDVec4f operator -(in FastVec4f left, in FastVec4f right) => left.SIMDVector - right.SIMDVector;
        public static SIMDVec4f operator *(in FastVec4f left, in FastVec4f right) => left.SIMDVector * right.SIMDVector;
        public static SIMDVec4f operator /(in FastVec4f left, in FastVec4f right) => left.SIMDVector / right.SIMDVector;
        public static SIMDVec4f operator +(in FastVec4f left, in (float x, float y, float z, float w) right) => left.SIMDVector + right.ToSIMD();
        public static SIMDVec4f operator -(in FastVec4f left, in (float x, float y, float z, float w) right) => left.SIMDVector - right.ToSIMD();
        public static SIMDVec4f operator *(in FastVec4f left, in (float x, float y, float z, float w) right) => left.SIMDVector * right.ToSIMD();
        public static SIMDVec4f operator /(in FastVec4f left, in (float x, float y, float z, float w) right) => left.SIMDVector / right.ToSIMD();
        public static SIMDVec4f operator +(in FastVec4f left, in (float x, float y, float z) right) => left.SIMDVector + right.ToSIMD(0);
        public static SIMDVec4f operator -(in FastVec4f left, in (float x, float y, float z) right) => left.SIMDVector - right.ToSIMD(0);
        public static SIMDVec4f operator *(in FastVec4f left, in (float x, float y, float z) right) => left.SIMDVector * right.ToSIMD(1);
        public static SIMDVec4f operator /(in FastVec4f left, in (float x, float y, float z) right) => left.SIMDVector / right.ToSIMD(1);
        public static SIMDVec4f operator *(in FastVec4f left, float right) => left.SIMDVector * right;
        public static SIMDVec4f operator /(in FastVec4f left, float right) => left * (1 /right);
        // unary - and +
        public static SIMDVec4f operator -(in FastVec4f value) => -value.SIMDVector;
        public static SIMDVec4f operator +(in FastVec4f value) => value.SIMDVector;
        #endregion

        public override string ToString() => $"x={X}, y={Y}, z={Z}, w={W}";

        #region IVec3 implemementation
        int IVec3.XAsInt => (int)X;
        int IVec3.YAsInt => (int)Y;
        int IVec3.ZAsInt => (int)Z;
        double IVec3.XAsDouble => X;
        double IVec3.YAsDouble => Y;
        double IVec3.ZAsDouble => Z;
        float IVec3.XAsFloat => X;
        float IVec3.YAsFloat => Y;
        float IVec3.ZAsFloat => Z;
        Vec3i IVec3.AsVec3i => new((int)X, (int)Y, (int)Z);
        #endregion
    }
}
