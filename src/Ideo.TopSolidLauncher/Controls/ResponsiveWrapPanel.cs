using System.Windows;
using System.Windows.Controls;

namespace Ideo.TopSolidLauncher.Controls;

public sealed class ResponsiveWrapPanel : Panel
{
    public static readonly DependencyProperty TargetItemWidthProperty = DependencyProperty.Register(
        nameof(TargetItemWidth), typeof(double), typeof(ResponsiveWrapPanel),
        new FrameworkPropertyMetadata(314d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth), typeof(double), typeof(ResponsiveWrapPanel),
        new FrameworkPropertyMetadata(270d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxItemWidthProperty = DependencyProperty.Register(
        nameof(MaxItemWidth), typeof(double), typeof(ResponsiveWrapPanel),
        new FrameworkPropertyMetadata(350d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
        nameof(HorizontalSpacing), typeof(double), typeof(ResponsiveWrapPanel),
        new FrameworkPropertyMetadata(16d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
        nameof(VerticalSpacing), typeof(double), typeof(ResponsiveWrapPanel),
        new FrameworkPropertyMetadata(16d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double TargetItemWidth { get => (double)GetValue(TargetItemWidthProperty); set => SetValue(TargetItemWidthProperty, value); }
    public double MinItemWidth { get => (double)GetValue(MinItemWidthProperty); set => SetValue(MinItemWidthProperty, value); }
    public double MaxItemWidth { get => (double)GetValue(MaxItemWidthProperty); set => SetValue(MaxItemWidthProperty, value); }
    public double HorizontalSpacing { get => (double)GetValue(HorizontalSpacingProperty); set => SetValue(HorizontalSpacingProperty, value); }
    public double VerticalSpacing { get => (double)GetValue(VerticalSpacingProperty); set => SetValue(VerticalSpacingProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? Math.Max(MinItemWidth, TargetItemWidth)
            : Math.Max(0, availableSize.Width);
        var itemWidth = CalculateItemWidth(width, out var columns);

        foreach (UIElement child in InternalChildren)
            child.Measure(new Size(itemWidth, double.PositiveInfinity));

        var height = CalculateRows(itemWidth, columns, arrange: false);
        return new Size(double.IsInfinity(availableSize.Width) ? itemWidth : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = CalculateItemWidth(finalSize.Width, out var columns);
        var height = CalculateRows(itemWidth, columns, arrange: true);
        return new Size(finalSize.Width, height);
    }

    private double CalculateRows(double itemWidth, int columns, bool arrange)
    {
        var y = 0d;
        var index = 0;
        while (index < InternalChildren.Count)
        {
            var count = Math.Min(columns, InternalChildren.Count - index);
            var rowHeight = 0d;
            for (var offset = 0; offset < count; offset++)
                rowHeight = Math.Max(rowHeight, InternalChildren[index + offset].DesiredSize.Height);

            if (arrange)
            {
                for (var offset = 0; offset < count; offset++)
                {
                    var x = offset * (itemWidth + HorizontalSpacing);
                    InternalChildren[index + offset].Arrange(new Rect(x, y, itemWidth, rowHeight));
                }
            }

            y += rowHeight;
            index += count;
            if (index < InternalChildren.Count)
                y += VerticalSpacing;
        }
        return y;
    }

    private double CalculateItemWidth(double availableWidth, out int columns)
    {
        if (availableWidth <= 0)
        {
            columns = 1;
            return MinItemWidth;
        }

        columns = Math.Max(1, (int)Math.Floor((availableWidth + HorizontalSpacing) /
                                              (Math.Max(MinItemWidth, TargetItemWidth) + HorizontalSpacing)));
        var width = (availableWidth - HorizontalSpacing * (columns - 1)) / columns;
        return Math.Clamp(width, Math.Min(MinItemWidth, MaxItemWidth), Math.Max(MinItemWidth, MaxItemWidth));
    }
}
