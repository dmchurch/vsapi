using System;
using Vintagestory.API.MathTools;

using V128 = System.Runtime.Intrinsics.Vector128;
using V256 = System.Runtime.Intrinsics.Vector256;

namespace Vintagestory.API.Client
{
    public partial class ParticlePhysics
    {
        public EnumCollideFlags UpdateMotionSIMDVec(SIMDVec4d pos, ref SIMDVec4f motion, float size)
        {
            double halfSize = size / 2;
            SIMDVec4d collMin = pos - V256.Create(halfSize, 0d, halfSize, 0d);
            SIMDVec4d collMax = pos + V256.Create(halfSize, halfSize, halfSize, 0d);
            FastCuboidd particleCollBox = new(collMin, collMax);

            motion = motion.Clamp(-MotionCap, MotionCap);

            SIMDVec4i minLoc = (particleCollBox.Min + motion.ToDouble()).ToInt();
            minLoc -= V128.Create(0, 1, 0, 0);  // -1 for the extra high collision box of fences
            SIMDVec4i maxLoc = (particleCollBox.Max + motion.ToDouble()).ToInt();

            minPos.Set(minLoc.X(), minLoc.Y(), minLoc.Z());
            maxPos.Set(maxLoc.X(), maxLoc.Y(), maxLoc.Z());

            EnumCollideFlags flags = 0;

            // It's easier to compute collisions if we're traveling forward on each axis
            SIMDVec4d negativeComponents = motion.ToDouble().LessThan(SIMDVec4d.Zero);
            SIMDVec4d flipSigns = V256.ConditionalSelect(negativeComponents, V256.Create(-1d), V256.Create(1d));
            //SIMDVec4d flipSigns = negativeComponents.ConditionalSelect(SIMDVec.Double(-1), SIMDVec.Double(1));

            SIMDVec4d flippedMotion = motion.ToDouble() * flipSigns;
            particleCollBox.From *= flipSigns;
            particleCollBox.To *= flipSigns;

            particleCollBox.Normalize();

            SIMDVec4d motionMask = flippedMotion.GreaterThan(SIMDVec4d.Zero);

            fastBoxCount = 0;
            BlockAccess.WalkBlocks(minPos, maxPos, (cblock, x, y, z) => {
                Cuboidf[] collisionBoxes = cblock.GetParticleCollisionBoxes(BlockAccess, tmpPos.Set(x, y, z));
                if (collisionBoxes != null) {
                    foreach (Cuboidf collisionBox in collisionBoxes) {
                        FastCuboidd box = collisionBox;
                        box.From *= flipSigns;
                        box.To *= flipSigns;
                        box.Normalize();
                        while (fastBoxCount >= fastBoxList.Length) {
                            Array.Resize(ref fastBoxList, fastBoxList.Length * 2);
                        }
                        fastBoxList[fastBoxCount++] = box;
                    }
                }
            }, false);

            for (int i = 0; i < fastBoxCount; i++) {
                ref FastCuboidd box = ref fastBoxList[i];

                flags |= box.PushOutNormalized(particleCollBox, ref flippedMotion);
            }

            // Restore motion to non-flipped
            motion = (flippedMotion * flipSigns).ToFloat();

            return flags;
        }
    }
}
