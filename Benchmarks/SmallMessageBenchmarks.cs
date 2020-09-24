using BenchmarkDotNet.Attributes;
using BinaryPack;
using MessagePack;
using Newtonsoft.Json;
using StructPacker;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class SmallMessageBenchmarks
    {
        private readonly SmallValueMsg _smallValueMsg = new SmallValueMsg
        {
            Prop1 = -685,
            Prop2 = -98542654,
            Prop3 = -49846515616574,
            Prop4 = 685,
            Prop5 = 98542654,
            Prop6 = 49846515616574,
            Prop7 = 255,
            Prop8 = -120,
            Prop9 = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus sed pretium velit. Duis non nibh neque. In eu orci in ligula tincidunt porttitor eget vitae erat.
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus sed pretium velit. Duis non nibh neque. In eu orci in ligula tincidunt porttitor eget vitae erat.",
            Prop10 = new DateTime(2020, 3, 20, 10, 20, 30, DateTimeKind.Utc),
            Prop11 = new TimeSpan(20, 55, 12),
            Prop12 = true,
            Prop13 = 'T',
            Prop14 = 123456.123456m
        };

        private readonly BinaryFormatter _binFormatter = new BinaryFormatter();
               
        [Benchmark(Description = "StructPacker", Baseline = true)]
        public void StructPackerBench()
        {
            SmallValueMsg msg = _smallValueMsg;
            
            using PooledBuffer slice = msg.PackToBuffer();
            msg.Unpack(slice.Data);
        }
        
        [Benchmark(Description = "BinaryPack")]
        public void BinaryPackBench()
        {
            SmallValueMsg msg = _smallValueMsg;
        
            byte[] data = BinaryConverter.Serialize(msg);
            msg = BinaryConverter.Deserialize<SmallValueMsg>(data);
        }

        [Benchmark(Description = "MessagePack for C#")]
        public void MessagePackBench()
        {
            SmallValueMsg msg = _smallValueMsg;

            byte[] bytes = MessagePackSerializer.Serialize(msg);
            msg = MessagePackSerializer.Deserialize<SmallValueMsg>(bytes);
        }

        [Benchmark(Description = "BinaryFormatter")]
        public void BinaryFormatterBench()
        {
            SmallValueMsg msg = _smallValueMsg;

            using var memStr = new MemoryStream();

            _binFormatter.Serialize(memStr, msg);
            memStr.Position = 0;
            msg = (SmallValueMsg)_binFormatter.Deserialize(memStr);
        }

        [Benchmark(Description = "Newtonsoft.Json")]
        public void NewtonsoftJsonBench()
        {
            SmallValueMsg msg = _smallValueMsg;

            string data = JsonConvert.SerializeObject(msg);
            msg = JsonConvert.DeserializeObject<SmallValueMsg>(data);
        }
    }

    [MessagePackObject, Pack, Serializable]
    public struct SmallValueMsg
    {
        [Key(0)]
        public short Prop1 { get; set; }

        [Key(1)]
        public int Prop2 { get; set; }

        [Key(2)]
        public long Prop3 { get; set; }

        [Key(3)]
        public ushort Prop4 { get; set; }

        [Key(4)]
        public uint Prop5 { get; set; }

        [Key(5)]
        public ulong Prop6 { get; set; }

        [Key(6)]
        public byte Prop7 { get; set; }

        [Key(7)]
        public sbyte Prop8 { get; set; }

        [Key(8)]
        public string Prop9 { get; set; }

        [Key(9)]
        public DateTime Prop10 { get; set; }

        [Key(10)]
        public TimeSpan Prop11 { get; set; }

        [Key(11)]
        public bool Prop12 { get; set; }

        [Key(12)]
        public char Prop13 { get; set; }

        [Key(13)]
        public decimal Prop14 { get; set; }
    }
}