using System;

namespace umekan
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadonlyInScriptAttribute : Attribute
    {
    }
}