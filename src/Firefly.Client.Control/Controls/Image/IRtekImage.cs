// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Windows;
using System.Net.Http;
using System.Collections;
using System.Windows.Input;
using System.Windows.Documents;

namespace Firefly.Client.Control
{
    public class IRtekImage : System.Windows.Controls.Control
    {
        public const string PART_FULLSCREEN = "btnFullScreen";
        public const string PART_ANALYZIR = "btnAnalyzIR";
        public const string PART_DOWNLOAD = "btnDownload";

        private Amur.Controls.Button _btnAnalyzIR;
        private Amur.Controls.Button _btnFullScreen;
        private Amur.Controls.Button _btnDownload;
        private AdornerLayer _adornerLayout = null;
        private UIElement _rootElement;

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(string), typeof(IRtekImage), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, null, null), null);

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SupportAnalyzIRProperty = DependencyProperty.Register(nameof(SupportAnalyzIR), typeof(bool), typeof(IRtekImage), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, null, null), null);

        public bool SupportAnalyzIR
        {
            get => (bool)GetValue(SupportAnalyzIRProperty);
            set => SetValue(SupportAnalyzIRProperty, value);
        }

        public static readonly RoutedEvent AnalyzIRClickEvent = EventManager.RegisterRoutedEvent("AnalyzIRClick", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(IRtekImage));

        public event RoutedEventHandler AnalyzIRClick
        {
            add { AddHandler(AnalyzIRClickEvent, value); }
            remove { RemoveHandler(AnalyzIRClickEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _btnFullScreen = GetTemplateChild(PART_FULLSCREEN) as Amur.Controls.Button;
            _btnDownload = GetTemplateChild(PART_DOWNLOAD) as Amur.Controls.Button;
            _btnAnalyzIR = GetTemplateChild(PART_ANALYZIR) as Amur.Controls.Button;

            _btnAnalyzIR.Click += btnAnalyzIR_Click;
            _btnDownload.Click += btnDownload_Click;
            _btnFullScreen.Click += btnFullScreen_Click;
            _btnAnalyzIR.Visibility = SupportAnalyzIR ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnAnalyzIR_Click(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs newEvent = new RoutedEventArgs(AnalyzIRClickEvent, this);
            RaiseEvent(newEvent);
        }

        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (_adornerLayout == null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Window window = Window.GetWindow(this);
                    IEnumerable enumerable = LogicalTreeHelper.GetChildren(window);

                    if (enumerable.GetEnumerator().MoveNext())
                    {
                        var child = enumerable.GetEnumerator().Current as UIElement;
                        if (child != null)
                        {
                            _rootElement = child;
                            _adornerLayout = AdornerLayer.GetAdornerLayer(child);
                            if (_adornerLayout != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (_adornerLayout != null)
            {
                IRtekImageAdorner rtekImageAdorner = new IRtekImageAdorner(Source, _rootElement);
                rtekImageAdorner.CloseAdornerCallback = new Action(() =>
                {
                    _adornerLayout?.Remove(rtekImageAdorner);
                });
                _adornerLayout.Add(rtekImageAdorner);
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                CheckPathExists = true,
                Filter = "JPEG Image (*.jpg)|*.jpg",
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMddHHmmssfff}.jpg"),
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = saveFileDialog.FileName;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(Source))
                {
                    return;
                }
                string imageURL = Source;
                try
                {
                    _btnDownload.IsEnabled = false;
                    Mouse.SetCursor(Cursors.Wait);
                    try
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            HttpResponseMessage res = await httpClient.GetAsync(imageURL);
                            using (Stream contentStream = await res.Content.ReadAsStreamAsync())
                            {
                                if (string.IsNullOrWhiteSpace(res.Content.Headers?.ContentDisposition?.FileName))
                                {
                                    IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                                    return;
                                }

                                byte[] bs = new byte[contentStream.Length];
                                contentStream.Read(bs, 0, bs.Length);

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    ms.Write(bs, 0, bs.Length);
                                    FileStream fs = new FileStream(fileName, FileMode.Create);
                                    BinaryWriter bw = new BinaryWriter(fs);
                                    bw.Write(ms.ToArray());
                                    bw.Close();
                                    ms.Close();
                                }
                            }
                        }

                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SavedSuccessfully, I18n.Properties.Resources.Tip, MessageBoxButton.OK);
                    }
                    catch
                    {
                        IRtekMessageBox.ShowDialog(Window.GetWindow(this), I18n.Properties.Resources.SaveFailed, I18n.Properties.Resources.Tip, Control.MessageBoxButton.OKCancel);
                    }
                }
                finally
                {
                    _btnDownload.IsEnabled = true;
                    Mouse.SetCursor(Cursors.Arrow);
                }
            }
        }
    }
}