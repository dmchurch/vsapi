#nullable enable
global using SIMDVec4i = System.Runtime.Intrinsics.Vector128<int>;
global using SIMDVec4f = System.Runtime.Intrinsics.Vector128<float>;
global using SIMDVec4l = System.Runtime.Intrinsics.Vector256<long>;
global using SIMDVec4d = System.Runtime.Intrinsics.Vector256<double>;
global using SIMDVec = Vintagestory.API.MathTools.SIMDVectorExtensions;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

using SIMDVec2l = System.Runtime.Intrinsics.Vector128<long>;
using SIMDVec2d = System.Runtime.Intrinsics.Vector128<double>;

using V128 = System.Runtime.Intrinsics.Vector128;
using V256 = System.Runtime.Intrinsics.Vector256;

namespace Vintagestory.API.MathTools
{
    public static class SIMDVectorExtensions
    {
        #region Inbound conversions (ToSIMD)
        // Tuples to SIMD vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4i ToSIMD(in this (int x, int y, int z, int w) tuple) => V128.Create(tuple.x, tuple.y, tuple.z, tuple.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4i ToSIMD(in this (int x, int y, int z) tuple, int w) => V128.Create(tuple.x, tuple.y, tuple.z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4f ToSIMD(in this (float x, float y, float z, float w) tuple) => V128.Create(tuple.x, tuple.y, tuple.z, tuple.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4f ToSIMD(in this (float x, float y, float z) tuple, float w) => V128.Create(tuple.x, tuple.y, tuple.z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4d ToSIMD(in this (double x, double y, double z, double w) tuple) => V256.Create(tuple.x, tuple.y, tuple.z, tuple.w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SIMDVec4d ToSIMD(in this (double x, double y, double z) tuple, double w) => V256.Create(tuple.x, tuple.y, tuple.z, w);

        // Existing classes/structs to SIMD vectors
        public static SIMDVec4i ToSIMD(this Vec3i vec) => Int(vec.X, vec.Y, vec.Z);
        public static SIMDVec4i ToSIMD(this Vec4i vec) => Int(vec.X, vec.Y, vec.Z, vec.W);
        public static SIMDVec4i ToSIMD(in this FastVec3i vec) => Int(vec.X, vec.Y, vec.Z);
        public static SIMDVec4f ToSIMD(this Vec3f vec) => Float(vec.X, vec.Y, vec.Z);
        public static SIMDVec4f ToSIMD(this Vec4f vec) => Float(vec.X, vec.Y, vec.Z, vec.W);
        public static SIMDVec4f ToSIMD(in this FastVec3f vec) => Float(vec.X, vec.Y, vec.Z);
        public static SIMDVec4d ToSIMD(this Vec3d vec) => Double(vec.X, vec.Y, vec.Z);
        public static SIMDVec4d ToSIMD(this Vec4d vec) => Double(vec.X, vec.Y, vec.Z, vec.W);
        public static SIMDVec4d ToSIMD(in this FastVec3d vec) => Double(vec.X, vec.Y, vec.Z);
        #endregion

        #region int vectors (Vector128<int>, SSE instructions)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4i Int(int xyzw) => V128.Create(xyzw);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4i Int(int x, int y, int z, int w = 0) => V128.Create(x, y, z, w);

        public static SIMDVec4l ToLong(this SIMDVec4i self)
        {
            (SIMDVec2l lower, SIMDVec2l upper) = V128.Widen(self);
            return V256.Create(lower, upper);
        }
        public static SIMDVec4d ToDouble(this SIMDVec4i self) => V256.ConvertToDouble(self.ToLong());
        public static SIMDVec4f ToFloat(this SIMDVec4i self) => V128.ConvertToSingle(self);

        // Length and Distance can't be generic, because you can't convert a generic T to a double for Math.Sqrt
        public static double Length(this SIMDVec4i self) => Math.Sqrt(self.LengthSquared());
        public static double Distance(this SIMDVec4i self, SIMDVec4i other) => (other - self).Length();
        // Clamp needs to use the right constructor for a T vec
        public static SIMDVec4i Clamp(this SIMDVec4i self, int min, int max) => V128.Min(V128.Max(self, V128.Create(min)), V128.Create(max));
        #endregion

        #region float vectors (Vector128<float>, SSE instructions)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4f Float(float xyzw) => V128.Create(xyzw);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4f Float(float x, float y, float z, float w = 0) => V128.Create(x, y, z, w);
        public static SIMDVec4i ToInt(this SIMDVec4f self) => V128.ConvertToInt32(self);
        public static SIMDVec4d ToDouble(this SIMDVec4f self)
        {
            (SIMDVec2d lower, SIMDVec2d upper) = V128.Widen(self);
            return V256.Create(lower, upper);
        }
        public static double Length(this SIMDVec4f self) => Math.Sqrt(self.LengthSquared());
        public static double Distance(this SIMDVec4f self, SIMDVec4f other) => (other - self).Length();
        public static SIMDVec4f Clamp(this SIMDVec4f self, float min, float max) => V128.Min(V128.Max(self, V128.Create(min)), V128.Create(max));
        #endregion

        #region Generic v128 (int, float) behaviors
        public static T X<T>(this Vector128<T> self) where T : struct => self[0];
        public static T Y<T>(this Vector128<T> self) where T : struct => self[1];
        public static T Z<T>(this Vector128<T> self) where T : struct => self[2];
        public static T W<T>(this Vector128<T> self) where T : struct => self[3];
        public static Vector128<T> WithX<T>(this Vector128<T> self, T value) where T : struct => self.WithElement(0, value);
        public static Vector128<T> WithY<T>(this Vector128<T> self, T value) where T : struct => self.WithElement(1, value);
        public static Vector128<T> WithZ<T>(this Vector128<T> self, T value) where T : struct => self.WithElement(2, value);
        public static Vector128<T> WithW<T>(this Vector128<T> self, T value) where T : struct => self.WithElement(3, value);
        public static T Dot<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.Dot(self, other);
        public static Vector128<T> Abs<T>(this Vector128<T> self) where T : struct => V128.Abs(self);
        public static T LengthSquared<T>(this Vector128<T> self) where T : struct => self.Dot(self);
        public static T DistanceSq<T>(this Vector128<T> self, Vector128<T> other) where T : struct => (other - self).LengthSquared();
        public static Vector128<T> Min<T>(Vector128<T> self, Vector128<T> other) where T : struct => V128.Min(self, other);
        public static Vector128<T> Max<T>(Vector128<T> self, Vector128<T> other) where T : struct => V128.Max(self, other);
        public static Vector128<T> Clamp<T>(this Vector128<T> self, Vector128<T> min, Vector128<T> max) where T : struct => V128.Min(V128.Max(self, min), max);

        public static Vector128<T> EqualTo<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.Equals(self, other);
        public static bool EqualToAll<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.EqualsAll(self, other);
        public static bool EqualToAny<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.EqualsAny(self, other);
        public static Vector128<T> GreaterThan<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThan(self, other);
        public static bool GreaterThanAll<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThanAll(self, other);
        public static bool GreaterThanAny<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThanAny(self, other);
        public static Vector128<T> GreaterThanOrEqual<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThanOrEqual(self, other);
        public static bool GreaterThanOrEqualAll<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThanOrEqualAll(self, other);
        public static bool GreaterThanOrEqualAny<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.GreaterThanOrEqualAny(self, other);
        public static Vector128<T> LessThan<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThan(self, other);
        public static bool LessThanAll<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThanAll(self, other);
        public static bool LessThanAny<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThanAny(self, other);
        public static Vector128<T> LessThanOrEqual<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThanOrEqual(self, other);
        public static bool LessThanOrEqualAll<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThanOrEqualAll(self, other);
        public static bool LessThanOrEqualAny<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.LessThanOrEqualAny(self, other);
        public static Vector128<T> AndNot<T>(this Vector128<T> self, Vector128<T> other) where T : struct => V128.AndNot(self, other);
        public static Vector128<T> ConditionalSelect<T>(this Vector128<T> condition, Vector128<T> valueIfTrue, Vector128<T> valueIfFalse) where T : struct => V128.ConditionalSelect(condition, valueIfTrue, valueIfFalse);
        #endregion

        #region long vectors (Vector256<long>, AVX instructions)
        // We don't actually use long vectors, but when converting int ⇔ double it's better to go through long than through float
        public static SIMDVec4l Int(long xyzw) => V256.Create(xyzw);
        public static SIMDVec4l Int(long x, long y, long z, long w = 0) => V256.Create(x, y, z, w);

        public static SIMDVec4i ToInt(this SIMDVec4l self) => V128.Narrow(self.GetLower(), self.GetUpper());
        public static SIMDVec4d ToDouble(this SIMDVec4l self) => V256.ConvertToDouble(self);
        #endregion

        #region double vectors (Vector256<double>, AVX instructions)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4d Double(double xyzw) => V256.Create(xyzw);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static SIMDVec4d Double(double x, double y, double z, double w = 0) => V256.Create(x, y, z, w);
        public static SIMDVec4l ToLong(this SIMDVec4d self) => V256.ConvertToInt64(self);
        public static SIMDVec4i ToInt(this SIMDVec4d self) => self.ToLong().ToInt();
        public static SIMDVec4f ToFloat(this SIMDVec4d self) => V128.Narrow(self.GetLower(), self.GetUpper());

        public static double Length(this SIMDVec4d self) => Math.Sqrt(self.LengthSquared());
        public static double Distance(this SIMDVec4d self, SIMDVec4d other) => (other - self).Length();
        public static SIMDVec4d Clamp(this SIMDVec4d self, double min, double max) => V256.Min(V256.Max(self, V256.Create(min)), V256.Create(max));
        #endregion

        #region Generic v256 (long, double) behaviors
        public static T X<T>(this Vector256<T> self) where T : struct => self[0];
        public static T Y<T>(this Vector256<T> self) where T : struct => self[1];
        public static T Z<T>(this Vector256<T> self) where T : struct => self[2];
        public static T W<T>(this Vector256<T> self) where T : struct => self[3];
        public static Vector256<T> WithX<T>(this Vector256<T> self, T value) where T : struct => self.WithElement(0, value);
        public static Vector256<T> WithY<T>(this Vector256<T> self, T value) where T : struct => self.WithElement(1, value);
        public static Vector256<T> WithZ<T>(this Vector256<T> self, T value) where T : struct => self.WithElement(2, value);
        public static Vector256<T> WithW<T>(this Vector256<T> self, T value) where T : struct => self.WithElement(3, value);
        public static T Dot<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.Dot(self, other);
        public static Vector256<T> Abs<T>(this Vector256<T> self) where T : struct => V256.Abs(self);
        public static T LengthSquared<T>(this Vector256<T> self) where T : struct => self.Dot(self);
        public static T DistanceSq<T>(this Vector256<T> self, Vector256<T> other) where T : struct => (other - self).LengthSquared();
        public static Vector256<T> Min<T>(Vector256<T> self, Vector256<T> other) where T : struct => V256.Min(self, other);
        public static Vector256<T> Max<T>(Vector256<T> self, Vector256<T> other) where T : struct => V256.Max(self, other);
        public static Vector256<T> Clamp<T>(this Vector256<T> self, Vector256<T> min, Vector256<T> max) where T : struct => V256.Min(V256.Max(self, min), max);

        public static Vector256<T> EqualTo<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.Equals(self, other);
        public static bool EqualToAll<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.EqualsAll(self, other);
        public static bool EqualToAny<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.EqualsAny(self, other);
        public static Vector256<T> GreaterThan<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThan(self, other);
        public static bool GreaterThanAll<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThanAll(self, other);
        public static bool GreaterThanAny<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThanAny(self, other);
        public static Vector256<T> GreaterThanOrEqual<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThanOrEqual(self, other);
        public static bool GreaterThanOrEqualAll<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThanOrEqualAll(self, other);
        public static bool GreaterThanOrEqualAny<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.GreaterThanOrEqualAny(self, other);
        public static Vector256<T> LessThan<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThan(self, other);
        public static bool LessThanAll<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThanAll(self, other);
        public static bool LessThanAny<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThanAny(self, other);
        public static Vector256<T> LessThanOrEqual<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThanOrEqual(self, other);
        public static bool LessThanOrEqualAll<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThanOrEqualAll(self, other);
        public static bool LessThanOrEqualAny<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.LessThanOrEqualAny(self, other);
        public static Vector256<T> AndNot<T>(this Vector256<T> self, Vector256<T> other) where T : struct => V256.AndNot(self, other);
        public static Vector256<T> ConditionalSelect<T>(this Vector256<T> condition, Vector256<T> valueIfTrue, Vector256<T> valueIfFalse) where T : struct => V256.ConditionalSelect(condition, valueIfTrue, valueIfFalse);
        #endregion

    }
}
