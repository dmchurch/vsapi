using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#nullable enable
namespace Vintagestory.API.MathTools
{
    using V128 = Vector128;

    /// <summary>
    /// Represents a vector of 4 ints, which is slightly easier for the processor than a vector
    /// of 3 ints. This is an immutable data type! Rather than changing individual fields,
    /// reassign the vector as a whole:
    /// <code>
    /// FastVec4i vector = new(0, 0, 0, 1);
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
    public readonly struct FastVec4i : IEquatable<FastVec4i>, IVec3
    {
        public static readonly FastVec4i Zero = default;
        public static readonly FastVec4i One = Fill(1);
        public static readonly FastVec4i NegativeOne = -One;
        public static readonly FastVec4i MaxValue = Fill(int.MaxValue);
        public static readonly FastVec4i MinValue = Fill(int.MinValue);
        public static readonly FastVec4i UnitX = (1, 0, 0, 0);
        public static readonly FastVec4i UnitY = (0, 1, 0, 0);
        public static readonly FastVec4i UnitZ = (0, 0, 1, 0);
        public static readonly FastVec4i UnitW = (0, 0, 0, 1);

        /// <summary>
        /// The vector as a four-component SIMD value, for math manipulations
        /// </summary>
        [FieldOffset(0)]
        public readonly SIMDVec4i SIMDVector;
        /// <summary>
        /// The vector as a four-component tuple
        /// </summary>
        [FieldOffset(0)]
        public readonly (int x, int y, int z, int w) Tuple;

        [FieldOffset(0)]
        private readonly int x_;
        [FieldOffset(4)]
        private readonly int y_;
        [FieldOffset(8)]
        private readonly int z_;
        [FieldOffset(12)]
        private readonly int w_;

        /// <summary>
        /// The first component of the vector (X)
        /// </summary>
        public readonly int X { get => x_; init => x_ = value; }
        /// <summary>
        /// The second component of the vector (Y)
        /// </summary>
        public readonly int Y { get => y_; init => y_ = value; }
        /// <summary>
        /// The third component of the vector (Z)
        /// </summary>
        public readonly int Z { get => z_; init => z_ = value; }
        /// <summary>
        /// The fourth component of the vector (W)
        /// </summary>
        public readonly int W { get => w_; init => w_ = value; }

        public readonly int R { get => x_; init => x_ = value; }
        public readonly int G { get => y_; init => y_ = value; }
        public readonly int B { get => z_; init => z_ = value; }
        public readonly int A { get => w_; init => w_ = value; }

        public ref readonly int this[int component] => ref MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x_), 4)[component];
        public int Count => 4;

        #region Constructors and conversions
        // Each of these three primary constructors (bare component, tuple, and Vector128) set the fields for one of the
        // three views of this union. Unsafe.SkipInit is used on the fields belonging to the other views of the union
        // so that the compiler doesn't zero them out before setting the data. Since the actual data assignment is done as
        // a standard C# assignment, this allows the data path to remain visible to the optimizer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastVec4i Of(int x, int y, int z, int w) => V128.Create(x, y, z, w);
        public FastVec4i(int x, int y, int z, int w)
        {
            Unsafe.SkipInit(out SIMDVector);
            Unsafe.SkipInit(out Tuple);
            x_ = x;
            y_ = y;
            z_ = z;
            w_ = w;
        }

        public FastVec4i(in (int x, int y, int z, int w) tuple)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out SIMDVector);
            Tuple = tuple;
        }

        public FastVec4i(in SIMDVec4i vec128)
        {
            Unsafe.SkipInit(out x_);
            Unsafe.SkipInit(out y_);
            Unsafe.SkipInit(out z_);
            Unsafe.SkipInit(out w_);
            Unsafe.SkipInit(out Tuple);
            SIMDVector = vec128;
        }

        public static SIMDVec4i Fill(int fillValue)
        {
            return V128.Create(fillValue);
        }

        // Vector128.Create() does proper input checking
        public FastVec4i(ReadOnlySpan<int> components)
            : this(V128.Create(components))
        {
        }

        public FastVec4i(int[] components)
            : this(V128.Create(components))
        {
        }

        public FastVec4i(FastVec3i vec)
            : this((vec.X, vec.Y, vec.Z, 0))
        {
        }

        public FastVec4i(Vec4i vec)
            : this((vec.X, vec.Y, vec.Z, vec.W))
        {
        }

        public FastVec4i(Vec3i vec)
            : this((vec.X, vec.Y, vec.Z, 0))
        {
        }

        public static implicit operator SIMDVec4i(in FastVec4i self) => self.SIMDVector;
        // A Vector128 can be safely reinterpreted as a FastVec4i, since both are immutable and both share alignment characteristics
        public static implicit operator FastVec4i(in SIMDVec4i vec128) => FromVector128(ref Unsafe.AsRef(vec128));

        public static implicit operator FastVec4i(in (int x, int y, int z, int w) tuple) => new(tuple);
        public static explicit operator (int x, int y, int z, int w)(in FastVec4i self) => self.Tuple;

        public static explicit operator FastVec4i(in FastVec3i vec) => new(vec);
        public static explicit operator FastVec3i(in FastVec4i self) => new(self.X, self.Y, self.Z);

        public static explicit operator SIMDVec4l(in FastVec4i self) => self.SIMDVector.ToLong();
        public static explicit operator SIMDVec4d(in FastVec4i self) => self.SIMDVector.ToDouble();
        public static explicit operator SIMDVec4f(in FastVec4i self) => self.SIMDVector.ToFloat();
        // Reverse conversions
        public static explicit operator FastVec4i(in SIMDVec4l vec256) => new(vec256.ToInt());
        public static explicit operator FastVec4i(in SIMDVec4d vec256) => new(vec256.ToInt());
        public static explicit operator FastVec4i(in SIMDVec4f vec128) => new(vec128.ToInt());

        public readonly SIMDVec4d ToDouble() => (SIMDVec4d)this;
        public readonly SIMDVec4f ToFloat() => (SIMDVec4f)this;

        // These methods reinterpret an existing referenced FastVec4i, Vector128, or 4-component tuple as one of the other kinds, without incurring a copy.
        // These should only be used when you have an existing hard reference to one of these types! Otherwise the implicit conversions above are better.
        public static ref readonly SIMDVec4i AsVector128(ref FastVec4i self) => ref Unsafe.As<FastVec4i, SIMDVec4i>(ref self);
        public static ref readonly (int x, int y, int z, int w) AsTuple(ref FastVec4i self) => ref Unsafe.As<FastVec4i, (int x, int y, int z, int w)>(ref self);
        public static ref readonly FastVec4i FromVector128(ref SIMDVec4i vec) => ref Unsafe.As<SIMDVec4i, FastVec4i>(ref vec);
        // Conversion from tuple requires a copy because tuples don't have the alignment guarantees
        public static FastVec4i FromTuple(ref (int x, int y, int z, int w) tuple) => new(V128.LoadUnsafe(ref tuple.x));
        #endregion

        /// <summary>
        /// Allows the use of deconstruction syntax, i.e.:
        /// <code>
        /// FastVec4i vec4i = default;
        /// (int x, int y, int z, int w) = vec4i;
        /// </code>
        /// </summary>
        public void Deconstruct(out int x, out int y, out int z, out int w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public override bool Equals(object? obj) => obj is FastVec4i i && Equals(i);
        bool IEquatable<FastVec4i>.Equals(FastVec4i other) => Equals(other);
        public bool Equals(in FastVec4i other) => SIMDVector == other.SIMDVector;
        public override int GetHashCode() => SIMDVector.GetHashCode();

        public int Dot(in FastVec4i other) => SIMDVector.Dot(other.SIMDVector);
        public SIMDVec4i Abs() =>  SIMDVector.Abs();
        public int LengthSquared() => SIMDVector.Dot(SIMDVector);
        public double Length() => Math.Sqrt(LengthSquared());
        public double Distance(in FastVec4i other) => (other - this).Length();
        public double DistanceSq(in FastVec4i other) => (other - this).LengthSquared();

        public static SIMDVec4i Min(in FastVec4i left, in FastVec4i right) => SIMDVec.Min(left.SIMDVector, right.SIMDVector);
        public static SIMDVec4i Max(in FastVec4i left, in FastVec4i right) => SIMDVec.Min(left.SIMDVector, right.SIMDVector);
        public SIMDVec4i Min(in FastVec4i other) => Min(this, other);
        public SIMDVec4i Max(in FastVec4i other) => Max(other.SIMDVector);
        public SIMDVec4i Clamp(in FastVec4i min, in FastVec4i max) => SIMDVector.Clamp(min, max);
        public SIMDVec4i Clamp(int min, int max) => SIMDVector.Clamp(Fill(min), Fill(max));


        #region Operators
        public static bool operator ==(in FastVec4i left, in FastVec4i right) => left.SIMDVector == right.SIMDVector;
        public static bool operator !=(in FastVec4i left, in FastVec4i right) => !(left == right);
        public static bool operator ==(in FastVec4i left, in (int x, int y, int z, int w) right) => left.SIMDVector == right.ToSIMD();
        public static bool operator !=(in FastVec4i left, in (int x, int y, int z, int w) right) => !(left == right);
        public static bool operator ==(in FastVec4i left, in (int x, int y, int z) right) => left.SIMDVector == right.ToSIMD(0);
        public static bool operator !=(in FastVec4i left, in (int x, int y, int z) right) => !(left == right);
        public static SIMDVec4i operator +(in FastVec4i left, in FastVec4i right) => left.SIMDVector + right.SIMDVector;
        public static SIMDVec4i operator -(in FastVec4i left, in FastVec4i right) => left.SIMDVector - right.SIMDVector;
        public static SIMDVec4i operator *(in FastVec4i left, in FastVec4i right) => left.SIMDVector * right.SIMDVector;
        public static SIMDVec4i operator /(in FastVec4i left, in FastVec4i right) => left.SIMDVector / right.SIMDVector;
        public static SIMDVec4i operator %(in FastVec4i left, in FastVec4i right) => left - left / right;
        public static SIMDVec4i operator +(in FastVec4i left, in (int x, int y, int z, int w) right) => left.SIMDVector + right.ToSIMD();
        public static SIMDVec4i operator -(in FastVec4i left, in (int x, int y, int z, int w) right) => left.SIMDVector - right.ToSIMD();
        public static SIMDVec4i operator *(in FastVec4i left, in (int x, int y, int z, int w) right) => left.SIMDVector * right.ToSIMD();
        public static SIMDVec4i operator /(in FastVec4i left, in (int x, int y, int z, int w) right) => left.SIMDVector / right.ToSIMD();
        public static SIMDVec4i operator %(in FastVec4i left, in (int x, int y, int z, int w) right) => left - left / right;
        public static SIMDVec4i operator +(in FastVec4i left, in (int x, int y, int z) right) => left.SIMDVector + right.ToSIMD(0);
        public static SIMDVec4i operator -(in FastVec4i left, in (int x, int y, int z) right) => left.SIMDVector - right.ToSIMD(0);
        public static SIMDVec4i operator *(in FastVec4i left, in (int x, int y, int z) right) => left.SIMDVector * right.ToSIMD(1);
        public static SIMDVec4i operator /(in FastVec4i left, in (int x, int y, int z) right) => left.SIMDVector / right.ToSIMD(1);
        public static SIMDVec4i operator %(in FastVec4i left, in (int x, int y, int z) right) => left - left / right;
        public static SIMDVec4i operator *(in FastVec4i left, int right) => left.SIMDVector * right;
        public static SIMDVec4i operator /(in FastVec4i left, int right) => left.SIMDVector / V128.Create(right);
        public static SIMDVec4i operator %(in FastVec4i left, int right) => left - left / right;
        // unary - and +
        public static SIMDVec4i operator -(in FastVec4i value) => -value.SIMDVector;
        public static SIMDVec4i operator +(in FastVec4i value) => value.SIMDVector;
        #endregion

        public override string ToString() => $"x={X}, y={Y}, z={Z}, w={W}";

        #region IVec3 implemementation
        int IVec3.XAsInt => X;
        int IVec3.YAsInt => Y;
        int IVec3.ZAsInt => Z;
        double IVec3.XAsDouble => X;
        double IVec3.YAsDouble => Y;
        double IVec3.ZAsDouble => Z;
        float IVec3.XAsFloat => X;
        float IVec3.YAsFloat => Y;
        float IVec3.ZAsFloat => Z;
        Vec3i IVec3.AsVec3i => new(X, Y, Z);
        #endregion
    }
}
