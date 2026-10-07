
using System.Reflection;

namespace Rezber.Core;

public static class AssemblyMarker
{
    public static readonly Assembly AssemblyType = typeof(AssemblyMarker).Assembly;
}