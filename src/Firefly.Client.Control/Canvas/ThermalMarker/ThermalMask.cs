using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalMask : ThermalMarker
    {
        private Point[] _points;

        public ThermalMask(string name)
            : base(name)
        {
            Points = new Point[0];
        }

        public override ThermalMarkerType Type
        {
            get { return ThermalMarkerType.Mask; }
        }

        public Point[] Points
        {
            get
            {
                if (_points == null)
                {
                    return null;
                }

                return _points.Clone() as Point[];
            }
            set
            {
                if (value == null)
                {
                    _points = null;
                }

                _points = value.Clone() as Point[];
            }
        }

        public override bool Contains(Point point)
        {
            return IsVisiblePointInPolyngon(_points, point);
        }

        public override void BeginMove(Point location, int index)
        {
            base.BeginMove(location, index);

            switch (MovingIndex)
            {
                case 0:
                    LocationOffset = _points[0] - location;
                    break;
                default:
                    if (MovingIndex > 0 && MovingIndex <= _points.Length)
                    {
                        LocationOffset = _points[MovingIndex - 1] - location;
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
                    ps = _points.Clone() as Point[];
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

                    _points = ps;
                    break;
                default:
                    if (MovingIndex > 0 && MovingIndex <= _points.Length)
                    {
                        int index = MovingIndex - 1;
                        ps = _points.Clone() as Point[];
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
                        _points = ps;
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

            for (int i = 0; i < _points.Length; i++)
            {
                Point p = _points[i];
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
            if (_points == null || _points.Length < 3)
            {
                return;
            }

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(_points[0], false, true);
                for (int i = 0; i < _points.Length; i++)
                {
                    ctx.LineTo(_points[i], true, false);
                }
            }

            dc.DrawGeometry(null, MaskThickShapePen, geometry);
            dc.DrawGeometry(null, MarkerThinShapePen, geometry);

            if (!IsMoving)
            {
                string text = Name;
                if (!string.IsNullOrEmpty(text))
                {
                    DrawMarkerTitleText(dc, text, _points[0], renderParams, parentRectangle, ref subRectangles);
                }
            }

            if (IsSelected)
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    Point p = _points[i];
                    dc.DrawRectangle(null, MarkerHightlightPen, new Rect(p.X - SELECT_PIVOT_RADIUS - 1, p.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1));
                }
            }
        }

        private static bool IsVisiblePointInPolyngon(Point[] _points, Point point)
        {
            if (_points == null || _points.Length < 3)
            {
                return false;
            }

            int j = _points.Length - 1;
            bool oddNodes = false;

            Point tPoint = new Point(point.X, point.Y);
            for (int i = 0; i < _points.Length; i++)
            {
                Point sPoint = _points[i];
                Point ePoint = _points[j];
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
