using Firefly.Client.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalMarkerRectangle : ThermalMarker
    {
        public const int MIN_WIDTH = 4;
        public const int MIN_HEIGHT = 4;

        public ThermalMarkerRectangle(string name)
            : base(name)
        {
            Rectangle = new Rect(0.0, 0.0, 0.0, 0.0);
            MinPoint = new Point(0.0, 0.0);
            MinValue = 293.15F;
            MaxPoint = new Point(0.0, 0.0);
            MaxValue = 293.15F;
            AvgValue = 293.15F;
        }

        public override ThermalMarkerType Type
        {
            get { return ThermalMarkerType.Rectangle; }
        }

        public Rect Rectangle { get; set; }

        public Point MinPoint { get; set; }

        public float MinValue { get; set; }

        public Point MaxPoint { get; set; }

        public float MaxValue { get; set; }

        public float AvgValue { get; set; }

        public override bool Contains(Point point)
        {
            return Rectangle.Contains(point);
        }

        public override void BeginMove(Point location, int index)
        {
            base.BeginMove(location, index);

            switch (MovingIndex)
            {
                case 0:
                    LocationOffset = Rectangle.TopLeft - location;
                    break;
                case 1:
                    LocationOffset = Rectangle.TopLeft - location;
                    break;
                case 2:
                    LocationOffset = Rectangle.TopLeft - location;
                    break;
                case 3:
                    LocationOffset = Rectangle.TopRight - location;
                    break;
                case 4:
                    LocationOffset = Rectangle.TopRight - location;
                    break;
                case 5:
                    LocationOffset = Rectangle.BottomRight - location;
                    break;
                case 6:
                    LocationOffset = Rectangle.BottomRight - location;
                    break;
                case 7:
                    LocationOffset = Rectangle.BottomLeft - location;
                    break;
                case 8:
                    LocationOffset = Rectangle.BottomLeft - location;
                    break;
            }
        }

        public override void DoMove(Point location, Rect area)
        {
            if (!IsMoving)
            {
                return;
            }

            Point newLocation = new Point(0.0, 0.0);
            Size newSize = new Size(0.0, 0.0);
            double newWidth = 0.0F;
            double newHeight = 0.0F;
            Rect newRectangle = new Rect(0.0, 0.0, 0.0, 0.0);
            switch (MovingIndex)
            {
                case 0:
                    newRectangle = new Rect(location + LocationOffset, Rectangle.Size);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                    }
                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.X = area.Right - Rectangle.Width;
                    }

                    if (newRectangle.Y < area.Y)
                    {
                        newRectangle.Y = area.Y;
                    }
                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Y = area.Bottom - Rectangle.Height;
                    }
                    break;
                case 1:
                    newLocation = location + LocationOffset;
                    newWidth = Rectangle.Right - newLocation.X;
                    newHeight = Rectangle.Bottom - newLocation.Y;
                    if (newWidth < MIN_WIDTH)
                    {
                        newLocation.X = Rectangle.Right - MIN_WIDTH;
                        newWidth = MIN_WIDTH;
                    }
                    if (newHeight < MIN_HEIGHT)
                    {
                        newLocation.Y = Rectangle.Bottom - MIN_HEIGHT;
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(newWidth, newHeight);
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                        newRectangle.Width = Rectangle.Right - newRectangle.X;
                    }

                    if (newRectangle.Y < 0)
                    {
                        newRectangle.Y = 0;
                        newRectangle.Height = Rectangle.Bottom - newRectangle.Y;
                    }
                    break;
                case 2:
                    newLocation = new Point(Rectangle.X, location.Y + LocationOffset.Y);
                    newHeight = Rectangle.Bottom - newLocation.Y;
                    if (newHeight < MIN_HEIGHT)
                    {
                        newLocation.Y = Rectangle.Bottom - MIN_HEIGHT;
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(Rectangle.Width, newHeight);
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.Y < area.Y)
                    {
                        newRectangle.Y = area.Y;
                        newRectangle.Height = Rectangle.Bottom - newRectangle.Y;
                    }
                    break;
                case 3:
                    newLocation = new Point(Rectangle.X, location.Y + LocationOffset.Y);
                    newWidth = location.X + LocationOffset.X - Rectangle.X;
                    newHeight = Rectangle.Bottom - newLocation.Y;
                    if (newWidth < MIN_WIDTH)
                    {
                        newWidth = MIN_WIDTH;
                    }
                    if (newHeight < MIN_HEIGHT)
                    {
                        newLocation.Y = Rectangle.Bottom - MIN_HEIGHT;
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(newWidth, newHeight);
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.Width = area.Right - Rectangle.X;
                    }

                    if (newRectangle.Y < area.Y)
                    {
                        newRectangle.Y = area.Y;
                        newRectangle.Height = Rectangle.Bottom - newRectangle.Y;
                    }
                    break;
                case 4:
                    newWidth = location.X + LocationOffset.X - Rectangle.X;
                    if (newWidth < MIN_WIDTH)
                    {
                        newWidth = MIN_WIDTH;
                    }
                    newSize = new Size(newWidth, Rectangle.Height);
                    newRectangle = new Rect(Rectangle.Location, newSize);

                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.Width = area.Right - Rectangle.X;
                    }
                    break;
                case 5:
                    newWidth = location.X + LocationOffset.X - Rectangle.X;
                    newHeight = location.Y + LocationOffset.Y - Rectangle.Y;
                    if (newWidth < MIN_WIDTH)
                    {
                        newWidth = MIN_WIDTH;
                    }
                    if (newHeight < MIN_HEIGHT)
                    {
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(newWidth, newHeight);
                    newRectangle = new Rect(Rectangle.Location, newSize);

                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.Width = area.Right - Rectangle.X;
                    }

                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Height = area.Bottom - Rectangle.Y;
                    }
                    break;
                case 6:
                    newHeight = location.Y + LocationOffset.Y - Rectangle.Y;
                    if (newHeight < MIN_HEIGHT)
                    {
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(Rectangle.Width, newHeight);
                    newRectangle = new Rect(Rectangle.Location, newSize);

                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Height = area.Bottom - Rectangle.Y;
                    }
                    break;
                case 7:
                    newLocation = new Point(location.X + LocationOffset.X, Rectangle.Y);
                    newWidth = Rectangle.Right - newLocation.X;
                    newHeight = location.Y + LocationOffset.Y - Rectangle.Y;
                    if (newWidth < MIN_WIDTH)
                    {
                        newLocation.X = Rectangle.Right - MIN_WIDTH;
                        newWidth = MIN_WIDTH;
                    }
                    if (newHeight < MIN_HEIGHT)
                    {
                        newHeight = MIN_HEIGHT;
                    }
                    newSize = new Size(newWidth, newHeight);
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                        newRectangle.Width = Rectangle.Right - newRectangle.X;
                    }

                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Height = area.Bottom - Rectangle.Y;
                    }
                    break;
                case 8:
                    newLocation = new Point(location.X + LocationOffset.X, Rectangle.Y);
                    newWidth = Rectangle.Right - newLocation.X;
                    if (newWidth < MIN_WIDTH)
                    {
                        newLocation.X = Rectangle.Right - MIN_WIDTH;
                        newWidth = MIN_WIDTH;
                    }
                    newSize = new Size(newWidth, Rectangle.Height);
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                        newRectangle.Width = Rectangle.Right - newRectangle.X;
                    }
                    break;
            }

            Rectangle = newRectangle;
        }

        public override bool TrySelectPivot(Point point, out int index)
        {
            index = 0;

            if (!IsSelected)
            {
                return false;
            }

            Point pTopLeft = Rectangle.TopLeft;
            Point pBottomRight = Rectangle.BottomRight;

            Rect rLeftTop = new Rect(pTopLeft.X - SELECT_PIVOT_RADIUS - 1, pTopLeft.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rLeftTop.Contains(point))
            {
                index = 1;
                return true;
            }

            Point pTop = new Point((pTopLeft.X + pBottomRight.X + 1) / 2, pTopLeft.Y);
            Rect rTop = new Rect(pTop.X - SELECT_PIVOT_RADIUS - 1, pTop.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rTop.Contains(point))
            {
                index = 2;
                return true;
            }

            Point pRightTop = new Point(pBottomRight.X, pTopLeft.Y);
            Rect rRightTop = new Rect(pRightTop.X - SELECT_PIVOT_RADIUS - 1, pRightTop.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rRightTop.Contains(point))
            {
                index = 3;
                return true;
            }

            Point pRight = new Point(pBottomRight.X, (pTopLeft.Y + pBottomRight.Y + 1) / 2);
            Rect rRight = new Rect(pRight.X - SELECT_PIVOT_RADIUS - 1, pRight.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rRight.Contains(point))
            {
                index = 4;
                return true;
            }

            Rect rRightBottom = new Rect(pBottomRight.X - SELECT_PIVOT_RADIUS - 1, pBottomRight.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rRightBottom.Contains(point))
            {
                index = 5;
                return true;
            }

            Point pBottom = new Point((pTopLeft.X + pBottomRight.X + 1) / 2, pBottomRight.Y);
            Rect rBottom = new Rect(pBottom.X - SELECT_PIVOT_RADIUS - 1, pBottom.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rBottom.Contains(point))
            {
                index = 6;
                return true;
            }

            Point pLeftBottom = new Point(pTopLeft.X, pBottomRight.Y);
            Rect rLeftBottom = new Rect(pLeftBottom.X - SELECT_PIVOT_RADIUS - 1, pLeftBottom.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rLeftBottom.Contains(point))
            {
                index = 7;
                return true;
            }

            Point pLeft = new Point(pTopLeft.X, (pTopLeft.Y + pBottomRight.Y + 1) / 2);
            Rect rLeft = new Rect(pLeft.X - SELECT_PIVOT_RADIUS - 1, pLeft.Y - SELECT_PIVOT_RADIUS - 1, 2 * SELECT_PIVOT_RADIUS + 1, 2 * SELECT_PIVOT_RADIUS + 1);
            if (rLeft.Contains(point))
            {
                index = 8;
                return true;
            }

            return false;
        }

        public override void DoRender(DrawingContext dc, RenderParams renderParams, Rect parentRectangle, ref List<Rect> subRectangles)
        {

            Rect rectangle = new Rect(Rectangle.X, Rectangle.Y, Rectangle.Width, Rectangle.Height);
            dc.DrawRectangle(null, MarkerThickShapePen, rectangle);
            dc.DrawRectangle(null, MarkerThinShapePen, rectangle);

            if (!IsMoving)
            {
                string text = string.Empty;
                string prefix = string.Empty;
                text = Name;
                prefix = "\r\n";

                if (renderParams.ShowMaxPoint)
                {
                    Geometry geometry = GetHighBadgePath(MaxPoint);
                    dc.DrawGeometry(HotBadgeBrush, BadgeBorder, geometry);

                    if (renderParams.FollowMaxMinPoint && renderParams.ShowMax)
                    {
                        DrawMarkerMaxText(dc, new Amur.Common.Temperature(MaxValue, Amur.Common.TemperatureUnit.Kelvin).ToString(), MaxPoint, renderParams, parentRectangle, ref subRectangles);
                    }
                }

                if (renderParams.ShowMinPoint)
                {
                    Geometry geometry = GetLowBadgePath(MinPoint);
                    dc.DrawGeometry(ColdBadgeBrush, BadgeBorder, geometry);

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
                    DrawMarkerTitleText(dc, text, Rectangle.Location, renderParams, parentRectangle, ref subRectangles);
                }
            }

            if (IsSelected)
            {
                Point pTopLeft = Rectangle.TopLeft;
                Point pBottomRight = Rectangle.BottomRight;
                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(pTopLeft.X - SELECT_PIVOT_RADIUS - 1, pTopLeft.Y + SELECT_PIVOT_RADIUS), false, false);
                    ctx.LineTo(new Point(pTopLeft.X - SELECT_PIVOT_RADIUS - 1, pTopLeft.Y - SELECT_PIVOT_RADIUS - 1), true, false);
                    ctx.LineTo(new Point(pTopLeft.X + SELECT_PIVOT_RADIUS, pTopLeft.Y - SELECT_PIVOT_RADIUS - 1), true, false);
                }
                dc.DrawGeometry(null, MarkerHightlightPen, geometry);

                Point pTop = new Point((pTopLeft.X + pBottomRight.X + 1) / 2, pTopLeft.Y);
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pTop.X - SELECT_PIVOT_RADIUS - 1, pTop.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pTop.X + SELECT_PIVOT_RADIUS, pTop.Y - SELECT_PIVOT_RADIUS - 1));
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pTop.X - SELECT_PIVOT_RADIUS - 1, pTop.Y + SELECT_PIVOT_RADIUS),
                            new Point(pTop.X + SELECT_PIVOT_RADIUS, pTop.Y + SELECT_PIVOT_RADIUS));

                Point pRightTop = new Point(pBottomRight.X, pTopLeft.Y);
                geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(pRightTop.X - SELECT_PIVOT_RADIUS - 1, pRightTop.Y - SELECT_PIVOT_RADIUS - 1), false, false);
                    ctx.LineTo(new Point(pRightTop.X + SELECT_PIVOT_RADIUS, pRightTop.Y - SELECT_PIVOT_RADIUS - 1), true, false);
                    ctx.LineTo(new Point(pRightTop.X + SELECT_PIVOT_RADIUS, pRightTop.Y + SELECT_PIVOT_RADIUS), true, false);
                }
                dc.DrawGeometry(null, MarkerHightlightPen, geometry);

                Point pRight = new Point(pBottomRight.X, (pTopLeft.Y + pBottomRight.Y + 1) / 2);
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pRight.X - SELECT_PIVOT_RADIUS - 1, pRight.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pRight.X - SELECT_PIVOT_RADIUS - 1, pRight.Y + SELECT_PIVOT_RADIUS));
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pRight.X + SELECT_PIVOT_RADIUS, pRight.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pRight.X + SELECT_PIVOT_RADIUS, pRight.Y + SELECT_PIVOT_RADIUS));

                geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(pBottomRight.X - SELECT_PIVOT_RADIUS - 1, pBottomRight.Y + SELECT_PIVOT_RADIUS), false, false);
                    ctx.LineTo(new Point(pBottomRight.X + SELECT_PIVOT_RADIUS, pBottomRight.Y + SELECT_PIVOT_RADIUS), true, false);
                    ctx.LineTo(new Point(pBottomRight.X + SELECT_PIVOT_RADIUS, pBottomRight.Y - SELECT_PIVOT_RADIUS - 1), true, false);
                }
                dc.DrawGeometry(null, MarkerHightlightPen, geometry);

                Point pBottom = new Point((pTopLeft.X + pBottomRight.X + 1) / 2, pBottomRight.Y);
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pBottom.X - SELECT_PIVOT_RADIUS - 1, pBottom.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pBottom.X + SELECT_PIVOT_RADIUS, pBottom.Y - SELECT_PIVOT_RADIUS - 1));
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pBottom.X - SELECT_PIVOT_RADIUS - 1, pBottom.Y + SELECT_PIVOT_RADIUS),
                            new Point(pBottom.X + SELECT_PIVOT_RADIUS, pBottom.Y + SELECT_PIVOT_RADIUS));

                Point pLeftBottom = new Point(pTopLeft.X, pBottomRight.Y);
                geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(pLeftBottom.X - SELECT_PIVOT_RADIUS - 1, pLeftBottom.Y - SELECT_PIVOT_RADIUS - 1), false, false);
                    ctx.LineTo(new Point(pLeftBottom.X - SELECT_PIVOT_RADIUS - 1, pLeftBottom.Y + SELECT_PIVOT_RADIUS), true, false);
                    ctx.LineTo(new Point(pLeftBottom.X + SELECT_PIVOT_RADIUS, pLeftBottom.Y + SELECT_PIVOT_RADIUS), true, false);
                }
                dc.DrawGeometry(null, MarkerHightlightPen, geometry);

                Point pLeft = new Point(pTopLeft.X, (pTopLeft.Y + pBottomRight.Y + 1) / 2);
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pLeft.X - SELECT_PIVOT_RADIUS - 1, pLeft.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pLeft.X - SELECT_PIVOT_RADIUS - 1, pLeft.Y + SELECT_PIVOT_RADIUS));
                dc.DrawLine(MarkerHightlightPen,
                            new Point(pLeft.X + SELECT_PIVOT_RADIUS, pLeft.Y - SELECT_PIVOT_RADIUS - 1),
                            new Point(pLeft.X + SELECT_PIVOT_RADIUS, pLeft.Y + SELECT_PIVOT_RADIUS));
            }
        }

        private static void DrawGlobalTitleText(DrawingContext dc, string text, Point point, RenderParams renderParams, Rect parentRectangle)
        {
            Position position = new Position(point, HorizontalAlignment.Left, VerticalAlignment.Top);
            ThermalCommon.DrawOutlinedText(dc, ThermalCommon.GetFormattedText(text, renderParams.FontParams), ThermalCommon.GetOutlinedTextBrush(), ThermalCommon.GetOutlinedTextStrokePen(renderParams.FontParams.FontSize), position);
        }
    }
}
