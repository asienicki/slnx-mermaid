using System;
using System.Globalization;

namespace SlnxMermaid.Gui.Avalonia.Services;

public static class ValueConversion
{
    public static object? ConvertTo(object? value, Type targetType)
    {
        if (value == null)
            return Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType ? null : Activator.CreateInstance(targetType);

        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullableType.IsEnum)
        {
            if (value.GetType() == nonNullableType)
                return value;

            return Enum.Parse(nonNullableType, value.ToString() ?? string.Empty);
        }

        if (nonNullableType == typeof(string))
            return value.ToString();

        if (nonNullableType == typeof(bool))
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

        return Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);
    }

    public static bool TryConvertTo(string? value, Type targetType, out object? converted, out string? error)
    {
        converted = null;
        error = null;

        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (string.IsNullOrWhiteSpace(value) && Nullable.GetUnderlyingType(targetType) != null)
            return true;

        try
        {
            if (nonNullableType == typeof(int))
                converted = int.Parse(value ?? string.Empty, CultureInfo.InvariantCulture);
            else if (nonNullableType == typeof(double))
                converted = double.Parse(value ?? string.Empty, CultureInfo.InvariantCulture);
            else if (nonNullableType == typeof(decimal))
                converted = decimal.Parse(value ?? string.Empty, CultureInfo.InvariantCulture);
            else
                converted = Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);

            return true;
        }
        catch (FormatException)
        {
            error = "Enter a valid number.";
            return false;
        }
        catch (OverflowException)
        {
            error = "Number is outside the supported range.";
            return false;
        }
    }
}
