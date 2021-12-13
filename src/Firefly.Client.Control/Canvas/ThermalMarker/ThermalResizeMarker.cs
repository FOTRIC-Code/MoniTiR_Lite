using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Firefly.Client.Control
{
    public class ThermalResizeMarker
    {
        public const int DRAW_RADIUS = 8;

        public const int MIN_WIDTH = 32;
        public const int MIN_HEIGHT = 32;

        private readonly static Pen MarkerHightlightPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.0F);

        public ThermalResizeMarker()
        {
            IsMoving = false;
            MovingIndex = -1;
            LocationOffset = new Vector(0.0, 0.0);
            Rectangle = new Rect(0.0, 0.0, 0.0, 0.0);
        }

        private bool IsMoving { get; set; }

        private int MovingIndex { get; set; }

        private Vector LocationOffset { get; set; }

        public Rect Rectangle { get; set; }

        public void BeginMove(Point location, int index)
        {
            IsMoving = true;
            MovingIndex = index;

            switch (MovingIndex)
            {
                case 0:
                    LocationOffset = Rectangle.TopLeft - location;
                    break;
                case 1:
                    LocationOffset = Rectangle.TopLeft - location;
                    break;
                case 3:
                    LocationOffset = Rectangle.TopRight - location;
                    break;
                case 5:
                    LocationOffset = Rectangle.BottomRight - location;
                    break;
                case 7:
                    LocationOffset = Rectangle.BottomLeft - location;
                    break;
            }
        }

        public void DoMove(Point location, Rect area)
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

                    double width = area.Width;
                    double height = area.Height;

                    if (newSize.Width * height > newSize.Height * width)
                    {
                        newSize.Height = newSize.Width * height / width;
                        newLocation.Y = Rectangle.Bottom - newSize.Height;
                    }
                    else
                    {
                        newSize.Width = newSize.Height * width / height;
                        newLocation.X = Rectangle.Right - newSize.Width;
                    }
                    
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                        newRectangle.Width = Rectangle.Right - newRectangle.X;
                        newRectangle.Height = newRectangle.Width * height / width;
                        newRectangle.Y = Rectangle.Bottom - newRectangle.Height;
                    }

                    if (newRectangle.Y < area.Y)
                    {
                        newRectangle.Y = area.Y;
                        newRectangle.Height = Rectangle.Bottom - newRectangle.Y;
                        newRectangle.Width = newRectangle.Height * width / height;
                        newRectangle.X = Rectangle.Right - newRectangle.Width;
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

                    width = area.Width;
                    height = area.Height;

                    if (newSize.Width * height > newSize.Height * width)
                    {
                        newSize.Height = newSize.Width * height / width;
                        newLocation.Y = Rectangle.Bottom - newSize.Height;
                    }
                    else
                    {
                        newSize.Width = newSize.Height * width / height;
                    }
                    
                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.Width = area.Right - Rectangle.X;
                        newRectangle.Height = newRectangle.Width * height / width;
                        newRectangle.Y = Rectangle.Bottom - newRectangle.Height;
                    }

                    if (newRectangle.Y < area.Y)
                    {
                        newRectangle.Y = area.Y;
                        newRectangle.Height = Rectangle.Bottom - newRectangle.Y;
                        newRectangle.Width = newRectangle.Height * width / height;
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

                    width = area.Width;
                    height = area.Height;

                    if (newSize.Width * height > newSize.Height * width)
                    {
                        newSize.Height = newSize.Width * height / width;
                    }
                    else
                    {
                        newSize.Width = newSize.Height * width / height;
                    }

                    newRectangle = new Rect(Rectangle.Location, newSize);

                    if (newRectangle.Right > area.Right)
                    {
                        newRectangle.Width = area.Right - Rectangle.X;
                        newRectangle.Height = newRectangle.Width * height / width;
                    }

                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Height = area.Bottom - Rectangle.Y;
                        newRectangle.Width = newRectangle.Height * width / height;
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

                    width = area.Width;
                    height = area.Height;

                    if (newSize.Width * height > newSize.Height * width)
                    {
                        newSize.Height = newSize.Width * height / width;
                    }
                    else
                    {
                        newSize.Width = newSize.Height * width / height;
                        newLocation.X = Rectangle.Right - newSize.Width;
                    }

                    newRectangle = new Rect(newLocation, newSize);

                    if (newRectangle.X < area.X)
                    {
                        newRectangle.X = area.X;
                        newRectangle.Width = Rectangle.Right - newRectangle.X;
                        newRectangle.Height = newRectangle.Width * height / width;
                    }

                    if (newRectangle.Bottom > area.Bottom)
                    {
                        newRectangle.Height = area.Bottom - Rectangle.Y;
                        newRectangle.Width = newRectangle.Height * width / height;
                        newRectangle.X = Rectangle.Right - newRectangle.Width;
                    }
                    break;
            }

            Rectangle = newRectangle;
        }

        public void EndMove()
        {
            IsMoving = false;
            MovingIndex = -1;
            LocationOffset = new Vector(0.0, 0.0);
        }

        public bool TrySelectPivot(Point point, out int index)
        {
            index = 0;
            Point pLeftTop = new Point(Rectangle.X, Rectangle.Y);
            Point pRightBottom = new Point(Rectangle.Right, Rectangle.Bottom);

            Rect rLeftTop = new Rect(pLeftTop.X - DRAW_RADIUS - 1, pLeftTop.Y - DRAW_RADIUS - 1, 2 * DRAW_RADIUS + 1, 2 * DRAW_RADIUS + 1);
            if (rLeftTop.Contains(point))
            {
                index = 1;
                return true;
            }

            Point pRightTop = new Point(pRightBottom.X, pLeftTop.Y);
            Rect rRightTop = new Rect(pRightTop.X - DRAW_RADIUS - 1, pRightTop.Y - DRAW_RADIUS - 1, 2 * DRAW_RADIUS + 1, 2 * DRAW_RADIUS + 1);
            if (rRightTop.Contains(point))
            {
                index = 3;
                return true;
            }

            Rect rRightBottom = new Rect(pRightBottom.X - DRAW_RADIUS - 1, pRightBottom.Y - DRAW_RADIUS - 1, 2 * DRAW_RADIUS + 1, 2 * DRAW_RADIUS + 1);
            if (rRightBottom.Contains(point))
            {
                index = 5;
                return true;
            }

            Point pLeftBottom = new Point(pLeftTop.X, pRightBottom.Y);
            Rect rLeftBottom = new Rect(pLeftBottom.X - DRAW_RADIUS - 1, pLeftBottom.Y - DRAW_RADIUS - 1, 2 * DRAW_RADIUS + 1, 2 * DRAW_RADIUS + 1);
            if (rLeftBottom.Contains(point))
            {
                index = 7;
                return true;
            }

            if (Rectangle.Contains(point))
            {
                index = 0;
                return true;
            }

            return false;
        }

        public void DoRender(DrawingContext dc)
        {
            Point pTopLeft = Rectangle.TopLeft;
            Point pTopRight = Rectangle.TopRight;
            Point pBottomLeft = Rectangle.BottomLeft;
            Point pBottomRight = Rectangle.BottomRight;

            dc.DrawLine(MarkerHightlightPen, new Point(pTopLeft.X - DRAW_RADIUS - 1, pTopLeft.Y), new Point(pTopRight.X + DRAW_RADIUS + 1, pTopRight.Y));
            dc.DrawLine(MarkerHightlightPen, new Point(pTopRight.X, pTopRight.Y - DRAW_RADIUS - 1), new Point(pBottomRight.X, pBottomRight.Y + DRAW_RADIUS + 1));
            dc.DrawLine(MarkerHightlightPen, new Point(pBottomLeft.X - DRAW_RADIUS - 1, pBottomLeft.Y), new Point(pBottomRight.X + DRAW_RADIUS + 1, pBottomRight.Y));
            dc.DrawLine(MarkerHightlightPen, new Point(pTopLeft.X, pTopLeft.Y - DRAW_RADIUS - 1), new Point(pBottomLeft.X, pBottomLeft.Y + DRAW_RADIUS + 1));
        }        
    }
}
