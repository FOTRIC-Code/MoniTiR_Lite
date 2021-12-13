// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Windows;
using System.Globalization;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public enum PaletteSideMode
    {
        Inside = 1,
        Outside = 2,
    }

    public struct RenderFontParams
    {
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontFamily FontFamily { get; set; }
        public double FontSize { get; set; }
    }

    public struct Position
    {
        public static Position Empty = new Position(new Point(0.0, 0.0), HorizontalAlignment.Left, VerticalAlignment.Top);

        public Position(Point point, HorizontalAlignment hAlignment, VerticalAlignment vAlignment)
        {
            Point = point;
            HAlignment = hAlignment;
            VAlignment = vAlignment;
        }

        public Point Point { get; set; }
        public HorizontalAlignment HAlignment { get; set; }
        public VerticalAlignment VAlignment { get; set; }
    }

    public sealed class ThermalCommon
    {
        private readonly static SolidColorBrush OutlinedTextBrush = new SolidColorBrush(Colors.White);
        private readonly static Pen OutlinedTextStrokeThinPen = new Pen(new SolidColorBrush(Colors.Black), 0.7);
        private readonly static Pen OutlinedTextStrokeThickPen = new Pen(new SolidColorBrush(Colors.Black), 1.0);

        public static FormattedText GetFormattedText(string text, RenderFontParams renderFontParams)
        {
            FontFamily font = renderFontParams.FontFamily;
            double fontSize = renderFontParams.FontSize;
            FontStyle fontStyle = renderFontParams.FontStyle;
            FontWeight fontWeight = renderFontParams.FontWeight;

            FormattedText ft = new FormattedText(
                text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(font, fontStyle, fontWeight, FontStretches.Normal),
                fontSize, Brushes.Black);

            return ft;
        }

        public static Brush GetOutlinedTextBrush()
        {
            return OutlinedTextBrush;
        }

        public static Pen GetOutlinedTextStrokePen(double fontSize)
        {
            if (fontSize <= 14.0)
            {
                return OutlinedTextStrokeThinPen;
            }
            else if (fontSize >= 18.0)
            {
                return OutlinedTextStrokeThickPen;
            }
            else
            {
                return new Pen(new SolidColorBrush(Colors.Black), (fontSize - 14.0) * 0.3 / 4.0 + 0.7);
            }
        }

        public static void DrawOutlinedText(DrawingContext dc, FormattedText ft, Brush brush, Pen pen, Position position)
        {
            Point point = position.Point;
            HorizontalAlignment hAlignment = position.HAlignment;
            VerticalAlignment vAlignment = position.VAlignment;

            switch (hAlignment)
            {
                case HorizontalAlignment.Center:
                    point.X -= ft.Width / 2;
                    break;
                case HorizontalAlignment.Right:
                    point.X -= ft.Width;
                    break;
            }

            switch (vAlignment)
            {
                case VerticalAlignment.Center:
                    point.Y -= ft.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    point.Y -= ft.Height;
                    break;
            }

            Geometry tg = ft.BuildGeometry(point);

            dc.DrawGeometry(brush, pen, tg);
        }

        public static System.Drawing.Point ToPoint(Point srcPoint, Rect srcArea, System.Drawing.Size destSize)
        {
            return new System.Drawing.Point((int)((srcPoint.X - srcArea.X) * (destSize.Width - 1) / srcArea.Width + 0.5), (int)((srcPoint.Y - srcArea.Y) * (destSize.Height - 1) / srcArea.Height + 0.5));
        }

        //Covert absolute point to relative point in display coordinate system
        public static Point AbsToRelPoint(Point point, Rect area)
        {
            return new Point((point.X - area.X) / area.Width, (point.Y - area.Y) / area.Height);
        }

        public static Point[] AbsToRelPoints(Point[] points, Rect area)
        {
            Point[] destPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                destPoints[i] = AbsToRelPoint(points[i], area);
            }
            return destPoints;
        }

        //Covert relative point to absolute point in display coordinate system
        public static Point RelToAbsPoint(Point point, Rect area)
        {
            return new Point(area.X + point.X * area.Width, area.Y + point.Y * area.Height);
        }

        //Covert relative point to absolute point in display coordinate system
        public static Point[] RelToAbsPoints(Point[] points, Rect area)
        {
            Point[] destPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                destPoints[i] = RelToAbsPoint(points[i], area);
            }
            return destPoints;
        }

        //Covert relative point from display coordinate system to image coordinate system
        public static System.Drawing.Point RelToAbsPoint(Point point, System.Drawing.Size size)
        {
            return new System.Drawing.Point((int)(point.X * (size.Width - 1) + 0.5), (int)(point.Y * (size.Height - 1) + 0.5));
        }

        //Covert relative point from display coordinate system to image coordinate system
        public static System.Drawing.Rectangle RelToAbsRectangle(Rect rect, System.Drawing.Size size)
        {
            return new System.Drawing.Rectangle(RelToAbsPoint(rect.Location, size), new System.Drawing.Size((int)(rect.Width * (size.Width - 1) + 0.5), (int)(rect.Height * (size.Height - 1) + 0.5)));
        }

        //Covert relative point from display coordinate system to image coordinate system
        public static System.Drawing.Point[] RelToAbsPoints(Point[] points, System.Drawing.Size size)
        {
            System.Drawing.Point[] destPoints = new System.Drawing.Point[points.Length];
            for (int i = 0; i < destPoints.Length; i++)
            {
                destPoints[i] = RelToAbsPoint(points[i], size);
            }
            return destPoints;
        }

        public static Point GetNearestPointInArea(Point point, Rect area)
        {
            return new Point(point.X < area.X ? area.X : (point.X > area.Right ? area.Right : point.X),
                             point.Y < area.Y ? area.Y : (point.Y > area.Bottom ? area.Bottom : point.Y));
        }
    }
}
