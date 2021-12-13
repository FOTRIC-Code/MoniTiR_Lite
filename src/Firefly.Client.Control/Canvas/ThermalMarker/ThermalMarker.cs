using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public enum ThermalMarkerType
    {
        Spot = 1,
        Rectangle = 3,
        Ellipse = 4,
        Polygon = 5,
        Polyline = 6,
        Mask = 7,
    }

    public struct RenderParams
    {
        public bool ShowMaxPoint { get; set; }
        public bool ShowMinPoint { get; set; }
        public bool ShowMax { get; set; }
        public bool ShowMin { get; set; }
        public bool ShowAvg { get; set; }
        public bool ShowDiff { get; set; }
        public bool FollowMaxMinPoint { get; set; }
        public bool AutoPosition { get; set; }
        public RenderFontParams FontParams { get; set; }
    }

    public abstract class ThermalMarker
    {
        protected const int CONTAIN_RADIUS = 4;
        protected const int SELECT_PIVOT_RADIUS = 4;

        protected readonly static Pen MarkerThickShapePen = new Pen(Brushes.Black, 3.0F);
        protected readonly static Pen MaskThickShapePen = new Pen(Brushes.Red, 3.0F);
        protected readonly static Pen MarkerThinShapePen = new Pen(Brushes.White, 1.0F);
        protected readonly static Pen MarkerHightlightPen = new Pen(new SolidColorBrush(Color.FromRgb(140, 177, 218)), 3.0F);
        protected readonly static Brush HotBadgeBrush = new SolidColorBrush(Color.FromRgb(132, 0, 0));
        protected readonly static Brush ColdBadgeBrush = new SolidColorBrush(Color.FromRgb(8, 28, 132));
        protected readonly static Pen BadgeBorder = new Pen(Brushes.White, 1.0F);

        public ThermalMarker(string name)
        {
            Name = name;
            IsMoving = false;
            MovingIndex = -1;
            LocationOffset = new Vector(0.0, 0.0);
            IsMoved = false;
            IsSelected = false;
        }

        public abstract ThermalMarkerType Type { get; }

        public string Name { get; set; }

        public bool IsMoving { get; private set; }

        protected int MovingIndex { get; private set; }

        protected Vector LocationOffset { get; set; }

        protected bool IsMoved { get; set; }

        public bool IsSelected { get; set; }

        public abstract bool Contains(Point point);

        public virtual void BeginMove(Point location, int index)
        {
            IsMoving = true;
            MovingIndex = index;
            LocationOffset = new Vector(0.0, 0.0);
            IsMoved = false;
        }

        public abstract void DoMove(Point location, Rect area);

        public void EndMove()
        {
            IsMoving = false;
            MovingIndex = -1;
            LocationOffset = new Vector(0.0, 0.0);
            IsMoved = false;
        }

        public abstract bool TrySelectPivot(Point point, out int index);

        public abstract void DoRender(DrawingContext dc, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles);

        protected static void DrawMarkerTitleText(DrawingContext dc, string text, Point point, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            FormattedText ft = ThermalCommon.GetFormattedText(text, renderParams.FontParams);
            if (renderParams.AutoPosition)
            {
                int radius = 4;
                Position[] positions = new Position[]
                {
                    new Position(new Point(point.X, point.Y - radius), HorizontalAlignment.Left, VerticalAlignment.Bottom),
                    new Position(new Point(point.X + radius, point.Y + radius), HorizontalAlignment.Left, VerticalAlignment.Top),
                    new Position(new Point(point.X - radius, point.Y + radius), HorizontalAlignment.Right, VerticalAlignment.Top),
                    new Position(new Point(point.X, point.Y - radius), HorizontalAlignment.Right, VerticalAlignment.Bottom),
                };

                DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), positions, parentRectangle, ref subRectangles);
            }
            else
            {
                Position position = new Position(point, HorizontalAlignment.Left, VerticalAlignment.Bottom);

                ThermalCommon.DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), position);
            }
        }

        protected static void DrawMarkerMaxText(DrawingContext dc, string text, Point point, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            FormattedText ft = ThermalCommon.GetFormattedText(text, renderParams.FontParams);
            if (renderParams.AutoPosition)
            {
                int radius = 8;
                Position[] positions = new Position[]
                {
                    new Position(point, HorizontalAlignment.Center, VerticalAlignment.Bottom),
                    new Position(new Point(point.X + radius, point.Y), HorizontalAlignment.Left, VerticalAlignment.Top),
                    new Position(new Point(point.X, point.Y + 2 * radius), HorizontalAlignment.Center, VerticalAlignment.Top),
                    new Position(new Point(point.X - radius, point.Y), HorizontalAlignment.Right, VerticalAlignment.Top),
                };

                DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), positions, parentRectangle, ref subRectangles);
            }
            else
            {
                Position position = new Position(point, HorizontalAlignment.Center, VerticalAlignment.Bottom);

                ThermalCommon.DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), position);
            }
        }

        protected static void DrawMarkerMinText(DrawingContext dc, string text, Point point, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            FormattedText ft = ThermalCommon.GetFormattedText(text, renderParams.FontParams);
            if (renderParams.AutoPosition)
            {
                int radius = 8;
                Position[] positions = new Position[]
                {
                    new Position(point, HorizontalAlignment.Center, VerticalAlignment.Top),
                    new Position(new Point(point.X - radius, point.Y), HorizontalAlignment.Right, VerticalAlignment.Bottom),
                    new Position(new Point(point.X, point.Y - 2 * radius), HorizontalAlignment.Center, VerticalAlignment.Bottom),
                    new Position(new Point(point.X + radius, point.Y), HorizontalAlignment.Left, VerticalAlignment.Bottom),
                };

                DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), positions, parentRectangle, ref subRectangles);
            }
            else
            {
                Position position = new Position(point, HorizontalAlignment.Center, VerticalAlignment.Top);

                ThermalCommon.DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), position);
            }
        }

        protected static void DrawOutlinedText(DrawingContext dc, FormattedText ft, Brush brush, Pen pen, Position[] positions, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            if (positions.Length == 0)
            {
                return;
            }

            Size tmpSize = new Size(ft.Width, ft.Height);
            double area = double.MaxValue;
            Position position = Position.Empty;
            Rect rectangle = Rect.Empty;

            for (int i = 0; i < positions.Length; i++)
            {
                Position tmpPosition = positions[i];
                Rect tmpRectangle = GetTextRectangle(tmpSize, tmpPosition);
                double tmpArea = Intersect(parentRectangle, subRectangles, tmpRectangle);

                if (tmpArea < double.Epsilon)
                {
                    subRectangles.Add(tmpRectangle);
                    ThermalCommon.DrawOutlinedText(dc, ft, brush, pen, tmpPosition);
                    return;
                }

                if (area > tmpArea)
                {
                    area = tmpArea;
                    position = tmpPosition;
                    rectangle = tmpRectangle;
                }
            }

            subRectangles.Add(rectangle);
            ThermalCommon.DrawOutlinedText(dc, ft, brush, pen, position);
        }

        protected static Geometry GetHighBadgePath(Point point)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                int radius = 8;
                ctx.BeginFigure(point, true, true);
                ctx.LineTo(new Point(point.X + radius, point.Y + 2 * radius), true, false);
                ctx.LineTo(new Point(point.X - radius, point.Y + 2 * radius), true, false);
            }
            return geometry;
        }

        protected static Geometry GetLowBadgePath(Point point)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                int radius = 8;
                ctx.BeginFigure(point, true, true);
                ctx.LineTo(new Point(point.X + radius, point.Y - 2 * radius), true, false);
                ctx.LineTo(new Point(point.X - radius, point.Y - 2 * radius), true, false);
            }
            return geometry;
        }

        protected static Rect GetTextRectangle(Size size, Position position)
        {
            Point point = position.Point;
            HorizontalAlignment hAlignment = position.HAlignment;
            VerticalAlignment vAlignment = position.VAlignment;

            switch (hAlignment)
            {
                case HorizontalAlignment.Center:
                    point.X -= size.Width / 2;
                    break;
                case HorizontalAlignment.Right:
                    point.X -= size.Width;
                    break;
            }

            switch (vAlignment)
            {
                case VerticalAlignment.Center:
                    point.Y -= size.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    point.Y -= size.Height;
                    break;
            }

            return new Rect(point, size);
        }

        protected static double Intersect(Rect destRectangle, List<Rect> subRectangles, Rect srcRectangle)
        {
            double area = 0;
            if (destRectangle.Contains(srcRectangle))
            {
                for (int i = 0; i < subRectangles.Count; i++)
                {
                    Rect tmpRectangle = Rect.Intersect(subRectangles[i], srcRectangle);
                    if (tmpRectangle != Rect.Empty)
                    {
                        area += tmpRectangle.Width * tmpRectangle.Height;
                    }
                }
            }
            else
            {
                area = srcRectangle.Width * srcRectangle.Height;
            }
            return area;
        }
    }    
}
