using System;

namespace StructPacker
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipPackAttribute : Attribute
    {
    }
}