using System.Reflection;

namespace Rezber.Services;

public static class AssemblyMarker
{
    public static Assembly AssemblyName 
        = typeof(Mappers).Assembly;
}