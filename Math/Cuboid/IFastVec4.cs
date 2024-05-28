using System;
using System.Numerics;

namespace Vintagestory.API.MathTools
{
    public interface IFastVec4<TFastVec, TSIMDVector, TComponent> : IEquatable<TFastVec>, IVec3
        where TFastVec : unmanaged, IFastVec4<TFastVec, TSIMDVector, TComponent>
        where TSIMDVector : unmanaged
        where TComponent : unmanaged, INumber<TComponent>
    {
    }
}
