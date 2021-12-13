using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace Firefly.Client.Control
{
    public class ThermalMarkerCanvas : System.Windows.Controls.Canvas
    {
        private List<ThermalMarker> _markerList;

        private Rect _clientArea;
        private Rect _thermalArea;


        public const int MIN_WIDTH = 4;
        public const int MIN_HEIGHT = 4;

        protected readonly static Pen MaskThickShapePen = new Pen(Brushes.Red, 3.0F);
        protected readonly static Pen MarkerThickShapePen = new Pen(Brushes.Black, 3.0F);
        protected readonly static Pen MarkerThinShapePen = new Pen(Brushes.White, 1.0F);

        public ThermalMarkerCanvas()
        {
            Background = Brushes.Transparent;
            MarkerList = new List<ThermalMarker>();
        }

        internal RTSPClientControl Owner { get; set; }

        internal List<ThermalMarker> MarkerList
        {
            get { return _markerList; }
            private set
            {
                if (_markerList != value)
                {
                    _markerList = value;
                    InvalidateVisual();
                }
            }
        }

        internal ThermalMarker SelectedMarker
        {
            get
            {
                if (MarkerList != null)
                {
                    for (int i = 0; i < MarkerList.Count; i++)
                    {
                        if (MarkerList[i].IsSelected)
                        {
                            return MarkerList[i];
                        }
                    }
                }
                return null;
            }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(ThermalMarkerCanvas),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ThermalMarkerCanvas),
            new FrameworkPropertyMetadata(FontWeights.Bold, FrameworkPropertyMetadataOptions.AffectsRender));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(ThermalMarkerCanvas),
            new FrameworkPropertyMetadata(new FontFamily("Arial"), FrameworkPropertyMetadataOptions.AffectsRender));

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(ThermalMarkerCanvas),
            new FrameworkPropertyMetadata(18.0, FrameworkPropertyMetadataOptions.AffectsRender));

        internal Rect ClientArea
        {
            get { return _clientArea; }
            set
            {
                if (_clientArea != value)
                {
                    _clientArea = value;
                    InvalidateVisual();
                }
            }
        }

        internal Rect ThermalArea
        {
            get { return _thermalArea; }
            set
            {
                if (_thermalArea != value)
                {
                    _thermalArea = value;
                }
            }
        }

        internal void InitMarker(List<DeviceMarker> markerList)
        {
            if (markerList != null)
            {
                foreach (DeviceMarker marker in markerList)
                {
                    if (marker != null)
                    {
                        int index = GetMarkerIndex(marker.Name, CanvasType.Marker);

                        if (!ValidateDeviceMarker(marker))
                        {
                            continue;
                        }

                        switch (marker.MarkerType)
                        {
                            case MarkerType.Spot:
                                ThermalMarkerSpot spotMarker = null;
                                Point point = ThermalCommon.RelToAbsPoint(ThermalCommon.AbsToRelPoint(marker.PointList[0], ThermalArea), ClientArea);
                                if (index == -1)
                                {
                                    spotMarker = new ThermalMarkerSpot(marker.Name);
                                    spotMarker.Point = point;
                                    MarkerList.Add(spotMarker);
                                }
                                else
                                {
                                    spotMarker = MarkerList[index] as ThermalMarkerSpot;
                                    spotMarker.Point = point;
                                    MarkerList[index] = spotMarker;
                                }
                                break;
                            case MarkerType.Line:
                                ThermalMarkerPolyline lineMarker = null;
                                Point[] points = ThermalCommon.RelToAbsPoints(ThermalCommon.AbsToRelPoints(marker.PointList.ToArray(), ThermalArea), ClientArea);
                                if (index == -1)
                                {
                                    lineMarker = new ThermalMarkerPolyline(marker.Name);
                                    lineMarker.Points = points;
                                    MarkerList.Add(lineMarker);
                                }
                                else
                                {
                                    lineMarker = MarkerList[index] as ThermalMarkerPolyline;
                                    lineMarker.Points = points;
                                    MarkerList[index] = lineMarker;
                                }
                                break;
                            case MarkerType.Region:
                                ThermalMarkerPolygon polygonMarker = null;
                                points = ThermalCommon.RelToAbsPoints(ThermalCommon.AbsToRelPoints(marker.PointList.ToArray(), ThermalArea), ClientArea);
                                if (index == -1)
                                {
                                    polygonMarker = new ThermalMarkerPolygon(marker.Name);
                                    polygonMarker.Points = points;
                                    MarkerList.Add(polygonMarker);
                                }
                                else
                                {
                                    polygonMarker = MarkerList[index] as ThermalMarkerPolygon;
                                    polygonMarker.Points = points;
                                    MarkerList[index] = polygonMarker;
                                }
                                break;
                        }
                    }
                }

                List<ThermalMarker> removeMarkerList = new List<ThermalMarker>();
                foreach (ThermalMarker thermalMarker in MarkerList)
                {
                    if (thermalMarker.Type == ThermalMarkerType.Spot || thermalMarker.Type == ThermalMarkerType.Polyline || thermalMarker.Type == ThermalMarkerType.Polygon)
                    {
                        DeviceMarker marker = markerList.Where<DeviceMarker>(x => x.Name == thermalMarker.Name).FirstOrDefault();
                        if (marker == null)
                        {
                            removeMarkerList.Add(thermalMarker);
                        }
                    }

                }
                for (int i = 0; i < removeMarkerList.Count; i++)
                {
                    MarkerList.Remove(removeMarkerList[i]);
                }

                InvalidateVisual();
            }
        }

        internal void InitShieldMask(List<DeviceMarker> markerList)
        {
            if (markerList != null)
            {
                foreach (DeviceMarker marker in markerList)
                {
                    if (marker != null)
                    {
                        int index = GetMarkerIndex(marker.Name, CanvasType.Shield);

                        if (!ValidateDeviceMarker(marker))
                        {
                            continue;
                        }

                        ThermalMask mask = null;
                        Point[] points = ThermalCommon.RelToAbsPoints(ThermalCommon.AbsToRelPoints(marker.PointList.ToArray(), ThermalArea), ClientArea);
                        if (index == -1)
                        {
                            mask = new ThermalMask(marker.Name);
                            mask.Points = points;
                            MarkerList.Add(mask);
                        }
                        else
                        {
                            mask = MarkerList[index] as ThermalMask;
                            mask.Points = points;
                            MarkerList[index] = mask;
                        }
                    }
                }

                List<ThermalMarker> removeMarkerList = new List<ThermalMarker>();
                foreach (ThermalMarker thermalMarker in MarkerList)
                {
                    if (thermalMarker.Type == ThermalMarkerType.Mask)
                    {
                        DeviceMarker marker = markerList.Where<DeviceMarker>(x => x.Name == thermalMarker.Name).FirstOrDefault();
                        if (marker == null)
                        {
                            removeMarkerList.Add(thermalMarker);
                        }
                    }

                }
                for (int i = 0; i < removeMarkerList.Count; i++)
                {
                    MarkerList.Remove(removeMarkerList[i]);
                }

                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            try
            {
                GuidelineSet guidelines = new GuidelineSet(new double[] { 0.5 }, new double[] { 0.5 });
                dc.PushGuidelineSet(guidelines);
                try
                {
                    Geometry clipGeometry = new RectangleGeometry(ClientArea);
                    dc.PushClip(clipGeometry);

                    if (MarkerList != null && MarkerList.Count > 0)
                    {
                        RenderParams renderParams = new RenderParams();
                        renderParams.ShowMaxPoint = false;
                        renderParams.ShowMinPoint = false;
                        renderParams.ShowMax = false;
                        renderParams.ShowMin = false;
                        renderParams.ShowAvg = false;
                        renderParams.ShowDiff = false;
                        renderParams.FollowMaxMinPoint = false;
                        renderParams.AutoPosition = false;
                        RenderFontParams renderFontParams = new RenderFontParams();
                        renderFontParams.FontStyle = FontStyle;
                        renderFontParams.FontWeight = FontWeight;
                        renderFontParams.FontFamily = FontFamily;
                        renderFontParams.FontSize = FontSize;
                        renderParams.FontParams = renderFontParams;
                        List<Rect> subRectangles = new List<Rect>();

                        renderParams.ShowMaxPoint = false;
                        renderParams.ShowMinPoint = false;
                        for (int i = 0; i < MarkerList.Count; i++)
                        {
                            MarkerList[i].DoRender(dc, renderParams, ClientArea, ref subRectangles);
                        }
                    }

                    if (Owner.IsShapeCreating)
                    {
                        switch (Owner.ShapeCreatorType)
                        {
                            case ShapeCreatorType.Polyline:
                                Point[] tmpPoints = ThermalCommon.RelToAbsPoints(Owner.ShapePoints.ToArray(), ClientArea);
                                Point[] points = new Point[tmpPoints.Length + 1];
                                for (int i = 0; i < tmpPoints.Length; i++)
                                {
                                    points[i] = tmpPoints[i];
                                }
                                points[tmpPoints.Length] = Owner.LastMouseOverPoint;

                                StreamGeometry geometry = new StreamGeometry();
                                using (StreamGeometryContext ctx = geometry.Open())
                                {
                                    ctx.BeginFigure(points[0], false, false);
                                    for (int i = 0; i < points.Length; i++)
                                    {
                                        ctx.LineTo(points[i], true, false);
                                    }
                                }
                                dc.DrawGeometry(null, MarkerThickShapePen, geometry);
                                dc.DrawGeometry(null, MarkerThinShapePen, geometry);
                                break;
                            case ShapeCreatorType.Polygon:
                                tmpPoints = ThermalCommon.RelToAbsPoints(Owner.ShapePoints.ToArray(), ClientArea);
                                points = new Point[tmpPoints.Length + 1];
                                for (int i = 0; i < tmpPoints.Length; i++)
                                {
                                    points[i] = tmpPoints[i];
                                }
                                points[tmpPoints.Length] = Owner.LastMouseOverPoint;

                                geometry = new StreamGeometry();
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
                                break;
                            case ShapeCreatorType.Mask:
                                tmpPoints = ThermalCommon.RelToAbsPoints(Owner.ShapePoints.ToArray(), ClientArea);
                                points = new Point[tmpPoints.Length + 1];
                                for (int i = 0; i < tmpPoints.Length; i++)
                                {
                                    points[i] = tmpPoints[i];
                                }
                                points[tmpPoints.Length] = Owner.LastMouseOverPoint;

                                geometry = new StreamGeometry();
                                using (StreamGeometryContext ctx = geometry.Open())
                                {
                                    ctx.BeginFigure(points[0], false, true);
                                    for (int i = 0; i < points.Length; i++)
                                    {
                                        ctx.LineTo(points[i], true, false);
                                    }
                                }
                                dc.DrawGeometry(null, MaskThickShapePen, geometry);
                                dc.DrawGeometry(null, MarkerThinShapePen, geometry);
                                break;
                        }
                    }
                }
                finally
                {
                    dc.Pop();
                }
            }
            finally
            {
                dc.Pop();
            }
        }

        internal int GetMarkerIndex(string name, CanvasType canvasType)
        {
            switch (canvasType)
            {
                case CanvasType.Marker:
                    for (int i = 0; i < MarkerList.Count; i++)
                    {
                        if (string.Equals(MarkerList[i].Name, name) && MarkerList[i].Type != ThermalMarkerType.Mask)
                        {
                            return i;
                        }
                    }
                    break;
                case CanvasType.Shield:
                    for (int i = 0; i < MarkerList.Count; i++)
                    {
                        if (string.Equals(MarkerList[i].Name, name) && MarkerList[i].Type == ThermalMarkerType.Mask)
                        {
                            return i;
                        }
                    }
                    break;
            }

            return -1;
        }

        internal int GetMarkerIndexAt(Point point)
        {
            if (MarkerList != null)
            {
                for (int i = MarkerList.Count - 1; i >= 0; i--)
                {
                    ThermalMarker marker = MarkerList[i];
                    if (marker.Contains(point))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private bool ValidateDeviceMarker(DeviceMarker marker)
        {
            if (marker != null && marker.PointList.Count > 0)
            {
                bool isValidate = true;
                foreach (Point point in marker.PointList)
                {
                    if (!ThermalArea.Contains(point))
                    {
                        isValidate = false;
                        break;
                    }
                }
                return isValidate;
            }
            return false;
        }
    }
}
