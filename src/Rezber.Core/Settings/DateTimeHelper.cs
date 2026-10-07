using System.Globalization;

namespace Rezber.Core.Settings;

public class DateTimeHelper
{
    public static DateTime ToDateTimeUTC(string param, CultureInfo? cultureInfo = null)
    {
        cultureInfo = (cultureInfo is null) ?
            CultureInfo.InvariantCulture :
            CultureInfo.CreateSpecificCulture(cultureInfo.Name);

        DateTime dateTime = DateTime.ParseExact(param, "yyyy-MM-dd HH:mm:ss",
                                       cultureInfo);

        return dateTime.ToUniversalTime();
    }
}