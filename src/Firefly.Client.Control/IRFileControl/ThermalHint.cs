using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalHint : FrameworkElement
    {
        private const int DEFAULT_X_OFFSET = 10;
        private const int DEFAULT_Y_OFFSET = 15;
        private const int SPOT_X_OFFSET = 4;
        private const int SPOT_Y_OFFSET = 4;

        private string temperatureHint;
        private Rect thermalArea;
        private Point lastMouseOverPoint;

        internal string TemperatureHint
        {
            get { return temperatureHint; }
            set
            {
                if (temperatureHint != value)
                {
                    temperatureHint = value;
                    InvalidateVisual();
                }
            }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(ThermalHint),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ThermalHint),
            new FrameworkPropertyMetadata(FontWeights.Bold, FrameworkPropertyMetadataOptions.AffectsRender));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(ThermalHint),
            new FrameworkPropertyMetadata(new FontFamily("Arial"), FrameworkPropertyMetadataOptions.AffectsRender));

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(ThermalHint),
            new FrameworkPropertyMetadata(18.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public ShapeCreatorType ShapeCreatorType
        {
            get { return (ShapeCreatorType)GetValue(ShapeCreatorTypeProperty); }
            set { SetValue(ShapeCreatorTypeProperty, value); }
        }

        public static readonly DependencyProperty ShapeCreatorTypeProperty = DependencyProperty.Register("ShapeCreatorType", typeof(ShapeCreatorType), typeof(ThermalHint),
            new FrameworkPropertyMetadata(ShapeCreatorType.None, FrameworkPropertyMetadataOptions.AffectsRender));

        internal Rect ThermalArea
        {
            get { return thermalArea; }
            set
            {
                if (thermalArea != value)
                {
                    thermalArea = value;
                    InvalidateVisual();
                }
            }
        }

        internal Point LastMouseOverPoint
        {
            get { return lastMouseOverPoint; }
            set
            {
                if (lastMouseOverPoint != value)
                {
                    lastMouseOverPoint = value;
                    InvalidateVisual();
                }
            }
        }

        public CanvasMode CanvasMode
        {
            get { return (CanvasMode)GetValue(CanvasModeProperty); }
            set { SetValue(CanvasModeProperty, value); }
        }

        public static readonly DependencyProperty CanvasModeProperty = DependencyProperty.Register("CanvasMode", typeof(CanvasMode), typeof(ThermalHint),
            new FrameworkPropertyMetadata(CanvasMode.Analysis, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (CanvasMode != CanvasMode.Analysis || string.IsNullOrEmpty(TemperatureHint))
            {
                return;
            }

            bool showHint = false;
            int x = DEFAULT_X_OFFSET, y = DEFAULT_Y_OFFSET;
            switch (ShapeCreatorType)
            {
                case ShapeCreatorType.None:
                    showHint = true;
                    x = DEFAULT_X_OFFSET;
                    y = DEFAULT_Y_OFFSET;
                    break;
                case ShapeCreatorType.Spot:
                    showHint = true;
                    x = SPOT_X_OFFSET;
                    y = SPOT_Y_OFFSET;
                    break;
            }

            if (showHint)
            {
                Rect thermalArea = ThermalArea;
                RenderFontParams renderFontParams = new RenderFontParams();
                renderFontParams.FontStyle = FontStyle;
                renderFontParams.FontWeight = FontWeight;
                renderFontParams.FontFamily = FontFamily;
                renderFontParams.FontSize = FontSize;
                FormattedText ft = ThermalCommon.GetFormattedText(TemperatureHint, renderFontParams);

                Point tmpPoint = ThermalCommon.GetNearestPointInArea(LastMouseOverPoint, ThermalArea);
                bool right = true;
                if (tmpPoint.X + x + ft.Width > thermalArea.Right)
                {
                    right = false;
                    switch (ShapeCreatorType)
                    {
                        case ShapeCreatorType.None:
                            x = 0;
                            break;
                        case ShapeCreatorType.Spot:
                            x = -SPOT_X_OFFSET;
                            break;
                    }
                }
                bool bottom = true;
                if (tmpPoint.Y + y + ft.Height > thermalArea.Bottom)
                {
                    bottom = false;
                    switch (ShapeCreatorType)
                    {
                        case ShapeCreatorType.None:
                            y = 0;
                            break;
                        case ShapeCreatorType.Spot:
                            y = -SPOT_Y_OFFSET;
                            break;
                    }
                }

                Point point = new Point(tmpPoint.X + x, tmpPoint.Y + y);
                HorizontalAlignment hAlignment = right ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                VerticalAlignment vAlignment = bottom ? VerticalAlignment.Top : VerticalAlignment.Bottom;

                Position position = new Position(point, hAlignment, vAlignment);

                ThermalCommon.DrawOutlinedText(drawingContext, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderFontParams.FontSize), position);
            }
        }

        internal void ClearThermalArea()
        {
            ThermalArea = new Rect();
        }

        internal void ClearTemperatureHint()
        {
            TemperatureHint = string.Empty;
        }
    }
}
