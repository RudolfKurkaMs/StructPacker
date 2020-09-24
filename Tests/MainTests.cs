using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StructPacker;

namespace Tests
{
    [TestClass]
    public class MainTests
    {
        private static readonly int[] TestArrayLengths = {0, 1, 2, 10, 100, 200, 250, 251, 252, 253, 254, 255, 256, 200000};

        [TestMethod("Value serialization")]
        public void ValueSerializationTests()
        {
            TestValue(true, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(false, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue((byte) 150, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue((sbyte) -95, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue('h', Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue((short) -12547, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue((ushort) 12547, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(-268552487, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(418552487U, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(-268552458564564587L, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(368552458564564587UL, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(6548.98564f, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(65544565648.98564d, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(new DateTime(2000, 8, 15, 14, 16, 59, DateTimeKind.Utc), Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(new TimeSpan(15, 14, 16, 59), Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue("test", Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
            TestValue(123456.789456m, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write, Tools.GetSize, Tools.ReadFromBytes, Tools.ReadFromStream, Tools.Write);
        }

        [TestMethod("Message serialization")]
        public void MessageSerializationTests()
        {
            var msgPack = new BigMessage(true);

            int size = msgPack.GetSize();
            size.TestIsGreater(0);

            using PooledBuffer buffer = Tools.PackMsgToBuffer(ref msgPack, size, BigMessage.Pack);
            byte[] array = Tools.PackMsgToArray(ref msgPack, size, BigMessage.Pack);

            using var memStr = new MemoryStream();
            Tools.PackMsgToStream(ref msgPack, memStr, size, BigMessage.Pack);
            memStr.Position = 0;

            var msgUnpack = new BigMessage();
            Tools.UnpackMsg(ref msgUnpack, memStr, BigMessage.Unpack);

            msgPack.TestEqual(msgUnpack);
            
            msgUnpack = new BigMessage();

            int index = 0;
            Tools.UnpackMsg(ref msgUnpack, array, ref index, BigMessage.Unpack);

            msgPack.TestEqual(msgUnpack);
        }

        private static void TestValue<T>(
            T value,
            GetSizeDeleg<T> getSize,
            ReadFromBytesDeleg<T> readFromBytes,
            ReadFromStreamDeleg<T> readFromStream,
            WriteDeleg<T> write,
            GetSizeDeleg<T[]> getSizeArr,
            ReadFromBytesDeleg<T[]> readFromBytesArr,
            ReadFromStreamDeleg<T[]> readFromStreamArr,
            WriteDeleg<T[]> writeArr)
            where T : IEquatable<T>
        {
            Type type = typeof(T);

            int size = getSize(value);

            size.TestIsGreater(0);

            if (type.IsPrimitive)
                size.TestEqual(TypeSize<T>.Size);

            byte[] buffer = new byte[size], gpBuffer = new byte[16];

            int index = 0;
            write(value, buffer, ref index);

            index.TestEqual(size);

            index = 0;
            T read1 = readFromBytes(value, buffer, ref index);

            read1.TestEqual(value);

            using var memStr = new MemoryStream(buffer);

            T read2 = readFromStream(value, memStr, gpBuffer);

            read2.TestEqual(value);

            TestValueArray(null, getSizeArr, readFromBytesArr, readFromStreamArr, writeArr);

            foreach (int arrLength in TestArrayLengths)
            {
                var arr = new T[arrLength];

                //test with defaults
                TestValueArray(arr, getSizeArr, readFromBytesArr, readFromStreamArr, writeArr);

                if (arr.Length > 0)
                {
                    //test with actual values

                    for (var i = 0; i < arr.Length; i++)
                        arr[i] = value;

                    TestValueArray(arr, getSizeArr, readFromBytesArr, readFromStreamArr, writeArr);
                }
            }
        }

        private static void TestValueArray<T>(
            T[] value,
            GetSizeDeleg<T[]> getSize,
            ReadFromBytesDeleg<T[]> readFromBytes,
            ReadFromStreamDeleg<T[]> readFromStream,
            WriteDeleg<T[]> write)
            where T : IEquatable<T>
        {
            int size = getSize(value);

            size.TestIsGreater(0);

            byte[] buffer = new byte[size], gpBuffer = new byte[8];

            int index = 0;
            write(value, buffer, ref index);

            index.TestEqual(size);

            index = 0;
            T[] read1 = readFromBytes(value, buffer, ref index);

            read1.TestCollectionEqual(value);

            using var memStr = new MemoryStream(buffer);

            T[] read2 = readFromStream(value, memStr, gpBuffer);

            read2.TestCollectionEqual(value);
        }

        private delegate int GetSizeDeleg<in T>(T value);

        private delegate void WriteDeleg<in T>(T value, byte[] destBytes, ref int index);

        private delegate T ReadFromBytesDeleg<T>(T value, byte[] srcBytes, ref int startIndex);

        private delegate T ReadFromStreamDeleg<T>(T value, Stream src, byte[] gpBuffer);
    }
}