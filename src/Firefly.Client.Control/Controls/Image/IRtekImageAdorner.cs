// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Firefly.Client.Control
{
    public class IRtekImageAdorner : Adorner
    {
        private VisualCollection _visualCollection;
        private Image _image;
        private Grid _grid;

        public Action CloseAdornerCallback { get; set; }

        public IRtekImageAdorner(string uri, UIElement adornedElement) : base(adornedElement)
        {
            _visualCollection = new VisualCollection(this);
            _grid = new Grid();
            _grid.RowDefinitions.Add(new RowDefinition());
            _grid.Background = new SolidColorBrush(Color.FromArgb(255,23,29,43));
            _grid.MouseLeftButtonDown += grid_MouseLeftButtonDown;

            _image = new Image();
            _image.HorizontalAlignment = HorizontalAlignment.Center;
            _image.Source = new BitmapImage(new Uri(uri));
            _image.MouseLeftButtonDown += grid_MouseLeftButtonDown;
            _grid.Children.Add(_image);
            Grid.SetRow(_image, 0);
            _visualCollection.Add(_grid);
        }

        private void grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CloseAdornerCallback?.Invoke();
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visualCollection[index];
        }
        protected override int VisualChildrenCount => 1;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _grid.Arrange(new Rect(new Point(0,0), finalSize));
            return base.ArrangeOverride(finalSize);
        }
    }
}
