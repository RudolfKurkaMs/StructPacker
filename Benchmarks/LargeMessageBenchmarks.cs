using BenchmarkDotNet.Attributes;
using BinaryPack;
using MessagePack;
using Newtonsoft.Json;
using StructPacker;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class LargeMessageBenchmarks
    {
        private readonly LargeValueMsg _largeValueMsg;

        private readonly BinaryFormatter _binFormatter = new BinaryFormatter();

        public LargeMessageBenchmarks()
        {
            var rnd = new Random(123456);

            var bytes = new byte[512 * 1024];

            rnd.NextBytes(bytes);

            var strings = new string[1024];

            for (int i = 0; i < strings.Length; i++)
                strings[i] = new string('s', i);

            var decimals = new decimal[1024];

            for (int i = 0; i < decimals.Length; i++)
                decimals[i] = (decimal)rnd.NextDouble();

            static T[] GenerateSequence<T>() => Enumerable.Range(0, 1500).Select(v => (T)((IConvertible)(v % 128)).ToType(typeof(T), null)).ToArray();

            _largeValueMsg = new LargeValueMsg
            {
                Prop1 = GenerateSequence<short>(),
                Prop2 = GenerateSequence<int>(),
                Prop3 = GenerateSequence<long>(),
                Prop4 = GenerateSequence<ushort>(),
                Prop5 = GenerateSequence<uint>(),
                Prop6 = GenerateSequence<ulong>(),
                Prop8 = GenerateSequence<sbyte>(),
                Prop10 = Enumerable.Repeat(new DateTime(2020, 3, 20, 10, 20, 30, DateTimeKind.Utc), 250).ToArray(),
                Prop11 = Enumerable.Repeat(new TimeSpan(20, 55, 12), 250).ToArray(),
                Prop12 = new bool[512],
                Prop13 = strings.SelectMany(s => s).ToArray(),
                Prop7 = bytes,
                Prop9 = strings,
                Prop14 = decimals
            };
        }

        [Benchmark(Description = "StructPacker", Baseline = true)]
        public void StructPackerBenchLarge()
        {
            LargeValueMsg msg = _largeValueMsg;

            using PooledBuffer slice = msg.PackToBuffer();
            msg.Unpack(slice.Data);
        }

        [Benchmark(Description = "BinaryPack")]
        public void BinaryPackBenchLarge()
        {
            LargeValueMsg msg = _largeValueMsg;

            byte[] data = BinaryConverter.Serialize(msg);
            msg = BinaryConverter.Deserialize<LargeValueMsg>(data);
        }

        [Benchmark(Description = "MessagePack for C#")]
        public void MessagePackBench()
        {
            LargeValueMsg msg = _largeValueMsg;

            byte[] bytes = MessagePackSerializer.Serialize(msg);
            msg = MessagePackSerializer.Deserialize<LargeValueMsg>(bytes);
        }

        [Benchmark(Description = "BinaryFormatter")]
        public void BinaryFormatterBench()
        {
            LargeValueMsg msg = _largeValueMsg;

            using var memStr = new MemoryStream();

            _binFormatter.Serialize(memStr, msg);
            memStr.Position = 0;
            msg = (LargeValueMsg)_binFormatter.Deserialize(memStr);
        }

        [Benchmark(Description = "Newtonsoft.Json")]
        public void NewtonsoftJsonBench()
        {
            LargeValueMsg msg = _largeValueMsg;

            string data = JsonConvert.SerializeObject(msg);
            msg = JsonConvert.DeserializeObject<LargeValueMsg>(data);
        }
    }

    [MessagePackObject, Pack, Serializable]
    public struct LargeValueMsg
    {
        [Key(0)]
        public short[] Prop1 { get; set; }

        [Key(1)]
        public int[] Prop2 { get; set; }

        [Key(2)]
        public long[] Prop3 { get; set; }

        [Key(3)]
        public ushort[] Prop4 { get; set; }

        [Key(4)]
        public uint[] Prop5 { get; set; }

        [Key(5)]
        public ulong[] Prop6 { get; set; }

        [Key(6)]
        public byte[] Prop7 { get; set; }

        [Key(7)]
        public sbyte[] Prop8 { get; set; }

        [Key(8)]
        public string[] Prop9 { get; set; }

        [Key(9)]
        public DateTime[] Prop10 { get; set; }

        [Key(10)]
        public TimeSpan[] Prop11 { get; set; }

        [Key(11)]
        public bool[] Prop12 { get; set; }

        [Key(12)]
        public char[] Prop13 { get; set; }

        [Key(13)]
        public decimal[] Prop14 { get; set; }
    }
}