namespace Rezber.Core.Helpers;

public static class IntExtensions
{
          public static int CalculateReadTime(this string content)
          {
                    if (string.IsNullOrWhiteSpace(content)) return 1;
                    var words = content.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
                    return Math.Max(1, (int)Math.Ceiling(words / 200.0));
          }
}