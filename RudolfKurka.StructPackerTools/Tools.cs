using System;
using System.IO;
using System.Text;

namespace StructPacker
{
    // ReSharper disable once UnusedType.Global
    public static unsafe class Tools
    {
        private const int GpBufferSize = 16;

        #region Unsafe methods

        private static double ReadDouble(byte[] value, ref int index)
        {
            long val = ReadInt64(value, ref index);
            return *(double*)&val;
        }

        private static int ReadInt32(byte[] value, ref int index)
        {
            fixed (byte* pbyte = &value[index])
            {
                index += sizeof(int);
                return *((int*)pbyte);
            }
        }

        private static void WriteInt32(int value, byte[] bytes, ref int index)
        {
            fixed (byte* b = &bytes[index])
                *((int*)b) = value;

            index += sizeof(int);
        }

        private static long ReadInt64(byte[] value, ref int index)
        {
            fixed (byte* pbyte = &value[index])
            {
                index += sizeof(long);
                return *((long*)pbyte);
            }
        }

        private static void WriteInt64(long value, byte[] bytes, ref int index)
        {
            fixed (byte* b = &bytes[index])
                *((long*)b) = value;

            index += sizeof(long);
        }

        private static char ReadChar(byte[] value, ref int index) => (char)ReadInt16(value, ref index);

        private static short ReadInt16(byte[] value, ref int index)
        {
            fixed (byte* pbyte = &value[index])
            {
                index += sizeof(short);
                return *((short*)pbyte);
            }
        }

        private static void WriteInt16(short value, byte[] bytes, ref int index)
        {
            fixed (byte* b = &bytes[index])
                *((short*)b) = value;

            index += sizeof(short);
        }

        private static ushort ReadUInt16(byte[] value, ref int index) => (ushort)ReadInt16(value, ref index);

        private static uint ReadUInt32(byte[] value, ref int index) => (uint)ReadInt32(value, ref index);

        private static ulong ReadUInt64(byte[] value, ref int index) => (ulong)ReadInt64(value, ref index);

        private static float ReadSingle(byte[] value, ref int index)
        {
            int val = ReadInt32(value, ref index);
            return *(float*)&val;
        }

        private static void WriteDecimal(decimal value, byte[] bytes, ref int index)
        {
            fixed (byte* b = &bytes[index])
                *((decimal*)b) = value;

            index += sizeof(decimal);
        }

        private static decimal ReadDecimal(byte[] value, ref int index)
        {
            fixed (byte* pbyte = &value[index])
            {
                index += sizeof(decimal);
                return *((decimal*)pbyte);
            }
        }

        #endregion

        #region Main

        private const byte FlagNullArrayValue = 254, FlagFullLengthArray = 255, FlagTrue = 1, FlagFalse = 0;

        private static readonly Encoding StringEnc = Encoding.UTF8;

        public static void UnpackMsg<T>(ref T msg, Stream sourceStream, ReadPropsFromStreamDeleg<T> readProps)
            where T : struct
        {
            using PooledBuffer slice = PooledBuffer.Get(GpBufferSize);
            readProps(ref msg, sourceStream, slice.Data);
        }

        public static void UnpackMsg<T>(ref T msg, byte[] sourceData, ref int index, ReadPropsFromBytesDeleg<T> readProps)
            where T : struct
        {
            readProps(ref msg, sourceData, ref index);
        }

        public static PooledBuffer PackMsgToBuffer<T>(ref T msg, int msgSize, WritePropsDeleg<T> writeProps)
            where T : struct
        {
            PooledBuffer data = PooledBuffer.Get(msgSize);
            int index = 0;
            writeProps(ref msg, data.Data, ref index);
            return data;
        }

        public static void PackMsgToStream<T>(ref T msg, Stream destinationStream, int msgSize, WritePropsDeleg<T> writeProps)
            where T : struct
        {
            using PooledBuffer slice = PooledBuffer.Get(msgSize);
            int index = 0;
            writeProps(ref msg, slice.Data, ref index);
            slice.WriteInto(destinationStream);
        }

        public static byte[] PackMsgToArray<T>(ref T msg, int msgSize, WritePropsDeleg<T> writeProps)
            where T : struct
        {
            using PooledBuffer slice = PooledBuffer.Get(msgSize);
            int index = 0;
            writeProps(ref msg, slice.Data, ref index);
            return slice.ToArray();
        }

        public delegate void ReadPropsFromStreamDeleg<T>(ref T msg, Stream srcStream, byte[] gpBuffer) where T : struct;

        public delegate void ReadPropsFromBytesDeleg<T>(ref T msg, byte[] srcBytes, ref int startIndex) where T : struct;

        public delegate void WritePropsDeleg<T>(ref T msg, byte[] destBytes, ref int index) where T : struct;

        private static byte ReadByteFromStream(Stream str, byte[] gpBuffer)
        {
            int read = str.Read(gpBuffer, 0, 1);
            return read <= 0 ? throw new EndOfStreamException() : gpBuffer[0];
        }

        private static void ReadBytesFromStream(Stream source, byte[] target, int count)
        {
            int totalRead = 0;

            do
            {
                int read = source.Read(target, totalRead, count - totalRead);

                if (read == 0)
                    throw new EndOfStreamException();

                totalRead += read;
            } while (totalRead != count);
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static int GetArraySize1D<T>(T[] array, int itemSize)
        {
            if (array == null)
                return 1;

            if (array.Length < 254)
                return 1 + array.Length * itemSize;

            return 5 + array.Length * itemSize;
        }

        private static int? ReadArrayLength(Stream sourceStream, byte[] gpBuffer)
        {
            byte infoByte = ReadByteFromStream(sourceStream, gpBuffer);

            switch (infoByte)
            {
                case FlagNullArrayValue:
                    return null;
                case FlagFullLengthArray:
                    ReadBytesFromStream(sourceStream, gpBuffer, sizeof(int));

                    int index = 0;
                    return ReadInt32(gpBuffer, ref index);
                default:
                    return infoByte;
            }
        }

        private static int? ReadArrayLength(byte[] srcBytes, ref int startIndex)
        {
            byte infoByte = srcBytes[startIndex++];

            return infoByte switch
            {
                FlagNullArrayValue => null,
                FlagFullLengthArray => ReadInt32(srcBytes, ref startIndex),
                _ => infoByte
            };
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static int? WriteArrayLength<T>(T[] arr, byte[] targetBytes, ref int index)
        {
            if (arr == null)
            {
                targetBytes[index++] = FlagNullArrayValue;
                return null;
            }

            if (arr.Length < 254)
            {
                targetBytes[index++] = (byte)arr.Length;
            }
            else
            {
                targetBytes[index++] = FlagFullLengthArray;
                WriteInt32(arr.Length, targetBytes, ref index);
            }

            return arr.Length;
        }

        private static T[] ReadArray1DFast<T>(Stream sourceStream, byte[] buffer, int itemSize)
            where T : struct
        {
            int? length = ReadArrayLength(sourceStream, buffer);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<T>.Value;
            }

            int byteCount = length.Value * itemSize;

            using PooledBuffer slice = PooledBuffer.Get(byteCount);

            ReadBytesFromStream(sourceStream, slice.Data, slice.Size);

            var result = new T[length.Value];

            if (result.Length > 0)
                Buffer.BlockCopy(slice.Data, 0, result, 0, byteCount);

            return result;
        }

        private static T[] ReadArray1DFast<T>(byte[] srcBytes, int itemSize, ref int startIndex)
            where T : struct
        {
            int? length = ReadArrayLength(srcBytes, ref startIndex);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<T>.Value;
            }

            int byteCount = length.Value * itemSize;

            var result = new T[length.Value];

            if (result.Length > 0)
            {
                Buffer.BlockCopy(srcBytes, startIndex, result, 0, byteCount);
                startIndex += byteCount;
            }

            return result;
        }

        private static void WriteArray1DFast<T>(T[] arr, int itemSize, byte[] targetBytes, ref int index)
            where T : struct
        {
            if (WriteArrayLength(arr, targetBytes, ref index) > 0)
            {
                int byteCount = arr.Length * itemSize;

                Buffer.BlockCopy(arr, 0, targetBytes, index, byteCount);
                index += byteCount;
            }
        }

        private static bool ConvertToBoolean(byte value)
        {
            return value switch
            {
                FlagTrue => true,
                FlagFalse => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static DateTime ReadDateTime(byte[] inputBytes, ref int index) => DateTime.FromBinary(ReadInt64(inputBytes, ref index));

        private static TimeSpan ReadTimeSpan(byte[] inputBytes, ref int index) => TimeSpan.FromTicks(ReadInt64(inputBytes, ref index));

        #endregion

        #region GetSize

        public static int GetSize(bool _) => sizeof(bool);
        public static int GetSize(byte _) => sizeof(byte);
        public static int GetSize(sbyte _) => sizeof(sbyte);
        public static int GetSize(char _) => sizeof(char);
        public static int GetSize(short _) => sizeof(short);
        public static int GetSize(ushort _) => sizeof(ushort);
        public static int GetSize(int _) => sizeof(int);
        public static int GetSize(uint _) => sizeof(uint);
        public static int GetSize(long _) => sizeof(long);
        public static int GetSize(ulong _) => sizeof(ulong);
        public static int GetSize(DateTime _) => sizeof(long);
        public static int GetSize(TimeSpan _) => sizeof(long);
        public static int GetSize(float _) => sizeof(float);
        public static int GetSize(double _) => sizeof(double);
        public static int GetSize(decimal _) => sizeof(decimal);
        public static int GetSize(string value) => sizeof(int) + (value == null ? 0 : StringEnc.GetByteCount(value));
        public static int GetSize(bool[] value) => GetArraySize1D(value, sizeof(bool));
        public static int GetSize(byte[] value) => GetArraySize1D(value, sizeof(byte));
        public static int GetSize(sbyte[] value) => GetArraySize1D(value, sizeof(sbyte));
        public static int GetSize(char[] value) => GetArraySize1D(value, sizeof(char));
        public static int GetSize(short[] value) => GetArraySize1D(value, sizeof(short));
        public static int GetSize(ushort[] value) => GetArraySize1D(value, sizeof(ushort));
        public static int GetSize(int[] value) => GetArraySize1D(value, sizeof(int));
        public static int GetSize(uint[] value) => GetArraySize1D(value, sizeof(uint));
        public static int GetSize(long[] value) => GetArraySize1D(value, sizeof(long));
        public static int GetSize(ulong[] value) => GetArraySize1D(value, sizeof(ulong));
        public static int GetSize(float[] value) => GetArraySize1D(value, sizeof(float));
        public static int GetSize(double[] value) => GetArraySize1D(value, sizeof(double));
        public static int GetSize(decimal[] value) => GetArraySize1D(value, sizeof(decimal));
        public static int GetSize(DateTime[] value) => GetArraySize1D(value, sizeof(long));
        public static int GetSize(TimeSpan[] value) => GetArraySize1D(value, sizeof(long));

        public static int GetSize(string[] value)
        {
            if (value == null)
                return 1;

            int totalSize = value.Length < 254 ? 1 : 5;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string str in value)
                totalSize += GetSize(str);

            return totalSize;
        }

        #endregion

        #region Read from bytes

        public static bool ReadFromBytes(bool _, byte[] srcBytes, ref int startIndex) => ConvertToBoolean(srcBytes[startIndex++]);
        public static byte ReadFromBytes(byte _, byte[] srcBytes, ref int startIndex) => srcBytes[startIndex++];
        public static sbyte ReadFromBytes(sbyte _, byte[] srcBytes, ref int startIndex) => (sbyte)srcBytes[startIndex++];
        public static char ReadFromBytes(char _, byte[] srcBytes, ref int startIndex) => ReadChar(srcBytes, ref startIndex);
        public static short ReadFromBytes(short _, byte[] srcBytes, ref int startIndex) => ReadInt16(srcBytes, ref startIndex);
        public static ushort ReadFromBytes(ushort _, byte[] srcBytes, ref int startIndex) => ReadUInt16(srcBytes, ref startIndex);
        public static int ReadFromBytes(int _, byte[] srcBytes, ref int startIndex) => ReadInt32(srcBytes, ref startIndex);
        public static uint ReadFromBytes(uint _, byte[] srcBytes, ref int startIndex) => ReadUInt32(srcBytes, ref startIndex);
        public static long ReadFromBytes(long _, byte[] srcBytes, ref int startIndex) => ReadInt64(srcBytes, ref startIndex);
        public static ulong ReadFromBytes(ulong _, byte[] srcBytes, ref int startIndex) => ReadUInt64(srcBytes, ref startIndex);
        public static DateTime ReadFromBytes(DateTime _, byte[] srcBytes, ref int startIndex) => DateTime.FromBinary(ReadInt64(srcBytes, ref startIndex));
        public static TimeSpan ReadFromBytes(TimeSpan _, byte[] srcBytes, ref int startIndex) => TimeSpan.FromTicks(ReadInt64(srcBytes, ref startIndex));
        public static float ReadFromBytes(float _, byte[] srcBytes, ref int startIndex) => ReadSingle(srcBytes, ref startIndex);
        public static double ReadFromBytes(double _, byte[] srcBytes, ref int startIndex) => ReadDouble(srcBytes, ref startIndex);
        public static decimal ReadFromBytes(decimal _, byte[] srcBytes, ref int startIndex) => ReadDecimal(srcBytes, ref startIndex);

        public static string ReadFromBytes(string _, byte[] srcBytes, ref int startIndex)
        {
            int byteCount = ReadInt32(srcBytes, ref startIndex);

            switch (byteCount)
            {
                case -1:
                    return null;
                case 0:
                    return string.Empty;
            }

            string str = StringEnc.GetString(srcBytes, startIndex, byteCount);
            startIndex += byteCount;

            return str;
        }

        public static bool[] ReadFromBytes(bool[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<bool>(srcBytes, sizeof(bool), ref startIndex);
        public static byte[] ReadFromBytes(byte[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<byte>(srcBytes, sizeof(byte), ref startIndex);
        public static sbyte[] ReadFromBytes(sbyte[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<sbyte>(srcBytes, sizeof(sbyte), ref startIndex);
        public static char[] ReadFromBytes(char[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<char>(srcBytes, sizeof(char), ref startIndex);
        public static short[] ReadFromBytes(short[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<short>(srcBytes, sizeof(short), ref startIndex);
        public static ushort[] ReadFromBytes(ushort[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<ushort>(srcBytes, sizeof(ushort), ref startIndex);
        public static int[] ReadFromBytes(int[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<int>(srcBytes, sizeof(int), ref startIndex);
        public static uint[] ReadFromBytes(uint[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<uint>(srcBytes, sizeof(uint), ref startIndex);
        public static long[] ReadFromBytes(long[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<long>(srcBytes, sizeof(long), ref startIndex);
        public static ulong[] ReadFromBytes(ulong[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<ulong>(srcBytes, sizeof(ulong), ref startIndex);
        public static float[] ReadFromBytes(float[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<float>(srcBytes, sizeof(float), ref startIndex);
        public static double[] ReadFromBytes(double[] _, byte[] srcBytes, ref int startIndex) => ReadArray1DFast<double>(srcBytes, sizeof(double), ref startIndex);
        public static decimal[] ReadFromBytes(decimal[] _, byte[] srcBytes, ref int startIndex)
        {
            int? length = ReadArrayLength(srcBytes, ref startIndex);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<decimal>.Value;
            }

            var result = new decimal[length.Value];

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadDecimal(srcBytes, ref startIndex);

            return result;

            // return ReadArray1DFast<decimal>(srcBytes, 16, ref startIndex);
        }

        public static DateTime[] ReadFromBytes(DateTime[] _, byte[] srcBytes, ref int startIndex)
        {
            int? length = ReadArrayLength(srcBytes, ref startIndex);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<DateTime>.Value;
            }

            var result = new DateTime[length.Value];

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadDateTime(srcBytes, ref startIndex);

            return result;
        }

        public static TimeSpan[] ReadFromBytes(TimeSpan[] _, byte[] srcBytes, ref int startIndex)
        {
            int? length = ReadArrayLength(srcBytes, ref startIndex);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<TimeSpan>.Value;
            }

            var result = new TimeSpan[length.Value];

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadTimeSpan(srcBytes, ref startIndex);

            return result;
        }

        public static string[] ReadFromBytes(string[] _, byte[] srcBytes, ref int startIndex)
        {
            int? numStrings = ReadArrayLength(srcBytes, ref startIndex);

            switch (numStrings)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<string>.Value;
            }

            var result = new string[numStrings.Value];

            for (int i = 0; i < numStrings.Value; i++)
                result[i] = ReadFromBytes((string)null, srcBytes, ref startIndex);

            return result;
        }

        #endregion

        #region Read from stream

        public static bool ReadFromStream(bool _, Stream src, byte[] gpBuffer) => ConvertToBoolean(ReadByteFromStream(src, gpBuffer));
        public static byte ReadFromStream(byte _, Stream src, byte[] gpBuffer) => ReadByteFromStream(src, gpBuffer);
        public static sbyte ReadFromStream(sbyte _, Stream src, byte[] gpBuffer) => (sbyte)ReadByteFromStream(src, gpBuffer);

        public static char ReadFromStream(char _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(char));
            int index = 0;
            return ReadChar(gpBuffer, ref index);
        }

        public static short ReadFromStream(short _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(short));
            int index = 0;
            return ReadInt16(gpBuffer, ref index);
        }

        public static ushort ReadFromStream(ushort _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(ushort));
            int index = 0;
            return ReadUInt16(gpBuffer, ref index);
        }

        public static int ReadFromStream(int _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(int));
            int index = 0;
            return ReadInt32(gpBuffer, ref index);
        }

        public static uint ReadFromStream(uint _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(uint));
            int index = 0;
            return ReadUInt32(gpBuffer, ref index);
        }

        public static long ReadFromStream(long _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(long));
            int index = 0;
            return ReadInt64(gpBuffer, ref index);
        }

        public static ulong ReadFromStream(ulong _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(ulong));
            int index = 0;
            return ReadUInt64(gpBuffer, ref index);
        }

        public static float ReadFromStream(float _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(float));
            int index = 0;
            return ReadSingle(gpBuffer, ref index);
        }

        public static double ReadFromStream(double _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(double));
            int index = 0;
            return ReadDouble(gpBuffer, ref index);
        }

        public static decimal ReadFromStream(decimal _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(decimal));
            int index = 0;
            return ReadDecimal(gpBuffer, ref index);
        }

        public static DateTime ReadFromStream(DateTime _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(long));
            int index = 0;
            return DateTime.FromBinary(ReadInt64(gpBuffer, ref index));
        }

        public static TimeSpan ReadFromStream(TimeSpan _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(long));
            int index = 0;
            return TimeSpan.FromTicks(ReadInt64(gpBuffer, ref index));
        }

        public static string ReadFromStream(string _, Stream src, byte[] gpBuffer)
        {
            ReadBytesFromStream(src, gpBuffer, sizeof(int));

            int index = 0;
            int byteCount = ReadInt32(gpBuffer, ref index);

            switch (byteCount)
            {
                case -1:
                    return null;
                case 0:
                    return string.Empty;
            }

            using PooledBuffer slice = PooledBuffer.Get(byteCount);
            ReadBytesFromStream(src, slice.Data, slice.Size);

            return StringEnc.GetString(slice.Data, 0, slice.Size);
        }

        public static bool[] ReadFromStream(bool[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<bool>(src, gpBuffer, sizeof(bool));

        public static byte[] ReadFromStream(byte[] _, Stream src, byte[] gpBuffer)
        {
            int? length = ReadArrayLength(src, gpBuffer);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<byte>.Value;
            }

            var result = new byte[length.Value];
            ReadBytesFromStream(src, result, result.Length);

            return result;
        }

        public static sbyte[] ReadFromStream(sbyte[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<sbyte>(src, gpBuffer, sizeof(sbyte));
        public static char[] ReadFromStream(char[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<char>(src, gpBuffer, sizeof(char));
        public static short[] ReadFromStream(short[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<short>(src, gpBuffer, sizeof(short));
        public static ushort[] ReadFromStream(ushort[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<ushort>(src, gpBuffer, sizeof(ushort));
        public static int[] ReadFromStream(int[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<int>(src, gpBuffer, sizeof(int));
        public static uint[] ReadFromStream(uint[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<uint>(src, gpBuffer, sizeof(uint));
        public static long[] ReadFromStream(long[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<long>(src, gpBuffer, sizeof(long));
        public static ulong[] ReadFromStream(ulong[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<ulong>(src, gpBuffer, sizeof(ulong));
        public static float[] ReadFromStream(float[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<float>(src, gpBuffer, sizeof(float));
        public static double[] ReadFromStream(double[] _, Stream src, byte[] gpBuffer) => ReadArray1DFast<double>(src, gpBuffer, sizeof(double));
        public static decimal[] ReadFromStream(decimal[] _, Stream src, byte[] gpBuffer)
        {
            int? length = ReadArrayLength(src, gpBuffer);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<decimal>.Value;
            }

            using PooledBuffer slice = PooledBuffer.Get(length.Value * sizeof(decimal));
            ReadBytesFromStream(src, slice.Data, slice.Size);

            var result = new decimal[length.Value];
            int byteIndex = 0;

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadDecimal(slice.Data, ref byteIndex);

            return result;

            // return ReadArray1DFast<decimal>(src, gpBuffer, 16);
        }

        public static DateTime[] ReadFromStream(DateTime[] _, Stream src, byte[] gpBuffer)
        {
            int? length = ReadArrayLength(src, gpBuffer);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<DateTime>.Value;
            }

            using PooledBuffer slice = PooledBuffer.Get(length.Value * sizeof(long));
            ReadBytesFromStream(src, slice.Data, slice.Size);

            var result = new DateTime[length.Value];
            int byteIndex = 0;

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadDateTime(slice.Data, ref byteIndex);

            return result;
        }

        public static TimeSpan[] ReadFromStream(TimeSpan[] _, Stream src, byte[] gpBuffer)
        {
            int? length = ReadArrayLength(src, gpBuffer);

            switch (length)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<TimeSpan>.Value;
            }

            using PooledBuffer slice = PooledBuffer.Get(length.Value * sizeof(long));
            ReadBytesFromStream(src, slice.Data, slice.Size);

            var result = new TimeSpan[length.Value];
            int byteIndex = 0;

            for (int i = 0; i < result.Length; i++)
                result[i] = ReadTimeSpan(slice.Data, ref byteIndex);

            return result;
        }

        public static string[] ReadFromStream(string[] _, Stream src, byte[] gpBuffer)
        {
            int? numStrings = ReadArrayLength(src, gpBuffer);

            switch (numStrings)
            {
                case null:
                    return null;
                case 0:
                    return EmptyArrays<string>.Value;
            }

            var result = new string[numStrings.Value];

            for (int i = 0; i < numStrings.Value; i++)
                result[i] = ReadFromStream((string)null, src, gpBuffer);

            return result;
        }

        #endregion

        #region Write

        public static void Write(bool value, byte[] targetBytes, ref int index) => targetBytes[index++] = value ? FlagTrue : FlagFalse;
        public static void Write(byte value, byte[] targetBytes, ref int index) => targetBytes[index++] = value;
        public static void Write(sbyte value, byte[] targetBytes, ref int index) => targetBytes[index++] = (byte)value;
        public static void Write(char value, byte[] targetBytes, ref int index) => WriteInt16((short)value, targetBytes, ref index);
        public static void Write(short value, byte[] targetBytes, ref int index) => WriteInt16(value, targetBytes, ref index);
        public static void Write(ushort value, byte[] targetBytes, ref int index) => WriteInt16((short)value, targetBytes, ref index);
        public static void Write(int value, byte[] targetBytes, ref int index) => WriteInt32(value, targetBytes, ref index);
        public static void Write(uint value, byte[] targetBytes, ref int index) => WriteInt32((int)value, targetBytes, ref index);
        public static void Write(long value, byte[] targetBytes, ref int index) => WriteInt64(value, targetBytes, ref index);
        public static void Write(ulong value, byte[] targetBytes, ref int index) => WriteInt64((long)value, targetBytes, ref index);
        public static void Write(float value, byte[] targetBytes, ref int index) => WriteInt32(*(int*)&value, targetBytes, ref index);
        public static void Write(double value, byte[] targetBytes, ref int index) => WriteInt64(*(long*)&value, targetBytes, ref index);
        public static void Write(decimal value, byte[] targetBytes, ref int index) => WriteDecimal(value, targetBytes, ref index);
        public static void Write(DateTime value, byte[] targetBytes, ref int index) => WriteInt64(value.ToBinary(), targetBytes, ref index);
        public static void Write(TimeSpan value, byte[] targetBytes, ref int index) => WriteInt64(value.Ticks, targetBytes, ref index);

        public static void Write(string value, byte[] targetBytes, ref int index)
        {
            if (value == null)
            {
                WriteInt32(-1, targetBytes, ref index);
                return;
            }

            if (value.Length == 0 || value == string.Empty)
            {
                WriteInt32(0, targetBytes, ref index);
                return;
            }

            int lenPrefixIndex = index;
            index += sizeof(int);

            int writtenBytes = StringEnc.GetBytes(value, 0, value.Length, targetBytes, index);
            index += writtenBytes;

            WriteInt32(writtenBytes, targetBytes, ref lenPrefixIndex);
        }

        public static void Write(bool[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(bool), targetBytes, ref index);
        public static void Write(byte[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(byte), targetBytes, ref index);
        public static void Write(sbyte[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(sbyte), targetBytes, ref index);
        public static void Write(char[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(char), targetBytes, ref index);
        public static void Write(short[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(short), targetBytes, ref index);
        public static void Write(ushort[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(ushort), targetBytes, ref index);
        public static void Write(int[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(int), targetBytes, ref index);
        public static void Write(uint[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(uint), targetBytes, ref index);
        public static void Write(long[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(long), targetBytes, ref index);
        public static void Write(ulong[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(ulong), targetBytes, ref index);
        public static void Write(float[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(float), targetBytes, ref index);
        public static void Write(double[] value, byte[] targetBytes, ref int index) => WriteArray1DFast(value, sizeof(double), targetBytes, ref index);
        public static void Write(decimal[] value, byte[] targetBytes, ref int index)
        {
            if (WriteArrayLength(value, targetBytes, ref index) > 0)
            {
                foreach (decimal item in value)
                    Write(item, targetBytes, ref index);
            }

            // WriteArray1DFast(value, 16, targetBytes, ref index); //Buffer.Block copy does not recognize decimal as primitive type
        }

        public static void Write(DateTime[] value, byte[] targetBytes, ref int index)
        {
            if (WriteArrayLength(value, targetBytes, ref index) > 0)
            {
                foreach (DateTime item in value)
                    Write(item, targetBytes, ref index);
            }
        }

        public static void Write(TimeSpan[] value, byte[] targetBytes, ref int index)
        {
            if (WriteArrayLength(value, targetBytes, ref index) > 0)
            {
                foreach (TimeSpan item in value)
                    Write(item, targetBytes, ref index);
            }
        }

        public static void Write(string[] value, byte[] targetBytes, ref int index)
        {
            if (WriteArrayLength(value, targetBytes, ref index) > 0)
            {
                foreach (string item in value)
                    Write(item, targetBytes, ref index);
            }
        }

        #endregion
    }
}