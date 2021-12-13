// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using System.Windows;
using System.Windows.Controls;

namespace Firefly.Client
{
    public class SplitItemsPanelControl : Grid
    {
        static SplitItemsPanelControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitItemsPanelControl), new FrameworkPropertyMetadata(typeof(SplitItemsPanelControl)));
        }

        #region Properties

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns), typeof(int), typeof(SplitItemsPanelControl), new PropertyMetadata(default(int), ColumnsChangedCallback));

        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            nameof(Rows), typeof(int), typeof(SplitItemsPanelControl), new PropertyMetadata(default(int), RowsChangedCallback));


        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        #endregion

        #region Methods

        private static void ColumnsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitItemsPanelControl grid = (SplitItemsPanelControl)d;
            grid.ColumnDefinitions.Clear();
            for (int i = 0; i < grid.Columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        private static void RowsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitItemsPanelControl grid = (SplitItemsPanelControl)d;
            grid.RowDefinitions.Clear();
            for (int i = 0; i < grid.Rows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            int index = 0;
            for (int count = this.InternalChildren.Count; index < count; ++index)
            {
                FrameworkElement internalChild = this.InternalChildren[index] as FrameworkElement;
                if (internalChild != null)
                {
                    ISplitGridItem splitGridItem = internalChild.DataContext as ISplitGridItem;
                    if (splitGridItem != null)
                    {
                        Grid.SetColumn(internalChild, splitGridItem.Column);
                        Grid.SetColumnSpan(internalChild, splitGridItem.ColumnSpan);
                        Grid.SetRow(internalChild, splitGridItem.Row);
                        Grid.SetRowSpan(internalChild, splitGridItem.RowSpan);
                    }
                }
            }
            return base.MeasureOverride(constraint);
        }
        
        #endregion
    }

    public interface ISplitGridItem
    {
        int Row { get; set; }
        int RowSpan { get; set; }
        int Column { get; set; }
        int ColumnSpan { get; set; }
    }
}