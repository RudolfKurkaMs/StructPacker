## Overview

StructPacker is binary serializer that auto-generates C# serialization code to achieve peak runtime performance and efficiency.

## Features

* One of the fastest binary serialization you can get under .NET without going native.
* Very little to none collateral memory allocations resulting in low GC pressure. Well suited for devices with low memory and/or processing power like mobile phones, IoT or performance-critical apps like games.
* Supports C# language for generated output. Other .NET languages like VB.NET of F# could be added in the future.
* Output is fully managed code with single 20KB DLL dependency (added automatically as part of nuget package).
* No post-build steps, no runtime type inspections, no dynamic IL generation or native libraries which means high portability and compatibility.
* No fancy requirements. Can be installed in any C# .NET project that satisfies at least .NET Standard 1.0 api surface.
* Optimized serialization to/from byte array, stream or pooled byte array (using array pooling further increases efficiency).
* Serializable types:
  * Primitive types (bool, byte, sbyte, char, short, ushort, int, uint, long, ulong, float, double, decimal).
  * String, DateTime and TimeSpan.
  * Array (one-dimensional) version of all the above.

## How it works

StructPacker uses brand new feature of Visual Studio called [Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/).
Whenever you build your project (or just type something) StructPacker's component inspects your code, collects all structures that are marked with the attribute and emits tailor-made code that "knows" how to serialize and deserialize them.
This generated code is then automatically compiled into the project output as internal class.

This means StructPacker does not need to do any runtime type inspections or IL generation resulting in highly performant and highly portable code.

## Prerequisites

Visual Studio version 16.8 and above is required as its first version to support source generators.

**Note**: Visual Studio 16.8 is currently in its preview version.

Also Visual Studio enables source generators only for projects with "preview" language level (this is temporary measure). 
So, any project you install StructPacker in needs to have language version set to preview for it to work. You can do that by editing .csproj file and adding "\<LangVersion>preview\</LangVersion>" for example:

```
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>netcoreapp3.1</TargetFramework>
  <LangVersion>preview</LangVersion>
</PropertyGroup>
```

## Installation

Simply install the [nuget package](https://duckduckgo.com) and you're good to go.

## Usage

```csharp
using System;
using System.IO;
using StructPacker;
```

```csharp
[Pack]
internal struct ChatMsg
{
    public int ID { get; set; }
    public bool IsOutgoing { get; set; }
    public string Text { get; set; }
    public DateTime TimePosted { get; set; }
}
```

```csharp
//create a test message
var sourceMsg = new ChatMsg
{
    ID = 5,
    IsOutgoing = true,
    Text = "Test",
    TimePosted = DateTime.MaxValue
};

//save it to byte array
byte[] byteArr = sourceMsg.Pack();

//or save it to a pooled buffer (managed by StructPacker), once this instance is disposed its internal byte buffer is reclaimed and can be used again elsewhere
using PooledBuffer pooled = sourceMsg.PackToBuffer(); 

//byte data is available through Data property, number of valid bytes is in the Size property. Important: do not read past the Size property (it can be lower than actual length of the byte array)!
using var memStr = new MemoryStream(pooled.Data, 0, pooled.Size);

//declare message for deserializing
var unpackedMsg = new ChatMsg();

//load content from a stream
unpackedMsg.Unpack(memStr);

//or alternatively load it from a byte array (notice you can reuse the same structure for multiple uses)
unpackedMsg.Unpack(byteArr);
```

## Benchmarks

Test PC configuration:

```ini
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i5-9600K CPU 3.70GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
  DefaultJob : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
```

\
Test with small structure (395B):

|               Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------- |------------:|----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|         StructPacker |    294.9 ns |   5.87 ns |   5.49 ns |  1.00 |    0.00 | 0.1817 |      - |     - |     856 B |
|           [BinaryPack](https://github.com/Sergio0694/BinaryPack) |    324.6 ns |   6.27 ns |   5.86 ns |  1.10 |    0.03 | 0.2346 |      - |     - |    1104 B |
| [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) |  1,113.3 ns |  13.38 ns |  11.17 ns |  3.78 |    0.08 | 0.2346 |      - |     - |    1104 B |
|      [BinaryFormatter](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter?view=netcore-3.1) | 15,354.8 ns | 223.94 ns | 209.47 ns | 52.09 |    0.98 | 3.0212 | 0.0610 |     - |   14229 B |
|      [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13,402.5 ns | 254.67 ns | 272.49 ns | 45.38 |    1.17 | 2.5940 | 0.0305 |     - |   12232 B |

\
Test with large structure (~2MB):

|               Method |         Mean |       Error |      StdDev |  Ratio | RatioSD |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|--------------------- |-------------:|------------:|------------:|-------:|--------:|-----------:|----------:|----------:|----------:|
|         StructPacker |     767.8 us |    15.13 us |    22.18 us |   1.00 |    0.00 |   479.4922 |  412.1094 |  284.1797 |   2.59 MB |
|           [BinaryPack](https://github.com/Sergio0694/BinaryPack) |   2,344.8 us |    50.65 us |   149.34 us |   3.06 |    0.24 |  1089.8438 | 1035.1563 |  890.6250 |  10.66 MB |
| [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) |   8,118.8 us |    74.30 us |    69.50 us |  10.61 |    0.35 |  1140.6250 | 1062.5000 |  937.5000 |   4.63 MB |
|      [BinaryFormatter](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter?view=netcore-3.1) |   6,423.8 us |   143.63 us |   423.49 us |   8.27 |    0.67 |  1429.6875 | 1187.5000 |  859.3750 |   9.01 MB |
|      [Newtonsoft.Json](https://www.newtonsoft.com/json) | 191,304.4 us | 3,696.04 us | 3,954.72 us | 250.06 |    9.05 | 46000.0000 | 3000.0000 | 1000.0000 | 234.34 MB |

\
**Conclusions:**

StructPacker is faster and uses less memory than any other tested component.

However please note that in cases like Newtonsoft.Json the comparison only tells you that StructPacker is better at what it can do but Newtonsoft.Json can do much more things (trading off convenience for performance). It is also a text format (not a binary one).

Entire benchmark app is included in the source.

## FAQ

**Why use only structures and not classes?**

It's for performance optimization (not having to allocate objects on the heap). Also, it's easier to inspect structures as they cannot inherit from other types making the source generator's job easier.

**Are types marked as "partial" supported?**

At the moment no. Maybe in the future.

**Can StructPacker resolve type conflicts? Is there type-less api etc.?**

No, these features are considered outside of the scope of this project (in favor of performance and runtime efficiency).
Also note there is no type information in the output sequence meaning you have to know which structure to read next and if that structure isn't what has been serialized the stream gets corrupted.
It's up to the coder to prevent this situation.