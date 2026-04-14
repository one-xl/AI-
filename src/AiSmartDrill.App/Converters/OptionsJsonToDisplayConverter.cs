using System.Globalization;
using System.Windows.Data;

namespace AiSmartDrill.App.Converters;

/// <summary>
/// DataGrid 绑定：将 <see cref="Domain.Question.OptionsJson"/> 转为多行可读选项文案。
/// </summary>
public sealed class OptionsJsonToDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => QuestionOptionsDisplayFormatter.FormatForDisplay(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
