using Firefly.Client.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalMarkerPolygon : ThermalMarker
    {
        private Point[] points;

        public ThermalMarkerPolygon(string name)
            : base(name)
        {
            Points = new Point[0];
            MinPoint = new Point(0.0, 0.0);
            MinValue = 293.15F;
            MaxPoint = new Point(0.0, 0.0);
            MaxValue = 293.15F;
            AvgValue = 293.15F;
        }

        public override ThermalMarkerType Type
        {
            get { return ThermalMarkerType.Polygon; }
        }

        public Point[] Points
        {
            get
            {
                if (points == null)
                {
                    return null;
                }

                return points.Clone() as Point[];
            }
            set
            {
                if (value == null)
                {
                    points = null;
                }

                points = value.Clone() as Point[];
            }
        }

        public Point MinPoint { get; set; }

        public float MinValue { get; set; }

        public Point MaxPoint { get; set; }

        public float MaxValue { get; set; }

        public float AvgValue { get; set; }

        public override bool Contains(Point point)
        {
            return IsVisiblePointInPolyngon(points, point);
        }

        public override void BeginMove(Point location, int index)
        {
            base.BeginMove(location, index);

            switch (MovingIndex)
            {
                case 0:
                    LocationOffset = points[0] - location;
                    break;
                default:
                    if (MovingIndex > 0 && MovingIndex <= points.Length)
                    {
                        LocationOffset = points[MovingIndex - 1] - location;
                    }
                    break;
            }
        }

        public override void DoMove(Point location, Rect area)
        {
            if (!IsMoving)
            {
                return;
            }

            Point[] ps = null;
            switch (MovingIndex)
            {
                case 0:
                    double newOffsetX = 0;
                    double newOffsetY = 0;
                    ps = points.Clone() as Point[];
                    Point p = location + LocationOffset;
                    Vector offset = p - ps[0];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        ps[i] = ps[i] + offset;
                        if (ps[i].X + newOffsetX < area.X)
                        {
                            newOffsetX = area.X - ps[i].X;
                        }
                        if (ps[i].Y + newOffsetY < area.Y)
                        {
                            newOffsetY = area.Y - ps[i].Y;
                        }
                        if (ps[i].X + newOffsetX > area.Right)
                        {
                            newOffsetX = area.Right - ps[i].X;
                        }
                        if (ps[i].Y + newOffsetY > area.Bottom)
                        {
                            newOffsetY = area.Bottom - ps[i].Y;
                        }
                    }

                    if (newOffsetX != 0 || newOffsetY != 0)
                    {
                        for (int i = 0; i < ps.Length; i++)
                        {
                            ps[i].X = ps[i].X + newOffsetX;
                            ps[i].Y = ps[i].Y + newOffsetY;
                        }
                    }

                    points = ps;
                    break;
                default:
                    if (MovingIndex > 0 && MovingIndex <= points.Length)
                    {
                        int index = MovingIndex - 1;
                        ps = points.Clone() as Point[];
                        ps[index] = location + LocationOffset;
                        if (ps[index].X < area.X)
                        {
                            ps[index].X = area.X;
                        }
                        else if (ps[index].X > area.Right)
                        {
                            ps[index].X = area.Right;
                        }
                        if (ps[index].Y < area.Y)
                        {
                            ps[index].Y = area.Y;
                        }
                        else if (ps[index].Y > area.Bottom)
                        {
                            ps[index].Y = area.Bottom;
                        }
                        points = ps;
                    }
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

            for (int i = 0; i < points.Length; i++)
            {
                Point p = points[i];
                Rect r = new Rect(p.X - SELECT_PIVOT_RADIUS - 1, p.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
                if (r.Contains(point))
                {
                    index = i + 1;
                    return true;
                }
            }

            return false;
        }

        public override void DoRender(DrawingContext dc, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {
            if (points == null || points.Length < 3)
            {
                return;
            }

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, true);
                for (int i = 0; i < points.Length; i++)
                {
                    ctx.LineTo(points[i], true, false);
                }
            }

            dc.DrawGeometry(null, MarkerThickShapePen, geometry);
            dc.DrawGeometry(null, MarkerThinShapePen, geometry);

            if (!IsMoving)
            {
                string text = Name;
                string prefix = "\r\n";

                if (renderParams.ShowMaxPoint)
                {
                    Geometry tmpGeometry = GetHighBadgePath(MaxPoint);
                    dc.DrawGeometry(HotBadgeBrush, BadgeBorder, tmpGeometry);

                    if (renderParams.FollowMaxMinPoint && renderParams.ShowMax)
                    {
                        DrawMarkerMaxText(dc, new Amur.Common.Temperature(MaxValue, Amur.Common.TemperatureUnit.Kelvin).ToString(), MaxPoint, renderParams, parentRectangle, ref subRectangles);
                    }
                }

                if (renderParams.ShowMinPoint)
                {
                    Geometry tmpGeometry = GetLowBadgePath(MinPoint);
                    dc.DrawGeometry(ColdBadgeBrush, BadgeBorder, tmpGeometry);

                    if (renderParams.FollowMaxMinPoint && renderParams.ShowMin)
                    {
                        DrawMarkerMinText(dc, new Amur.Common.Temperature(MinValue, Amur.Common.TemperatureUnit.Kelvin).ToString(), MinPoint, renderParams, parentRectangle, ref subRectangles);
                    }
                }

                if (!renderParams.FollowMaxMinPoint)
                {
                    if (renderParams.ShowMax)
                    {
                        text += prefix + I18n.Properties.Resources.MaxColon + new Amur.Common.Temperature(MaxValue, Amur.Common.TemperatureUnit.Kelvin).ToString();
                        prefix = "\r\n";
                    }

                    if (renderParams.ShowMin)
                    {
                        text += prefix + I18n.Properties.Resources.MinColon + new Amur.Common.Temperature(MinValue, Amur.Common.TemperatureUnit.Kelvin).ToString();
                        prefix = "\r\n";
                    }
                }

                if (renderParams.ShowAvg)
                {
                    text += prefix + I18n.Properties.Resources.AvgColon + new Amur.Common.Temperature(AvgValue, Amur.Common.TemperatureUnit.Kelvin).ToString();
                    prefix = "\r\n";
                }

                if (renderParams.ShowDiff)
                {
                    text += prefix + I18n.Properties.Resources.DiffColon + (new Amur.Common.Temperature(MaxValue, Amur.Common.TemperatureUnit.Kelvin) - new Amur.Common.Temperature(MinValue, Amur.Common.TemperatureUnit.Kelvin)).ToString();
                }

                if (!string.IsNullOrEmpty(text))
                {
                    DrawMarkerTitleText(dc, text, points[0], renderParams, parentRectangle, ref subRectangles);
                }
            }

            if (IsSelected)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Point p = points[i];
                    dc.DrawRectangle(null, MarkerHightlightPen, new Rect(p.X - SELECT_PIVOT_RADIUS - 1, p.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1));
                }
            }
        }

        private static bool IsVisiblePointInPolyngon(Point[] points, Point point)
        {
            if (points == null || points.Length < 3)
            {
                return false;
            }

            int j = points.Length - 1;
            bool oddNodes = false;

            Point tPoint = new Point(point.X, point.Y);
            for (int i = 0; i < points.Length; i++)
            {
                Point sPoint = points[i];
                Point ePoint = points[j];
                if ((sPoint.Y < tPoint.Y && ePoint.Y >= tPoint.Y || ePoint.Y < tPoint.Y && sPoint.Y >= tPoint.Y) && (sPoint.X <= tPoint.X || ePoint.X <= tPoint.X))
                {
                    oddNodes ^= (sPoint.X + (tPoint.Y - sPoint.Y) / (ePoint.Y - sPoint.Y) * (ePoint.X - sPoint.X) < tPoint.X);
                }
                j = i;
            }

            return oddNodes;
        }
    }
}
