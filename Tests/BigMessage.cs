using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StructPacker;

namespace Tests
{
    internal struct BigMessage : IEquatable<BigMessage>
    {
        public short Prop1 { get; set; }
        public int Prop2 { get; set; }
        public long Prop3 { get; set; }
        public ushort Prop4 { get; set; }
        public uint Prop5 { get; set; }
        public ulong Prop6 { get; set; }
        public byte Prop7 { get; set; }
        public sbyte Prop8 { get; set; }
        public string Prop9 { get; set; }
        public DateTime Prop10 { get; set; }
        public TimeSpan Prop11 { get; set; }
        public bool Prop12 { get; set; }
        public char Prop13 { get; set; }
        public short[] Prop14 { get; set; }
        public int[] Prop15 { get; set; }
        public long[] Prop16 { get; set; }
        public ushort[] Prop17 { get; set; }
        public uint[] Prop18 { get; set; }
        public ulong[] Prop19 { get; set; }
        public byte[] Prop20 { get; set; }
        public sbyte[] Prop21 { get; set; }
        public string[] Prop22 { get; set; }
        public DateTime[] Prop23 { get; set; }
        public TimeSpan[] Prop24 { get; set; }
        public bool[] Prop25 { get; set; }
        public char[] Prop26 { get; set; }
        public float Prop27 { get; set; }
        public double Prop28 { get; set; }
        public decimal Prop29 { get; set; }
        public float[] Prop30 { get; set; }
        public double[] Prop31 { get; set; }
        public decimal[] Prop32 { get; set; }

        public BigMessage(bool _)
            : this()
        {
            Prop1 = short.MaxValue;
            Prop2 = int.MaxValue;
            Prop3 = long.MaxValue;
            Prop4 = ushort.MaxValue;
            Prop5 = uint.MaxValue;
            Prop6 = ulong.MaxValue;
            Prop7 = byte.MaxValue;
            Prop8 = sbyte.MaxValue;
            Prop9 = "Test";
            Prop10 = DateTime.MaxValue;
            Prop11 = TimeSpan.MaxValue;
            Prop12 = true;
            Prop13 = 'T';
            Prop14 = new[] { Prop1, Prop1 };
            Prop15 = new[] { Prop2, Prop2 };
            Prop16 = new[] { Prop3, Prop3 };
            Prop17 = new[] { Prop4, Prop4 };
            Prop18 = new[] { Prop5, Prop5 };
            Prop19 = new[] { Prop6, Prop6 };
            Prop20 = new[] { Prop7, Prop7 };
            Prop21 = new[] { Prop8, Prop8 };
            Prop22 = new[] { Prop9, Prop9 };
            Prop23 = new[] { Prop10, Prop10 };
            Prop24 = new[] { Prop11, Prop11 };
            Prop25 = new[] { Prop12, Prop12 };
            Prop26 = new[] { Prop13, Prop13 };
            Prop27 = float.MaxValue;
            Prop28 = double.MaxValue;
            Prop29 = decimal.MaxValue;
            Prop30 = new[] { Prop27, Prop27 };
            Prop31 = new[] { Prop28, Prop28 };
            Prop32 = new[] { Prop29, Prop29 };
        }

        public static void Unpack(ref BigMessage o, Stream src, byte[] gpbuffer)
        {
            o.Prop1 = Tools.ReadFromStream(o.Prop1, src, gpbuffer);
            o.Prop2 = Tools.ReadFromStream(o.Prop2, src, gpbuffer);
            o.Prop3 = Tools.ReadFromStream(o.Prop3, src, gpbuffer);
            o.Prop4 = Tools.ReadFromStream(o.Prop4, src, gpbuffer);
            o.Prop5 = Tools.ReadFromStream(o.Prop5, src, gpbuffer);
            o.Prop6 = Tools.ReadFromStream(o.Prop6, src, gpbuffer);
            o.Prop7 = Tools.ReadFromStream(o.Prop7, src, gpbuffer);
            o.Prop8 = Tools.ReadFromStream(o.Prop8, src, gpbuffer);
            o.Prop9 = Tools.ReadFromStream(o.Prop9, src, gpbuffer);
            o.Prop10 = Tools.ReadFromStream(o.Prop10, src, gpbuffer);
            o.Prop11 = Tools.ReadFromStream(o.Prop11, src, gpbuffer);
            o.Prop12 = Tools.ReadFromStream(o.Prop12, src, gpbuffer);
            o.Prop13 = Tools.ReadFromStream(o.Prop13, src, gpbuffer);
            o.Prop14 = Tools.ReadFromStream(o.Prop14, src, gpbuffer);
            o.Prop15 = Tools.ReadFromStream(o.Prop15, src, gpbuffer);
            o.Prop16 = Tools.ReadFromStream(o.Prop16, src, gpbuffer);
            o.Prop17 = Tools.ReadFromStream(o.Prop17, src, gpbuffer);
            o.Prop18 = Tools.ReadFromStream(o.Prop18, src, gpbuffer);
            o.Prop19 = Tools.ReadFromStream(o.Prop19, src, gpbuffer);
            o.Prop20 = Tools.ReadFromStream(o.Prop20, src, gpbuffer);
            o.Prop21 = Tools.ReadFromStream(o.Prop21, src, gpbuffer);
            o.Prop22 = Tools.ReadFromStream(o.Prop22, src, gpbuffer);
            o.Prop23 = Tools.ReadFromStream(o.Prop23, src, gpbuffer);
            o.Prop24 = Tools.ReadFromStream(o.Prop24, src, gpbuffer);
            o.Prop25 = Tools.ReadFromStream(o.Prop25, src, gpbuffer);
            o.Prop26 = Tools.ReadFromStream(o.Prop26, src, gpbuffer);
            o.Prop27 = Tools.ReadFromStream(o.Prop27, src, gpbuffer);
            o.Prop28 = Tools.ReadFromStream(o.Prop28, src, gpbuffer);
            o.Prop29 = Tools.ReadFromStream(o.Prop29, src, gpbuffer);
            o.Prop30 = Tools.ReadFromStream(o.Prop30, src, gpbuffer);
            o.Prop31 = Tools.ReadFromStream(o.Prop31, src, gpbuffer);
            o.Prop32 = Tools.ReadFromStream(o.Prop32, src, gpbuffer);
        }

        public static void Unpack(ref BigMessage msg, byte[] srcbytes, ref int startindex)
        {
            using PooledBuffer gpBuff = PooledBuffer.Get(16);
            Unpack(ref msg, new MemoryStream(srcbytes, startindex, srcbytes.Length - startindex), gpBuff.Data);
        }

        public static void Pack(ref BigMessage msg, byte[] destbytes, ref int index)
        {
            Tools.Write(msg.Prop1, destbytes, ref index);
            Tools.Write(msg.Prop2, destbytes, ref index);
            Tools.Write(msg.Prop3, destbytes, ref index);
            Tools.Write(msg.Prop4, destbytes, ref index);
            Tools.Write(msg.Prop5, destbytes, ref index);
            Tools.Write(msg.Prop6, destbytes, ref index);
            Tools.Write(msg.Prop7, destbytes, ref index);
            Tools.Write(msg.Prop8, destbytes, ref index);
            Tools.Write(msg.Prop9, destbytes, ref index);
            Tools.Write(msg.Prop10, destbytes, ref index);
            Tools.Write(msg.Prop11, destbytes, ref index);
            Tools.Write(msg.Prop12, destbytes, ref index);
            Tools.Write(msg.Prop13, destbytes, ref index);
            Tools.Write(msg.Prop14, destbytes, ref index);
            Tools.Write(msg.Prop15, destbytes, ref index);
            Tools.Write(msg.Prop16, destbytes, ref index);
            Tools.Write(msg.Prop17, destbytes, ref index);
            Tools.Write(msg.Prop18, destbytes, ref index);
            Tools.Write(msg.Prop19, destbytes, ref index);
            Tools.Write(msg.Prop20, destbytes, ref index);
            Tools.Write(msg.Prop21, destbytes, ref index);
            Tools.Write(msg.Prop22, destbytes, ref index);
            Tools.Write(msg.Prop23, destbytes, ref index);
            Tools.Write(msg.Prop24, destbytes, ref index);
            Tools.Write(msg.Prop25, destbytes, ref index);
            Tools.Write(msg.Prop26, destbytes, ref index);
            Tools.Write(msg.Prop27, destbytes, ref index);
            Tools.Write(msg.Prop28, destbytes, ref index);
            Tools.Write(msg.Prop29, destbytes, ref index);
            Tools.Write(msg.Prop30, destbytes, ref index);
            Tools.Write(msg.Prop31, destbytes, ref index);
            Tools.Write(msg.Prop32, destbytes, ref index);
        }

        public int GetSize()
        {
            return Tools.GetSize(Prop1)
                   + Tools.GetSize(Prop2)
                   + Tools.GetSize(Prop3)
                   + Tools.GetSize(Prop4)
                   + Tools.GetSize(Prop5)
                   + Tools.GetSize(Prop6)
                   + Tools.GetSize(Prop7)
                   + Tools.GetSize(Prop8)
                   + Tools.GetSize(Prop9)
                   + Tools.GetSize(Prop10)
                   + Tools.GetSize(Prop11)
                   + Tools.GetSize(Prop12)
                   + Tools.GetSize(Prop13)
                   + Tools.GetSize(Prop14)
                   + Tools.GetSize(Prop15)
                   + Tools.GetSize(Prop16)
                   + Tools.GetSize(Prop17)
                   + Tools.GetSize(Prop18)
                   + Tools.GetSize(Prop19)
                   + Tools.GetSize(Prop20)
                   + Tools.GetSize(Prop21)
                   + Tools.GetSize(Prop22)
                   + Tools.GetSize(Prop23)
                   + Tools.GetSize(Prop24)
                   + Tools.GetSize(Prop25)
                   + Tools.GetSize(Prop26)
                   + Tools.GetSize(Prop27)
                   + Tools.GetSize(Prop28)
                   + Tools.GetSize(Prop29)
                   + Tools.GetSize(Prop30)
                   + Tools.GetSize(Prop31)
                   + Tools.GetSize(Prop32);
        }

        public bool Equals(BigMessage other)
        {
            return Prop1 == other.Prop1
                   && Prop2 == other.Prop2
                   && Prop3 == other.Prop3
                   && Prop4 == other.Prop4
                   && Prop5 == other.Prop5
                   && Prop6 == other.Prop6
                   && Prop7 == other.Prop7
                   && Prop8 == other.Prop8
                   && Prop9 == other.Prop9
                   && Prop10.Equals(other.Prop10)
                   && Prop11.Equals(other.Prop11)
                   && Prop12 == other.Prop12
                   && Prop13 == other.Prop13
                   && SequenceEqual(Prop14, other.Prop14)
                   && SequenceEqual(Prop15, other.Prop15)
                   && SequenceEqual(Prop16, other.Prop16)
                   && SequenceEqual(Prop17, other.Prop17)
                   && SequenceEqual(Prop18, other.Prop18)
                   && SequenceEqual(Prop19, other.Prop19)
                   && SequenceEqual(Prop20, other.Prop20)
                   && SequenceEqual(Prop21, other.Prop21)
                   && SequenceEqual(Prop22, other.Prop22)
                   && SequenceEqual(Prop23, other.Prop23)
                   && SequenceEqual(Prop24, other.Prop24)
                   && SequenceEqual(Prop25, other.Prop25)
                   && SequenceEqual(Prop26, other.Prop26)
                   && Prop27 == other.Prop27
                   && Prop28 == other.Prop28
                   && Prop29 == other.Prop29
                   && SequenceEqual(Prop30, other.Prop30)
                   && SequenceEqual(Prop31, other.Prop31)
                   && SequenceEqual(Prop32, other.Prop32);
        }

        private static bool SequenceEqual<T>(ICollection<T> left, ICollection<T> right)
        {
            if (Equals(left, right))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            if (left.Count != right.Count)
                return false;

            return left.SequenceEqual(right);
        }

        public override bool Equals(object obj)
        {
            return obj is BigMessage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(BigMessage left, BigMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BigMessage left, BigMessage right)
        {
            return !left.Equals(right);
        }
    }
}