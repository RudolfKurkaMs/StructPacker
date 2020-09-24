using RudolfKurka.StructPackerTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SB = System.SerializableAttribute;
using SB2 = RudolfKurka.StructPackerTools.PackAttribute;

namespace AttachableDebugApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Test test = new Test(); //, Prop2 = 45.5
            byte[] b = test.Pack(); 
        }

        [SB, Pack]
        public struct Test
        {
            private double prop1;

            private double Prop6 { get; }

            //[SkipPack]
            public double Prop1 { get => prop1; set => prop1 = value; }

            [SkipPack]
            public double Prop2 { get; private set; }
                        
            internal double Prop3 { get; set; }
        }
    }

    [SB]
    public struct Test
    {
        public double Prop1 { get; set; }
    }
}