using Firefly.Client.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalMarkerSpot : ThermalMarker
    {
        private const int DRAW_RADIUS = 10;

        public ThermalMarkerSpot(string name)
            : base(name)
        {
            Point = new Point(0.0, 0.0);
            Value = 293.15F;
        }

        public override ThermalMarkerType Type
        {
            get { return ThermalMarkerType.Spot; }
        }

        public Point Point { get; set; }

        public float Value { get; set; }

        public override bool Contains(Point point)
        {
            Rect rect = new Rect(Point.X - CONTAIN_RADIUS, Point.Y - CONTAIN_RADIUS, 2 * CONTAIN_RADIUS + 1, 2 * CONTAIN_RADIUS + 1);
            return rect.Contains(point);
        }

        public override void BeginMove(Point location, int index)
        {
            base.BeginMove(location, index);

            switch (MovingIndex)
            {
                case 0:
                    LocationOffset = Point - location;
                    break;
            }
        }

        public override void DoMove(Point location, Rect thermalArea)
        {
            if (!IsMoving)
            {
                return;
            }

            switch (MovingIndex)
            {
                case 0:
                    Point p = location + LocationOffset;

                    if (p.X < thermalArea.X)
                    {
                        p.X = thermalArea.X;
                    }
                    else if (p.X > thermalArea.Right)
                    {
                        p.X = thermalArea.Right;
                    }

                    if (p.Y < thermalArea.Y)
                    {
                        p.Y = thermalArea.Y;
                    }
                    else if (p.Y > thermalArea.Bottom)
                    {
                        p.Y = thermalArea.Bottom;
                    }

                    Point = p;
                    break;
            }
        }

        public override bool TrySelectPivot(Point point, out int index)
        {
            index = 0;

            if (!IsSelected)
            {
                return false;
            }

            Rect rect = new Rect(Point.X - SELECT_PIVOT_RADIUS - 1, Point.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rect.Contains(point))
            {
                index = 1;
                return true;
            }

            return false;
        }

        public override void DoRender(DrawingContext dc, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            Point point = Point;
            dc.DrawLine(MarkerThickShapePen, new Point(point.X - DRAW_RADIUS, point.Y), new Point(point.X + DRAW_RADIUS, point.Y));
            dc.DrawLine(MarkerThickShapePen, new Point(point.X, point.Y - DRAW_RADIUS), new Point(point.X, point.Y + DRAW_RADIUS));
            dc.DrawLine(MarkerThinShapePen, new Point(point.X - DRAW_RADIUS, point.Y), new Point(point.X + DRAW_RADIUS, point.Y));
            dc.DrawLine(MarkerThinShapePen, new Point(point.X, point.Y - DRAW_RADIUS), new Point(point.X, point.Y + DRAW_RADIUS));

            if (!IsMoving)
            {
                string text = Name;
                
                if (renderParams.ShowMax || renderParams.ShowMin || renderParams.ShowAvg)
                {
                    text += "\r\n" + new Amur.Common.Temperature(Value, Amur.Common.TemperatureUnit.Kelvin).ToString();
                }

                if (!string.IsNullOrEmpty(text))
                {
                    DrawSpotTitleText(dc, text, point, renderParams, parentRectangle, ref subRectangles);
                }
            }

            if (IsSelected)
            {
                dc.DrawRectangle(null, MarkerHightlightPen, new Rect(Point.X - SELECT_PIVOT_RADIUS - 1, Point.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1));
            }
        }

        private static void DrawSpotTitleText(DrawingContext dc, string text, Point point, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            int bigRadius = DRAW_RADIUS;
            int smallRadius = (int)(0.7F * DRAW_RADIUS);
            FormattedText ft = ThermalCommon.GetFormattedText(text, renderParams.FontParams);

            if (renderParams.AutoPosition)
            {
                Position[] positions = new Position[]
                {
                    new Position(new Point(point.X, point.Y - bigRadius), HorizontalAlignment.Center, VerticalAlignment.Bottom),
                    new Position(new Point(point.X + bigRadius, point.Y), HorizontalAlignment.Left, VerticalAlignment.Center),
                    new Position(new Point(point.X, point.Y + bigRadius), HorizontalAlignment.Center, VerticalAlignment.Top),
                    new Position(new Point(point.X - bigRadius, point.Y), HorizontalAlignment.Right, VerticalAlignment.Center),
                    new Position(new Point(point.X + smallRadius, point.Y - smallRadius), HorizontalAlignment.Left, VerticalAlignment.Bottom),
                    new Position(new Point(point.X + smallRadius, point.Y + smallRadius), HorizontalAlignment.Left, VerticalAlignment.Top),
                    new Position(new Point(point.X - smallRadius, point.Y + smallRadius), HorizontalAlignment.Right, VerticalAlignment.Top),
                    new Position(new Point(point.X - smallRadius, point.Y - smallRadius), HorizontalAlignment.Right, VerticalAlignment.Bottom)
                };

                DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), positions, parentRectangle, ref subRectangles);
            }
            else
            {
                Position position = new Position(new Point(point.X, point.Y - bigRadius), HorizontalAlignment.Center, VerticalAlignment.Bottom);

                ThermalCommon.DrawOutlinedText(dc, ft, ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), position);
            }
        }
    }
}
