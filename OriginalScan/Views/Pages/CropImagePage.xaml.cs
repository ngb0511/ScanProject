﻿using FontAwesome5;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;
using Notification.Wpf;
using ScanProject.Converters;
using ScanProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScanProject.Views.Pages
{
    /// <summary>
    /// Interaction logic for CropImagePage.xaml
    /// </summary>
    public partial class CropImagePage : Page
    {
        public double _scale { get; set; }
        public double imageWidth { get; set; }
        public double imageHeight { get; set; }
        public double canvasWidth { get; set; }
        private ScannedImage _image { get; }
        private BitmapSource _displayedImage;
        private Stack<BitmapSource> _undoStack = new Stack<BitmapSource>();
        private Stack<BitmapSource> _redoStack = new Stack<BitmapSource>();
        private readonly NotificationManager _notificationManager;

        private double _leftNumericValue = 0;
        private double _rightNumericValue = 0;
        private double _topNumericValue = 0;
        private double _bottomNumericValue = 0;

        private bool isDragging = false;
        private Image? draggedImage;
        private Point offset;

        public CropImagePage(ScannedImage image)
        {
            _notificationManager = new NotificationManager();
            InitializeComponent();
            _image = image;
            _displayedImage = _image.bitmapImage;
            DisplayImage();
            DataContext = this;
            NotificationConstants.MessagePosition = NotificationPosition.TopRight;
        }

        private void NotificationShow(string type, string message)
        {
            switch (type)
            {
                case "error":
                    {
                        var errorNoti = new NotificationContent
                        {
                            Title = "Lỗi!",
                            Message = $"Có lỗi: {message}",
                            Type = NotificationType.Error,
                            Icon = new SvgAwesome()
                            {
                                Icon = EFontAwesomeIcon.Solid_Times,
                                Height = 25,
                                Foreground = new SolidColorBrush(Colors.Black)
                            },
                            Background = new SolidColorBrush(Colors.Red),
                            Foreground = new SolidColorBrush(Colors.White),
                        };
                        _notificationManager.Show(errorNoti);
                        break;
                    }
                case "success":
                    {
                        var successNoti = new NotificationContent
                        {
                            Title = "Thành công!",
                            Message = $"{message}",
                            Type = NotificationType.Success,
                            Icon = new SvgAwesome()
                            {
                                Icon = EFontAwesomeIcon.Solid_Check,
                                Height = 25,
                                Foreground = new SolidColorBrush(Colors.Black)
                            },
                            Background = new SolidColorBrush(Colors.Green),
                            Foreground = new SolidColorBrush(Colors.White),
                        };
                        _notificationManager.Show(successNoti);
                        break;
                    }
                case "warning":
                    {
                        var warningNoti = new NotificationContent
                        {
                            Title = "Thông báo!",
                            Message = $"{message}",
                            Type = NotificationType.Warning,
                            Icon = new SvgAwesome()
                            {
                                Icon = EFontAwesomeIcon.Solid_ExclamationTriangle,
                                Height = 25,
                                Foreground = new SolidColorBrush(Colors.Black)
                            },
                            Background = new SolidColorBrush(Colors.Yellow),
                            Foreground = new SolidColorBrush(Colors.Black),
                        };
                        _notificationManager.Show(warningNoti);
                        break;
                    }
            }

        }

        private void DisplayImage()
        {
            _scale = 0.5;
            imageHeight = _displayedImage.Height * _scale;
            imageWidth = _displayedImage.Width * _scale;
            canvasWidth = imageWidth + 50;
            image.Source = _displayedImage;
        }

        private void CropButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double left = Convert.ToDouble(txtLeft.Text);
                double right = Convert.ToDouble(txtRight.Text);
                double top = Convert.ToDouble(txtTop.Text);
                double bottom = Convert.ToDouble(txtBottom.Text);

                if (image.Source is BitmapSource bitmapSource)
                {
                    int x = (int)left;
                    int y = (int)top;
                    int width = (int)(bitmapSource.PixelWidth - left - right);
                    int height = (int)(bitmapSource.PixelHeight - top - bottom);

                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmapSource, new Int32Rect(x, y, width, height));

                    _undoStack.Push(_displayedImage);
                    _displayedImage = croppedBitmap;
                    image.Source = _displayedImage;
                    _redoStack.Clear();
                }
            }
            catch (Exception ex)
            {
                NotificationShow("error", ex.Message);
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(_displayedImage);
                _displayedImage = _undoStack.Pop();
                image.Source = _displayedImage;
            }
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(_displayedImage);
                _displayedImage = _redoStack.Pop();
                image.Source = _displayedImage;
            }
        }

        private void UpdateImageButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateImage();
        }

        private void UpdateImage()
        {
            if (_displayedImage != null)
            {
                try
                {
                    BitmapConverter.SaveImageToOriginalPath(_displayedImage, _image.ImagePath);
                }
                catch (Exception)
                {
                    NotificationShow("error", "Lưu hình ảnh thất bại!");
                    return;
                }

                MainWindow mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow.SelectedImage != null)
                {
                    mainWindow.SelectedImage.bitmapImage = BitmapConverter.ConvertBitmapSourceToBitmapImage(_displayedImage);
                    mainWindow.grEditImage.Visibility = Visibility.Collapsed;
                    mainWindow.lstvImages.Visibility = Visibility.Visible;
                }

                NotificationShow("success", "Lưu hình ảnh thành công!");
            }
            else
            {
                NotificationShow("error", "Lưu hình ảnh thất bại!");
            }
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    if (draggedImage == null) return;
                    draggedImage.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(files[0]));
                }
            }
        }

        private void UpdateLeftNumericValue()
        {
            txtLeft.Text = _leftNumericValue.ToString();
        }

        private void UpdateRightNumericValue()
        {
            txtRight.Text = _rightNumericValue.ToString();
        }

        private void UpdateTopNumericValue()
        {
            txtTop.Text = _topNumericValue.ToString();
        }

        private void UpdateBottomNumericValue()
        {
            txtBottom.Text = _bottomNumericValue.ToString();
        }

        private void IncreaseLeftButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseLeft(1);
        }

        private void DecreaseLeftButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseLeft(1);
        }

        private void IncreaseRightButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseRight(1);
        }

        private void DecreaseRightButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseRight(1);
        }

        private void IncreaseTopButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseTop(1);
        }

        private void DecreaseTopButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseTop(1);
        }

        private void IncreaseBottomButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseBottom(1);
        }

        private void DecreaseBottomButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseBottom(1);
        }

        private void IncreaseAllButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseBottom(int.Parse(txtAll.Text));
            IncreaseTop(int.Parse(txtAll.Text));
            IncreaseLeft(int.Parse(txtAll.Text));
            IncreaseRight(int.Parse(txtAll.Text));
        }

        private void DecreaseAllButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseBottom(int.Parse(txtAll.Text));
            DecreaseTop(int.Parse(txtAll.Text));
            DecreaseLeft(int.Parse(txtAll.Text));
            DecreaseRight(int.Parse(txtAll.Text));
        }

        private void DecreaseBottom(int amount)
        {
            _bottomNumericValue -= amount;

            if (_bottomNumericValue < 0)
                _bottomNumericValue = 0;

            UpdateBottomNumericValue();
        }

        private void DecreaseTop(int amount)
        {
            _topNumericValue -= amount;

            if (_topNumericValue < 0)
                _topNumericValue = 0;

            UpdateTopNumericValue();
        }

        private void DecreaseLeft(int amount)
        {
            _leftNumericValue -= amount;

            if (_leftNumericValue < 0)
                _leftNumericValue = 0;

            UpdateLeftNumericValue();
        }

        private void DecreaseRight(int amount)
        {
            _rightNumericValue -= amount;

            if (_rightNumericValue < 0)
                _rightNumericValue = 0;

            UpdateRightNumericValue();
        }

        private void IncreaseBottom(int amount)
        {
            _bottomNumericValue += amount;

            if (_bottomNumericValue < 0)
                _bottomNumericValue = 0;

            UpdateBottomNumericValue();
        }

        private void IncreaseTop(int amount)
        {
            _topNumericValue += amount;

            if (_topNumericValue < 0)
                _topNumericValue = 0;

            UpdateTopNumericValue();
        }

        private void IncreaseLeft(int amount)
        {
            _leftNumericValue += amount;

            if (_leftNumericValue < 0)
                _leftNumericValue = 0;

            UpdateLeftNumericValue();
        }

        private void IncreaseRight(int amount)
        {
            _rightNumericValue += amount;

            if (_rightNumericValue < 0)
                _rightNumericValue = 0;

            UpdateRightNumericValue();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult Result = System.Windows.MessageBox.Show($"Bạn có muốn thoát mà không lưu những thay đổi?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (Result == MessageBoxResult.Yes)
            {
                MainWindow mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.lstvImages.Visibility = Visibility.Visible;
                    mainWindow.grEditImage.Visibility = Visibility.Collapsed;
                }
            }

            else return;
        }

        private void txtAll_KeyDown(object sender, KeyEventArgs e)
        {
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)))
            {
                e.Handled = true;
            }
        }
    }
}
