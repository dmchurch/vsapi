using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client
{
    public partial class ParticlePhysics
    {
        FastCuboidd[] fastBoxList = new FastCuboidd[16];
        int fastBoxCount = 0;

        public EnumCollideFlags UpdateMotionFastVec(FastVec4d pos, ref FastVec4f motion, float size)
        {
            double halfSize = size / 2;
            FastVec4d collMin = pos - (halfSize, 0d, halfSize);
            FastVec4d collMax = pos + (halfSize, halfSize, halfSize);
            FastCuboidd particleCollBox = new(collMin, collMax);

            motion = motion.Clamp(-MotionCap, MotionCap);

            FastVec4i minLoc = (particleCollBox.Min + motion.ToDouble()).ToInt();
            minLoc -= (0, 1, 0);  // -1 for the extra high collision box of fences
            FastVec4i maxLoc = (particleCollBox.Max + motion.ToDouble()).ToInt();

            minPos.Set(minLoc.X, minLoc.Y, minLoc.Z);
            maxPos.Set(maxLoc.X, maxLoc.Y, maxLoc.Z);

            EnumCollideFlags flags = 0;

            // It's easier to compute collisions if we're traveling forward on each axis
            FastVec4d negativeComponents = motion.ToDouble().LessThan(FastVec4d.Zero);
            FastVec4d flipSigns = negativeComponents.ConditionalSelect(FastVec4d.Fill(-1), FastVec4d.Fill(1));

            FastVec4d flippedMotion = motion.ToDouble() * flipSigns;
            particleCollBox.From *= flipSigns;
            particleCollBox.To *= flipSigns;

            particleCollBox.Normalize();

            FastVec4d motionMask = flippedMotion.GreaterThan(FastVec4d.Zero);

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
