using System;
using System.Globalization;
using System.Windows.Controls;


public class NumberCheck : ValidationRule
{
    double max;
    double min;
    public NumberCheck(double max, double min)
    {
        this.max = max;
        this.min = min;
    }
    public NumberCheck()
    {
        this.max = Double.PositiveInfinity;
        this.min = Double.NegativeInfinity;
    }
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        int number;
        try
        {
            number = Convert.ToInt32(value);
        }
        catch (Exception e)
        {
            return new ValidationResult(false, e.Message);
        }
        return number <= max && number >= min ? new ValidationResult(true, null) : new ValidationResult(false, string.Format("Out of Range: Max:{0} Min{1}", max, min));
    }
}

