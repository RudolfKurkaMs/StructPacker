using System;
using System.Reflection.Emit;

namespace Tests
{
    public static class TypeSize<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int Size { get; }

        static TypeSize()
        {
            var dm = new DynamicMethod("_sizeOfType", typeof(int), new Type[0]);
            ILGenerator il = dm.GetILGenerator();

            il.Emit(OpCodes.Sizeof, typeof(T));
            il.Emit(OpCodes.Ret);

            Size = (int) dm.Invoke(null, null);
        }
    }
}