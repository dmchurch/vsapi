using System;
using System.Runtime.Intrinsics;
using Vintagestory.API.Client;

namespace Vintagestory.API.MathTools
{
    using V256 = Vector256;
    public struct FastCuboidd
    {
        public FastVec4d From;
        public FastVec4d To;

        public double X1 { readonly get => From.X; set => From = From with { X = value }; }
        public double Y1 { readonly get => From.Y; set => From = From with { Y = value }; }
        public double Z1 { readonly get => From.Z; set => From = From with { Z = value }; }
        public double X2 { readonly get => To.X; set => To = To with { X = value }; }
        public double Y2 { readonly get => To.Y; set => To = To with { Y = value }; }
        public double Z2 { readonly get => To.Z; set => To = To with { Z = value }; }

        public readonly FastVec4d Min => FastVec4d.Min(From, To);
        public readonly FastVec4d Max => FastVec4d.Max(From, To);
        public readonly FastVec4d Mid => (From + To) * 0.5;

        public readonly FastVec4d Size => To - From;
        public readonly FastVec4d Extent => Max - Min;

        /// <summary>
        /// This is a mask vector (a FastVec in which the bit patterns for any given component are either all 1
        /// or all 0) that defines which components are currently in the standard orientation of To >= From.
        /// </summary>
        public readonly FastVec4d OrientationMask => To.GreaterThanOrEqual(From);
        public readonly bool IsNormalized => OrientationMask == FastVec4d.AllBitsSet;

        /// <summary>
        /// A degenerate cuboid component is one which From == To, which has a Size and Extent of 0. Most
        /// cuboids will be degenerate in the W component, as it is not used in XYZ positioning.
        /// </summary>
        public readonly FastVec4d DegenerateComponentMask => To.EqualTo(From);
        public readonly bool Is3D => DegenerateComponentMask == FastVec4d.MaskW;
        public readonly bool Is4D => DegenerateComponentMask == FastVec4d.Zero;

        public readonly double XSize => Size.X;
        public readonly double YSize => Size.Y;
        public readonly double ZSize => Size.Z;

        public readonly double Width => Extent.X;
        public readonly double Height => Extent.Y;
        public readonly double Length => Extent.Z;

        public readonly double MinX => Min.X;
        public readonly double MinY => Min.Y;
        public readonly double MinZ => Min.Z;
        public readonly double MaxX => Max.X;
        public readonly double MaxY => Max.Y;
        public readonly double MaxZ => Max.Z;

        public readonly double MidX => Mid.X;
        public readonly double MidY => Mid.Y;
        public readonly double MidZ => Mid.Z;

        public double this[int index]
        {
            readonly get
            {
                switch (index) {
                    case 0: return X1;
                    case 1: return Y1;
                    case 2: return Z1;
                    case 3: return X2;
                    case 4: return Y2;
                    case 5: return Z2;
                }

                throw new ArgumentException("Out of bounds");
            }

            set
            {
                switch (index) {
                    case 0: X1 = value; return;
                    case 1: Y1 = value; return;
                    case 2: Z1 = value; return;
                    case 3: X2 = value; return;
                    case 4: Y2 = value; return;
                    case 5: Z2 = value; return;
                }

                throw new ArgumentException("Out of bounds");
            }
        }

        public FastCuboidd(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            // since we aren't using the W coordinate, we set it to be quite expansive so that
            // it will always count as "intersecting"
            From = new(x1, y1, z1, 0);
            To = new(x2, y2, z2, 0);
        }

        public FastCuboidd(scoped in FastVec4d start, scoped in FastVec4d end)
        {
            From = start;
            To = end;
        }

        public static implicit operator FastCuboidd(Cuboidd c) => new(c.X1, c.Y1, c.Z1, c.X2, c.Y2, c.Z2);
        public static implicit operator FastCuboidd(Cuboidf c) => new(c.X1, c.Y1, c.Z1, c.X2, c.Y2, c.Z2);
        public static implicit operator FastCuboidd(Cuboidi c) => new(c.X1, c.Y1, c.Z1, c.X2, c.Y2, c.Z2);

        public void Set(FastVec4d start, FastVec4d end)
        {
            From = start;
            To = end;
        }

        public void Set(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            From = V256.Create(x1, y1, z1, int.MinValue);
            To = V256.Create(x2, y2, z2, int.MaxValue);
        }

        public void SwapEnds() => SwapEnds(FastVec4d.AllBitsSet);
        public void SwapEnds(in FastVec4d componentMask) => ConditionalSet(componentMask, To, From);

        public void ConditionalSet(in FastVec4d componentMask, in FastVec4d fromIfOne, in FastVec4d toIfOne)
        {
            From = componentMask.ConditionalSelect(fromIfOne, toIfOne);
            To = componentMask.ConditionalSelect(toIfOne, fromIfOne);
        }

        public void Orient(in FastVec4d forwardVector)
        {
            FastVec4d forwardMask = V256.GreaterThanOrEqual(forwardVector, FastVec4d.Zero);
            OrientByMask(forwardMask);
        }

        public void OrientByMask(in SIMDVec4d forwardMask)
        {
            FastVec4d swapMask = forwardMask ^ OrientationMask;
            if (swapMask != FastVec4d.Zero) {
                SwapEnds(swapMask);
            }
        }

        public void OrientByMask(in FastVec4d forwardMask)
        {
            FastVec4d swapMask = forwardMask.SIMDVector ^ OrientationMask;
            if (swapMask != FastVec4d.Zero) {
                SwapEnds(swapMask);
            }
        }

        public void Normalize() => OrientByMask(FastVec4d.AllBitsSet);

        public void Set(in IVec3 min, in IVec3 max)
        {
            Set(min.XAsFloat, min.YAsFloat, min.ZAsFloat, max.XAsFloat, max.YAsFloat, max.ZAsFloat);
        }

        public readonly FastCuboidd TranslateCopy(FastVec4d offset)
        {
            FastCuboidd translated = this;
            translated.Translate(offset);
            return translated;
        }

        public readonly bool ContainsOrTouches(in FastVec4d point)
        {
            return point.GreaterThanOrEqualAll(From) && point.LessThanOrEqualAll(To);
        }

        public readonly bool Intersects(FastCuboidd other, bool allowTouching = true)
        {
            other.Normalize();
            return allowTouching ? this.ToNormalized().IntersectsOrTouchesNormalized(other)
                                 : this.ToNormalized().IntersectsNormalized(other);
        }

        // A normalized (To >= From) cuboid intersects another when each one's To is greater than the other's
        // From. If touches are allowed, then this is a greater-than-or-equal.
        public readonly bool IntersectsOrTouchesNormalized(in FastCuboidd other)
        {
            return To.GreaterThanOrEqualAll(other.From) && other.To.GreaterThanOrEqualAll(From);
        }

        // If touches are not allowed, then this is a strict greater-than, but a degenerate component can
        // never satisfy this, so we ignore the non-degenerate components.
        public readonly bool IntersectsNormalized(in FastCuboidd other)
        {
            return (To.GreaterThan(other.From) & other.To.GreaterThan(From)) == FastVec4d.AllBitsSet;
        }

        // Two cuboids that share alignment (which of To or From is greater, for any given component) intersect,
        // regardless of normalization state, if each one's To component lies on the same side of the other's
        // corresponding From component. In other words, if cuboids A and B intersect, then the following holds:
        //     (A.To.X > B.From.X && B.To.X > A.From.X) || (A.To.X < B.From.X && B.To.X < A.From.X)
        // for every non-degenerate component. Thus, the sign of (A.To - B.From) must be the same as the sign
        // of (B.To - A.From), for each non-degenerate component. Degenerate components will also satisfy this when
        // the operations are changed to ≥ and ≤
        public readonly bool IntersectsOrTouchesAligned(in FastCuboidd other)
        {
            return To.GreaterThanOrEqual(other.From).EqualTo(other.To.GreaterThanOrEqual(From)) == FastVec4d.AllBitsSet;
        }
        public readonly bool IntersectsAligned(in FastCuboidd other, out FastVec4d touchingComponents)
        {
            FastVec4d aToBFrom = To - other.From;
            FastVec4d bToAFrom = other.To - From;
            touchingComponents = aToBFrom.EqualTo(FastVec4d.Zero) | bToAFrom.EqualTo(FastVec4d.Zero);
            FastVec4d identicalSigns = aToBFrom.GreaterThanOrEqual(FastVec4d.Zero).EqualTo(bToAFrom.GreaterThanOrEqual(FastVec4d.Zero));
            return identicalSigns.AndNot(touchingComponents) == FastVec4d.AllBitsSet;
        }

        public void GrowToInclude(in FastVec4i point)
        {
            From = FastVec4d.Min(From, point.ToDouble());
            To = FastVec4d.Max(To, (point + Vector128.Create(1)).ToDouble());
        }

        // Raw intrinsic implementation
        public readonly EnumCollideFlags PushOutNormalized(in FastCuboidd originBox, ref SIMDVec4d motion)
        {
            {
                // Which components start out on or before the near side of this collision box?
                SIMDVec4d toSideBefore = V256.LessThanOrEqual(originBox.To.SIMDVector, From.SIMDVector);
                // Which components end up past the near side of this box?
                SIMDVec4d toSideAfter = V256.GreaterThan(originBox.To.SIMDVector + motion, From.SIMDVector);
                SIMDVec4d fromSideBefore = V256.LessThan(originBox.From.SIMDVector, To.SIMDVector);
                SIMDVec4d intersectingComponents = fromSideBefore & toSideAfter;

                EnumCollideFlags collision = 0;
                if (intersectingComponents == SIMDVec4d.AllBitsSet) {
                    // We need to reduce any component which has gone from "near side of collision" to
                    // "collision" back to the point where it's only just touching the other box.

                    // How much did we overshoot by, in each component?
                    SIMDVec4d overshoot = originBox.To + motion - From;
                    // Which components overshot?
                    SIMDVec4d overshootMask = toSideBefore & toSideAfter;
                    // Subtract that amount from all components that crossed the line
                    motion -= V256.ConditionalSelect(overshootMask, overshoot, SIMDVec4d.Zero);
                    // Set collision flags appropriately. Since EnumCollideFlags is X=1, Y=2, Z=4,
                    // this fits perfectly with fetching the MSB of each element in the vector.
                    collision |= (EnumCollideFlags)overshoot.ExtractMostSignificantBits();
                }

                return collision;
            }
        }

        // FastVec implementation
        public readonly EnumCollideFlags PushOutNormalized(in FastCuboidd originBox, ref FastVec4d motion)
        {
            // Which components start out on or before the near side of this collision box?
            FastVec4d toSideBefore = originBox.To.LessThanOrEqual(From);
            // Which components end up past the near side of this box?
            FastVec4d toSideAfter = (originBox.To + motion).GreaterThan(From);
            FastVec4d fromSideBefore = originBox.From.LessThan(To);
            FastVec4d intersectingComponents = fromSideBefore.SIMDVector & toSideAfter.SIMDVector;

            EnumCollideFlags collision = 0;
            if (intersectingComponents == FastVec4d.AllBitsSet) {
                // We need to reduce any component which has gone from "near side of collision" to
                // "collision" back to the point where it's only just touching the other box.

                // How much did we overshoot by, in each component?
                FastVec4d overshoot = originBox.To + motion - From;
                // Which components overshot?
                FastVec4d overshootMask = toSideBefore.SIMDVector & toSideAfter.SIMDVector;
                // Subtract that amount from all components that crossed the line
                motion -= V256.ConditionalSelect(overshootMask, overshoot, FastVec4d.Zero);
                // Set collision flags appropriately. Since EnumCollideFlags is X=1, Y=2, Z=4,
                // this fits perfectly with fetching the MSB of each element in the vector.
                collision |= (EnumCollideFlags)overshoot.SIMDVector.ExtractMostSignificantBits();
            }

            return collision;
        }
    }

    public static partial class CuboidExtensions
    {
        public static FastCuboidd ToNormalized(this FastCuboidd self)
        {
            self.Normalize();
            return self;
        }
        public static FastCuboidd ToOriented(this FastCuboidd self, in FastVec4d forwardVector)
        {
            self.Orient(forwardVector);
            return self;
        }
        public static FastCuboidd ToOrientedByMask(this FastCuboidd self, in FastVec4d forwardMask)
        {
            self.OrientByMask(forwardMask);
            return self;
        }
        public static ref FastCuboidd Translate(ref this FastCuboidd self, in FastVec4d offset)
        {
            self.From += offset;
            self.To += offset;
            return ref self;
        }
        public static ref FastCuboidd GrowBy(ref this FastCuboidd self, in FastVec4d delta)
        {
            self.From -= delta;
            self.To += delta;
            return ref self;
        }
    }
}
