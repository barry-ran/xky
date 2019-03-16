﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace Xky.Core
{
    /// <summary>
    ///     MirrorScreen.xaml 的交互逻辑
    /// </summary>
    public partial class MirrorScreen
    {
        private readonly Dictionary<int, int> _fpsDictionary = new Dictionary<int, int>();
        internal readonly Timer FpsTimer = new Timer();
        private MirrorClient _client;

        private bool _isShow;
        private WriteableBitmap _writeableBitmap;

        public MirrorScreen()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(ScreenImage, BitmapScalingMode.LowQuality);
            FpsTimer.Enabled = true;
            FpsTimer.Interval = 1000;
            FpsTimer.Elapsed += FpsTimer_Elapsed;
        }

        private void FpsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var second = DateTime.Now.Second;
            if (_fpsDictionary.ContainsKey(second - 1))
            {
                Dispatcher.Invoke(() => { FpsLabel.Content = "FPS:" + _fpsDictionary[second - 1]; });
                _fpsDictionary.Remove(second - 1);
            }
            else
            {
                Dispatcher.Invoke(() => { FpsLabel.Content = "FPS:" + 0; });
            }
        }

        public void SetClient(MirrorClient client)
        {
            if (_client != null) _client.Decoder.OnDecodeBitmapSource -= Decoder_OnDecodeBitmapSource;

            client.MirrorScreen = this;
            _isShow = false;
            _client = client;
            _client.Decoder.OnDecodeBitmapSource += Decoder_OnDecodeBitmapSource;
        }

        private void Decoder_OnDecodeBitmapSource(object sender, int width, int height, int stride, IntPtr intprt)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (IsShowFps)
                    {
                        var second = DateTime.Now.Second;
                        if (_fpsDictionary.ContainsKey(second))
                            _fpsDictionary[second]++;
                        else
                            _fpsDictionary.Add(second, 0);
                    }

                    if (ScreenImage.Source == null)
                    {
                        _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                        ScreenImage.Source = _writeableBitmap;
                    }

                    _writeableBitmap?.WritePixels(new Int32Rect(0, 0, width, height), intprt, width * height * 4,
                        stride);


                    if (!_isShow)
                    {
                        AddLabel("成功解析画面..", Colors.Lime);
                        _isShow = true;
                        HideLoading();
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region 属性

        public bool IsShowFps
        {
            get => (bool) GetValue(IsShowFpsProperty);
            set => SetValue(IsShowFpsProperty, value);
        }

        public static readonly DependencyProperty IsShowFpsProperty =
            DependencyProperty.Register("IsShowFps", typeof(bool), typeof(MirrorScreen), new PropertyMetadata(true,
                (o, e) =>
                {
                    var li = (MirrorScreen) o;


                    if ((bool) e.NewValue == false)
                    {
                        li.FpsTimer.Enabled = false;
                        li.FpsLabel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        li.FpsTimer.Enabled = true;
                        li.FpsLabel.Visibility = Visibility.Visible;
                    }
                }));

        public bool IsShowLog
        {
            get => (bool) GetValue(IsShowLogProperty);
            set => SetValue(IsShowLogProperty, value);
        }

        public static readonly DependencyProperty IsShowLogProperty =
            DependencyProperty.Register("IsShowLog", typeof(bool), typeof(MirrorScreen), new PropertyMetadata(true,
                (o, e) =>
                {
                    var li = (MirrorScreen) o;

                    li.LogPanel.Visibility = (bool) e.NewValue == false ? Visibility.Collapsed : Visibility.Visible;
                }));

        public bool IsShowArrow
        {
            get => (bool) GetValue(IsShowArrowProperty);
            set => SetValue(IsShowArrowProperty, value);
        }

        public static readonly DependencyProperty IsShowArrowProperty =
            DependencyProperty.Register("IsShowArrow", typeof(bool), typeof(MirrorScreen), new PropertyMetadata(true,
                (o, e) => { }));

        #endregion

        #region 屏幕操作

        private void Image_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var postion = e.GetPosition(ScreenImage);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (IsShowArrow)
                {
                    Tap.Fill = new SolidColorBrush(Color.FromArgb(126, 255, 182, 0));
                }

                _client?.EmitEvent(
                    new JObject
                    {
                        {"type", "device_button"},
                        {"name", "back"}
                    });
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsShowArrow)
                {
                    Tap.Fill = new SolidColorBrush(Color.FromArgb(126, 0, 255, 0));
                }

                var json = new JObject
                {
                    {"type", "mousedown"},
                    {"x", (postion.X / RenderSize.Width).ToString("F4")},
                    {"y", (postion.Y / RenderSize.Height).ToString("F4")}
                };
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    json.Add("zoom", true);
                _client?.EmitEvent(json);
                MyInput.Focus();
            }

            if (IsShowArrow)
            {
                Tap.Visibility = Visibility.Visible;
                Tap.SetValue(Window.TopProperty, postion.Y - 15);
                Tap.SetValue(Window.LeftProperty, postion.X - 15);
            }
        }

        private void Image_OnMouseMove(object sender, MouseEventArgs e)
        {
            var postion = e.GetPosition(ScreenImage);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var json = new JObject
                {
                    {"type", "mousedrag"},
                    {"x", (postion.X / RenderSize.Width).ToString("F4")},
                    {"y", (postion.Y / RenderSize.Height).ToString("F4")}
                };
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    json.Add("zoom", true);
                _client?.EmitEvent(json);
                if (IsShowArrow)
                {
                    Tap.SetValue(Window.TopProperty, postion.Y - 15);
                    Tap.SetValue(Window.LeftProperty, postion.X - 15);
                }
            }
        }

        private void Image_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var postion = e.GetPosition(ScreenImage);
            var json = new JObject
            {
                {"type", "mouseup"},
                {"x", (postion.X / RenderSize.Width).ToString("F4")},
                {"y", (postion.Y / RenderSize.Height).ToString("F4")}
            };
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                json.Add("zoom", true);
            _client?.EmitEvent(json);
            if (IsShowArrow)
            {
                Tap.Visibility = Visibility.Collapsed;
            }
        }


        private void Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            var postion = e.GetPosition(ScreenImage);
            var json = new JObject
            {
                {"type", "mouseup"},
                {"x", (postion.X / RenderSize.Width).ToString("F4")},
                {"y", (postion.Y / RenderSize.Height).ToString("F4")}
            };
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                json.Add("zoom", true);
            _client?.EmitEvent(json);
        }

        private void MyInput_LostFocus(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("丢失焦点");
        }

        private void MyInput_GotFocus(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("获取焦点");
        }

        private void MyInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _client.EmitEvent(new JObject
            {
                {"text", e.Text},
                {"type", "input"}
            });
        }

        private void MyInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Back:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 67},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Return:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 66},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Space:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 62},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Up:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 19},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Down:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 20},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Left:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 21},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
                case Key.Right:
                {
                    _client.EmitEvent(new JObject
                    {
                        {"key", 22},
                        {"name", "code"},
                        {"type", "device_button"}
                    });
                    return;
                }
            }

            if (e.Key != Key.V || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
                _client.EmitEvent(new JObject
                {
                    {"text", text},
                    {"type", "input"}
                });
        }

        #endregion

        #region  Loading和日志

        private void HideLoading()
        {
            var myBrush = new SolidColorBrush();
            var myColorAnimation = new ColorAnimation
            {
                From = Colors.White,
                To = Colors.Transparent,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                AutoReverse = false
            };
            myColorAnimation.Completed += MyColorAnimation_Completed;

            myBrush.BeginAnimation(SolidColorBrush.ColorProperty, myColorAnimation, HandoffBehavior.Compose);

            ScreenLoading.Foreground = myBrush;
        }

        public void ShowLoading()
        {
            //  ScreenLoading.Visibility = Visibility.Visible;
        }


        private void MyColorAnimation_Completed(object sender, EventArgs e)
        {
            // ScreenLoading.Visibility = Visibility.Collapsed;
        }


        private void HideLabel(Label label)
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(3 * 1000));
                Dispatcher.Invoke(() =>
                {
                    var myBrush = new SolidColorBrush();
                    ScreenLoading.IsActive = false;
                    var myColorAnimation = new ColorAnimation
                    {
                        From = ((SolidColorBrush) label.Foreground).Color,
                        To = Colors.Transparent,
                        Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                        AutoReverse = false
                    };
                    myColorAnimation.Completed += delegate { LogPanel.Children.Remove(label); };
                    myBrush.BeginAnimation(SolidColorBrush.ColorProperty, myColorAnimation, HandoffBehavior.Compose);
                    label.Foreground = myBrush;
                });
            });
        }

        public void AddLabel(string msg, Color color)
        {
            if (IsShowLog)
            {
                var label = new Label
                {
                    Content = msg,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Direction = 300,
                        ShadowDepth = 1,
                        BlurRadius = 0,
                        Opacity = 1
                    },
                    Foreground = new SolidColorBrush(color)
                };
                label.Style = null;
                LogPanel.Children.Add(label);
                HideLabel(label);
            }
        }

        #endregion
    }
}