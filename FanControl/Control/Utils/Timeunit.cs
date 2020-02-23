using System;


public enum TimeUnit
{
    Millisecond,
    Second,
    Minute,
    Hour,
    Day
}

public class TimeUnitHelper
{
    public static int UnitConvert(TimeUnit source, TimeUnit target, int time)
    {
        switch (source)
        {
            case TimeUnit.Millisecond:
                break;
            case TimeUnit.Second:
                time *= 1000;
                break;
            case TimeUnit.Minute:
                time *= (1000 * 60);
                break;
            case TimeUnit.Hour:
                time *= (1000 * 60 * 60);
                break;
            case TimeUnit.Day:
                time *= (1000 * 60 * 60 * 24);
                break;
            default:
                throw new ArgumentException("Invaild TimeUnit -> source");
        }
        switch (target)
        {
            case TimeUnit.Millisecond:
                return time;
            case TimeUnit.Second:
                return time / 1000;
            case TimeUnit.Minute:
                return time / (1000 * 60);
            case TimeUnit.Hour:
                return time / (1000 * 60 * 60);
            case TimeUnit.Day:
                return time / (1000 * 60 * 60 * 24);
            default:
                throw new ArgumentException("Invaild TimeUnit -> target");
        }
    }
}

