using Microsoft.Win32;
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
using System.IO;
using System.Drawing;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Xml;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using ru.lsreg.math;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Markup;
using HSI.SatelliteInfo;
using Ookii.Dialogs.Wpf;

namespace HSI
{

    public partial class MainWindow : System.Windows.Window
    {
        bool makingWrapper = false;
        bool settingModel = false;
        int[] wrapedArea;
        System.Windows.Point mouseStart;
        ImageInfo imageInfo;
        List<Model> models;
        BackgroundWorker backgroundWorker;
        string Formula = "";
        float classifyThreshold = 0;
        string classifyMethod = "ClassifyBarycentric";
        public Satellite satellite;
        string path;
        public string[] bandPaths = new string[3];
        public string[] bandNames = new string[3];
        float segSigma = 0, segK = 0;
        int segMin = 0;
        int mode = 1;

        public MainWindow()
        {
            InitializeComponent();
            models = new List<Model>();
            imageInfo = new ImageInfo();
            wrapedArea = new int[4];
            backgroundWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
            zoom_border.SizeChanged += zoom_border_SizeChanged;
            UpdateCommandState();
        }

        private bool HasImage()
        {
            return imageInfo != null && imageInfo.GetMat() != null && !imageInfo.GetMat().Empty();
        }

        private void SetStatus(string text)
        {
            statusText.Text = text;
        }

        private void SetImageSource()
        {
            if (HasImage())
            {
                BitmapSource source = imageInfo.GetBI();
                if (source != null)
                {
                    scr_img.Source = source;
                    scr_img.Width = imageInfo.width;
                    scr_img.Height = imageInfo.height;
                    ResetImageTransform();
                    emptyStateText.Visibility = Visibility.Collapsed;
                    Dispatcher.BeginInvoke(new Action(FitImageToWorkspace), System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
                else
                {
                    scr_img.Source = null;
                    emptyStateText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                scr_img.Source = null;
                emptyStateText.Visibility = Visibility.Visible;
            }
            scr_img.UpdateLayout();
        }

        private void ResetImageTransform()
        {
            TransformGroup group = scr_img.RenderTransform as TransformGroup;
            if (group == null)
                return;

            ScaleTransform scale = group.Children.FirstOrDefault(tr => tr is ScaleTransform) as ScaleTransform;
            TranslateTransform translate = group.Children.FirstOrDefault(tr => tr is TranslateTransform) as TranslateTransform;

            if (scale != null)
            {
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
            }

            if (translate != null)
            {
                translate.X = 0.0;
                translate.Y = 0.0;
            }
        }

        private void FitImageToWorkspace()
        {
            if (!HasImage())
                return;

            canvas_main.UpdateLayout();
            zoom_border.UpdateLayout();
            scr_img.UpdateLayout();
            zoom_border.FitToBounds(imageInfo.width, imageInfo.height);
        }

        private void HideResultLegend()
        {
            legendPanel.Visibility = Visibility.Collapsed;
            legendTitle.Text = "";
            legendContent.Children.Clear();
        }

        private TextBlock CreateLegendText(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 81, 95)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void ShowRasterLegend(string formulaText)
        {
            legendTitle.Text = "Палитра";
            legendContent.Children.Clear();

            legendContent.Children.Add(CreateLegendText("Значение формулы: " + formulaText));

            Border gradient = new Border
            {
                Height = 18,
                Margin = new Thickness(0, 8, 0, 4),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(216, 222, 230)),
                BorderThickness = new Thickness(1),
                Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 0), 0.0),
                        new GradientStop(System.Windows.Media.Color.FromRgb(200, 200, 0), 0.5),
                        new GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 0), 1.0)
                    },
                    new System.Windows.Point(0, 0.5),
                    new System.Windows.Point(1, 0.5))
            };
            legendContent.Children.Add(gradient);

            Grid labels = new Grid();
            labels.ColumnDefinitions.Add(new ColumnDefinition());
            labels.ColumnDefinitions.Add(new ColumnDefinition());
            labels.ColumnDefinitions.Add(new ColumnDefinition());
            labels.Children.Add(CreateLegendLabel("-1 и ниже", 0, HorizontalAlignment.Left));
            labels.Children.Add(CreateLegendLabel("0", 1, HorizontalAlignment.Center));
            labels.Children.Add(CreateLegendLabel("+1 и выше", 2, HorizontalAlignment.Right));
            legendContent.Children.Add(labels);

            legendPanel.Visibility = Visibility.Visible;
        }

        private TextBlock CreateLegendLabel(string text, int column, HorizontalAlignment alignment)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(94, 108, 122)),
                FontSize = 12,
                HorizontalAlignment = alignment
            };
            Grid.SetColumn(label, column);
            return label;
        }

        private void ShowClassificationLegend()
        {
            legendTitle.Text = "Классы";
            legendContent.Children.Clear();

            WrapPanel panel = new WrapPanel();
            foreach (Model model in models)
            {
                StackPanel item = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 16, 6),
                    VerticalAlignment = VerticalAlignment.Center
                };

                item.Children.Add(new Border
                {
                    Width = 18,
                    Height = 18,
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(216, 222, 230)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(model.userColor),
                    Margin = new Thickness(0, 0, 6, 0)
                });

                item.Children.Add(CreateLegendText(model.name + " - " + model.coverPercentage.ToString("0.##") + "%"));
                panel.Children.Add(item);
            }

            legendContent.Children.Add(panel);
            legendPanel.Visibility = models.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSegmentLegend()
        {
            legendTitle.Text = "Сегменты";
            legendContent.Children.Clear();
            legendContent.Children.Add(CreateLegendText("Цвет каждого сегмента - средний цвет пикселей внутри сегмента; отдельной числовой шкалы у этого результата нет."));
            legendPanel.Visibility = Visibility.Visible;
        }

        private void ShowGrayLegend(string title, string left, string right)
        {
            legendTitle.Text = title;
            legendContent.Children.Clear();

            Border gradient = new Border
            {
                Height = 18,
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(216, 222, 230)),
                BorderThickness = new Thickness(1),
                Background = new LinearGradientBrush(Colors.Black, Colors.White, new System.Windows.Point(0, 0.5), new System.Windows.Point(1, 0.5))
            };
            legendContent.Children.Add(gradient);

            Grid labels = new Grid();
            labels.ColumnDefinitions.Add(new ColumnDefinition());
            labels.ColumnDefinitions.Add(new ColumnDefinition());
            labels.Children.Add(CreateLegendLabel(left, 0, HorizontalAlignment.Left));
            labels.Children.Add(CreateLegendLabel(right, 1, HorizontalAlignment.Right));
            legendContent.Children.Add(labels);

            legendPanel.Visibility = Visibility.Visible;
        }

        private void ShowResultLegend(string operationName)
        {
            if (operationName == "CalculateRaster")
            {
                ShowRasterLegend(Formula);
                return;
            }

            if (operationName == "ClassifyBarycentric" || operationName == "ClassifyAngle" || operationName == "ClassifyEuclid")
            {
                ShowClassificationLegend();
                return;
            }

            if (operationName == "GBSegmentationSpectralAngle")
            {
                ShowSegmentLegend();
                return;
            }

            if (operationName == "EMD")
            {
                ShowGrayLegend("EMD", "0", "255");
                return;
            }

            HideResultLegend();
        }

        private void zoom_border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (HasImage())
                FitImageToWorkspace();
        }

        private bool EnsureImageLoaded(string action)
        {
            if (HasImage())
                return true;

            UiDialogHelper.ShowWarning(this, action + " можно выполнить только после загрузки изображения.");
            return false;
        }

        private bool EnsureSatelliteMetadata(string action)
        {
            if (satellite != null)
                return true;

            UiDialogHelper.ShowWarning(this, action + " требует снимок, собранный через \"Добавить изображение\", чтобы были известны спутник и разрешение каналов.");
            return false;
        }

        private bool EnsureModels()
        {
            if (models.Count > 0)
                return true;

            UiDialogHelper.ShowWarning(this, "Для классификации добавьте хотя бы один эталон: нажмите \"Добавить эталон\" и выделите область на изображении.");
            return false;
        }

        private bool TryStartOperation(string operationName)
        {
            if (backgroundWorker.IsBusy)
            {
                UiDialogHelper.ShowWarning(this, "Дождитесь завершения текущей операции.");
                return false;
            }

            progressBar.Value = 0;
            SetStatus("Выполняется: " + GetOperationTitle(operationName));
            backgroundWorker.RunWorkerAsync(operationName);
            UpdateCommandState();
            return true;
        }

        private string GetOperationTitle(string operationName)
        {
            switch (operationName)
            {
                case "AddImage": return "сборка изображения";
                case "CalculateRaster": return "калькулятор растров";
                case "GBSegmentationSpectralAngle": return "сегментация";
                case "EMD": return "EMD";
                case "ClassifyBarycentric": return "классификация";
                case "ClassifyAngle": return "классификация";
                case "ClassifyEuclid": return "классификация";
                default: return operationName;
            }
        }

        private void UpdateCommandState()
        {
            bool busy = backgroundWorker != null && backgroundWorker.IsBusy;
            bool hasImage = HasImage();
            addImage_btn.IsEnabled = !busy;
            saveImage_btn.IsEnabled = !busy && hasImage;
            crop_btn.IsEnabled = !busy && hasImage;
            calcRaster_btn.IsEnabled = !busy;
            EMD_btn.IsEnabled = !busy;
            setModel_btn.IsEnabled = !busy && hasImage;
            classify_btn.IsEnabled = !busy && hasImage && models.Count > 0;
            segment_btn.IsEnabled = !busy && hasImage;
            if (modelsHintText != null)
                modelsHintText.Visibility = models.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ImageAddingWindow dialog = new ImageAddingWindow();
            if (dialog.ShowDialog() == true)
            {
                bandPaths = dialog.BandPaths;
                path = dialog.path;
                bandNames[0] = (string)dialog.ch1_lbl.Content;
                bandNames[1] = (string)dialog.ch2_lbl.Content;
                bandNames[2] = (string)dialog.ch3_lbl.Content;
                
                satellite = dialog.sat;

                TryStartOperation("AddImage");
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                UpdateCommandState();
                return;
            } 
        }

        System.Windows.Media.Color GetRandomColor()
        {
            var randomBytes = new Byte[3];
            new Random().NextBytes(randomBytes);
            return System.Windows.Media.Color.FromRgb(randomBytes[0], randomBytes[1], randomBytes[2]);
        }

        void PrepareAndSaveStats(string path)
        {
            using (StreamWriter str = File.CreateText(path))
            {
                double totalPercentage = 0;
                for (int i = 0; i < models.Count; i++)
                {
                    totalPercentage += models[i].coverPercentage;
                    str.WriteLine(i + 1 + ". " + models[i].name + ": " + models[i].coverPercentage + "% -> " +
                        models[i].coverPixels + " pieces" + " -> " + models[i].coverMetres + "m");
                }
                str.WriteLine("\n Total: " + totalPercentage + "%");
            }
        }

        private void SetModel(int[] meanR, int[] meanG, int[] meanB, byte usersRed, byte usersGreen, byte usersBlue, string name)
        {
            Model model = new Model();
            model.data = new int[,] {
                { meanR[0], meanR[1], meanR[2] },
                { meanG[0], meanG[1], meanG[2] },
                { meanB[0], meanB[1], meanB[2] }
            };
            model.color = System.Windows.Media.Color.FromRgb((byte)meanR[0], (byte)meanG[1], (byte)meanB[2]);
            //model.userColor = GetRandomColor();
            model.userColor = System.Windows.Media.Color.FromRgb(usersRed, usersGreen, usersBlue);
            model.name = name;
            models.Add(model);

            Button button = new Button();
            button.Name = "ModelButton" + models.Count;
            button.Tag = model;
            button.Margin = new Thickness(0, 0, 0, 6);
            button.ToolTip = "Удалить эталон \"" + name + "\"";
            StackPanel panel = new StackPanel { Orientation = Orientation.Vertical, Width = 140, Height = 60 };
            panel.Children.Add(new Label { Content = name, Padding = new Thickness(8, 0, 8, 0), Height = 22, FontSize = 14 });
            panel.Children.Add(new Label { Background = new SolidColorBrush(model.userColor), Height = 18 });
            panel.Children.Add(new Label { Background = new SolidColorBrush(model.color), Height = 18 });
            button.Content = panel;
            button.Click += DeleteModel;
            modelsPanel.Children.Add(button);
            UpdateCommandState();

        }

        void DeleteModel(object sender, RoutedEventArgs e)
        {
            var ui = (UIElement)sender;
            var elem = (Button)sender;
            Model model = elem.Tag as Model;
            if (model != null)
                models.Remove(model);
            modelsPanel.Children.Remove(ui);
            UpdateCommandState();
        }

        byte Clamp(byte x, byte min, byte max)
        {
            if (x > max) return max;
            if (x < min) return min;
            return x;
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            OpenFileDialog ofd = new OpenFileDialog();
            string type = (string)item.Header;

            if (type == ".tif")
                ofd.Filter = "HSI | *.tif;*.tiff";
            if (type == ".png")
                ofd.Filter = "HSI | *.png";
            if (type == ".jpeg")
                ofd.Filter = "HSI | *.jpg;*.jpeg";
            if (type == ".jpeg2000")
                ofd.Filter = "HSI | *.j2k;*.jp2;*.jpf;*.jpm;*.jpc;*.jpg2;*.j2c;*.jpx;*.mj2";

            if (ofd.ShowDialog() == true)
            {
                Mat loaded = Cv2.ImRead(ofd.FileName);
                if (loaded == null || loaded.Empty())
                {
                    UiDialogHelper.ShowError(this, "Не удалось открыть выбранное изображение.");
                    return;
                }

                imageInfo.Dispose();
                imageInfo = new ImageInfo(loaded, ofd.FileName);
                satellite = null;
                models.Clear();
                modelsPanel.Children.Clear();
                SetImageSource();
                HideResultLegend();
                SetStatus("Открыто изображение: " + System.IO.Path.GetFileName(ofd.FileName));
                UpdateCommandState();
            }
        }

        private void ClearCanvas(object sender, RoutedEventArgs e)
        {
            imageInfo.Dispose();
            imageInfo = new ImageInfo();
            models.Clear();
            modelsPanel.Children.Clear();
            satellite = null;
            SetImageSource();
            HideResultLegend();
            SetStatus("Изображение очищено");
            UpdateCommandState();
            // остаётся неосвобождёнными 100мб, освобождаются после повторного вызова ClearCanvas(!?)
        }

        private void scr_img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (makingWrapper)
            {
                UIElement element = (UIElement)sender;
                int x = (int)e.GetPosition(element).X;
                int y = (int)e.GetPosition(element).Y;
                wrapedArea[0] = x;
                wrapedArea[1] = y;
                element.CaptureMouse();

                x = (int)e.GetPosition(canvas_main).X;
                y = (int)e.GetPosition(canvas_main).Y;
                mouseStart = new System.Windows.Point(x, y);
                var rect = (UIElement)rect_for_wrap;
                var tt = (TranslateTransform)((TransformGroup)rect.RenderTransform)
                    .Children.First(tr => tr is TranslateTransform);
                tt.X = x;
                tt.Y = y;
            }
        }

        private void scr_img_MouseMove(object sender, MouseEventArgs e)
        {
            if (makingWrapper)
            {
                UIElement element = (UIElement)sender;
                if (!element.IsMouseCaptured) return;

                int x = (int)e.GetPosition(canvas_main).X;
                int y = (int)e.GetPosition(canvas_main).Y;
                var st = (ScaleTransform)((TransformGroup)rect_for_wrap.RenderTransform)
                    .Children.First(tr => tr is ScaleTransform);
                if (y - mouseStart.Y < 0 && st.ScaleY > 0 || y - mouseStart.Y > 0 && st.ScaleY < 0) st.ScaleY *= -1;
                if (x - mouseStart.X < 0 && st.ScaleX > 0 || x - mouseStart.X > 0 && st.ScaleX < 0) st.ScaleX *= -1;
                rect_for_wrap.Width = Math.Abs(x - mouseStart.X);
                rect_for_wrap.Height = Math.Abs(y - mouseStart.Y);
            }
        }

        private void scr_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (makingWrapper)
            {
                UIElement element = (UIElement)sender;
                int x = (int)e.GetPosition(element).X;
                int y = (int)e.GetPosition(element).Y;
                setModel_btn.ClearValue(Button.BackgroundProperty);
                crop_btn.ClearValue(Button.BackgroundProperty);
                zoom_border.isActive = true;
                makingWrapper = false;
                wrapedArea[2] = x;
                wrapedArea[3] = y;
                element.ReleaseMouseCapture();
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;

                if (wrapedArea[0] > wrapedArea[2])
                {
                    var t = wrapedArea[0];
                    wrapedArea[0] = wrapedArea[2];
                    wrapedArea[2] = t;
                }
                if (wrapedArea[1] > wrapedArea[3])
                {
                    var t = wrapedArea[1];
                    wrapedArea[1] = wrapedArea[3];
                    wrapedArea[3] = t;
                }

                wrapedArea[0] = Math.Max(0, Math.Min(wrapedArea[0], imageInfo.width - 1));
                wrapedArea[1] = Math.Max(0, Math.Min(wrapedArea[1], imageInfo.height - 1));
                wrapedArea[2] = Math.Max(0, Math.Min(wrapedArea[2], imageInfo.width));
                wrapedArea[3] = Math.Max(0, Math.Min(wrapedArea[3], imageInfo.height));

                int wrapedX = wrapedArea[2] - wrapedArea[0];
                int wrapedY = wrapedArea[3] - wrapedArea[1];
                int wrapedSize = wrapedX * wrapedY;
                if (wrapedSize == 0)
                    return;

                Mat part = imageInfo.GetMat().SubMat(wrapedArea[1], wrapedArea[3], wrapedArea[0], wrapedArea[2]);

                if (settingModel)
                {
                    Vec3b[] wrapedBytes;
                    int[] meanR = { 0, 0, 0 }, meanG = { 0, 0, 0 }, meanB = { 0, 0, 0 };
                    part.GetArray(out wrapedBytes);

                    Scalar mean = part.Mean();
                    for (int i = 0; i < wrapedSize; i++)
                    {
                        if (Math.Abs(meanR[0] - mean[2]) > Math.Abs(wrapedBytes[i][2] - mean[2]))
                        {
                            meanR[0] = wrapedBytes[i][2];
                            meanR[1] = wrapedBytes[i][1];
                            meanR[2] = wrapedBytes[i][0];
                        }
                        if (Math.Abs(meanG[1] - mean[1]) > Math.Abs(wrapedBytes[i][1] - mean[1]))
                        {
                            meanG[0] = wrapedBytes[i][2];
                            meanG[1] = wrapedBytes[i][1];
                            meanG[2] = wrapedBytes[i][0];
                        }
                        if (Math.Abs(meanB[2] - mean[0]) > Math.Abs(wrapedBytes[i][0] - mean[0]))
                        {
                            meanB[0] = wrapedBytes[i][2];
                            meanB[1] = wrapedBytes[i][1];
                            meanB[2] = wrapedBytes[i][0];
                        }
                    }

                    ModelAddingWindow dialog = new ModelAddingWindow();
                    if (dialog.ShowDialog() == true)
                    {
                        SetModel(meanR, meanG, meanB, dialog.Red, dialog.Green, dialog.Blue, dialog.ModelName.Replace(" ", "_"));
                    }
                    settingModel = false;
                    SetStatus("Эталон добавлен. Всего эталонов: " + models.Count);
                }
                else
                {
                    Mat res = part.Clone();
                    imageInfo.Dispose();
                    imageInfo = new ImageInfo(res, satellite, "", bandPaths, bandNames);
                    SetImageSource();
                    HideResultLegend();
                    SetStatus("Изображение обрезано. Для записи результата используйте \"Сохранить как...\".");
                    UpdateCommandState();
                }

                part.Dispose();
            }
        }

        private void classify_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureImageLoaded("Классификацию") || !EnsureSatelliteMetadata("Классификация") || !EnsureModels())
                return;

            ClassifyWindow dialog = new ClassifyWindow();
            if (dialog.ShowDialog() == true)
            {
                classifyMethod = dialog.Method;
                classifyThreshold = dialog.Threshold;
                TryStartOperation(classifyMethod);
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                UpdateCommandState();
                return;
            }
        }

        private void segment_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureImageLoaded("Сегментацию") || !EnsureSatelliteMetadata("Сегментация"))
                return;

            SegmentWindow dialog = new SegmentWindow();
            if (dialog.ShowDialog() == true)
            {
                segSigma = dialog.tr1;
                segK = dialog.tr2;
                segMin = dialog.tr3;
                TryStartOperation("GBSegmentationSpectralAngle");
            }
            else
            {
                UpdateCommandState();
                return;
            }
        }

        private void setModel_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureImageLoaded("Добавление эталона"))
                return;

            if (makingWrapper)
            {
                settingModel = false;
                setModel_btn.ClearValue(Button.BackgroundProperty);
                crop_btn.ClearValue(Button.BackgroundProperty);
                zoom_border.isActive = true;
                makingWrapper = false;
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;
                SetStatus("Готово");
            }
            else
            {
                settingModel = true;
                setModel_btn.Background = System.Windows.Media.Brushes.PaleGreen;
                crop_btn.ClearValue(Button.BackgroundProperty);
                zoom_border.isActive = false;
                makingWrapper = true;
                SetStatus("Выделите область на изображении для нового эталона.");
            }
        }

        private void GetStatistic_btn(object sender, RoutedEventArgs e)
        {

        }

        private void EMD_btn_Click(object sender, RoutedEventArgs e)
        {
            EMDWindow dialog = new EMDWindow();
            if (dialog.ShowDialog() == true)
            {
                mode = dialog.Mode;
                path = dialog.path;
                satellite = dialog.sat;
                TryStartOperation("EMD");
            }
            else
            {
                UpdateCommandState();
                return;
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Tuple<string, Mat> res;

            switch ((string)e.Argument)
            {
                case "ClassifyBarycentric":
                    res = new Tuple<string, Mat>("ClassifyBarycentric", 
                        ClassifyImage.ClassifyBarycentric(classifyThreshold, models, imageInfo, backgroundWorker));
                    e.Result = res;
                    break;
                case "ClassifyAngle":
                    res = new Tuple<string, Mat>("ClassifyAngle", 
                        ClassifyImage.ClassifyAngle(classifyThreshold, models, imageInfo, backgroundWorker));
                    e.Result = res;
                    break;
                case "ClassifyEuclid":
                    res = new Tuple<string, Mat>("ClassifyEuclid", 
                        ClassifyImage.ClassifyEuclid(classifyThreshold, models, imageInfo, backgroundWorker));
                    e.Result = res;
                    break;
                case "AddImage":
                    e.Result = new Tuple<string, Mat>("AddImage", 
                        ImageBuilder.BuildImage(bandPaths, satellite, backgroundWorker));
                    break;
                case "CalculateRaster":
                    res = new Tuple<string, Mat>("CalculateRaster", 
                        RasterCalcs.CalculateRaster(Formula, bandPaths, backgroundWorker, satellite));
                    e.Result = res;
                    break;
                case "GBSegmentationSpectralAngle":
                    int ccsNum;
                    res = new Tuple<string, Mat>("GBSegmentationSpectralAngle", 
                        SegmentImage.SegmentAngle(imageInfo.GetMat(), segSigma, segK, segMin, out ccsNum, backgroundWorker));
                    e.Result = res;
                    break;
                case "EMD":
                    res = new Tuple<string, Mat>("EMD",
                        EMDImage.EMD(mode, path, imageInfo.GetMat(), backgroundWorker, satellite));
                    e.Result = res;
                    break;
            }
            backgroundWorker.ReportProgress(0);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateCommandState();

            if (e.Error != null)
            {
                progressBar.Value = 0;
                SetStatus("Операция завершилась с ошибкой");
                UiDialogHelper.ShowError(this, "Не удалось выполнить операцию. Проверьте выбранные файлы и параметры.", e.Error);
                return;
            }

            if (e.Cancelled || e.Result == null)
            {
                progressBar.Value = 0;
                SetStatus("Операция отменена или не дала результата");
                UiDialogHelper.ShowWarning(this, "Операция не дала результата. Проверьте, что каналы выбраны корректно и имеют одинаковый размер.");
                return;
            }

            var res = (Tuple<string, Mat>)e.Result;
            if (res.Item2 == null || res.Item2.Empty())
            {
                progressBar.Value = 0;
                SetStatus("Операция не дала результата");
                UiDialogHelper.ShowWarning(this, "Операция не дала результата. Проверьте входные данные.");
                return;
            }

            string savePath = "";

            if (res.Item1 == "ClassifyBarycentric" || res.Item1 == "ClassifyAngle" || res.Item1 == "ClassifyEuclid") {
                PrepareAndSaveStats(System.IO.Path.Combine(path, "classification_stats.txt"));
                savePath = System.IO.Path.Combine(path, "classification_result.tif");
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
            }
            if (res.Item1 == "AddImage")
            {
                savePath = System.IO.Path.Combine(path, "rgb_preview.tif");
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
            }
            if (res.Item1 == "CalculateRaster")
            {
                savePath = System.IO.Path.Combine(path, "raster_calc_result.tif");
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
            }
            if (res.Item1 == "GBSegmentationSpectralAngle")
            {
                savePath = System.IO.Path.Combine(path, "segmentation_result.tif");
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
            }
            if (res.Item1 == "EMD")
            {
                savePath = System.IO.Path.Combine(path, "emd_result.tif");
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
            }
            
            Cv2.ImWrite(savePath, res.Item2);
            //OpenSaveHelper.SaveTifImage(savePath, res.Item2);
            SetImageSource();
            ShowResultLegend(res.Item1);
            SetStatus("Готово: " + savePath);
            UpdateCommandState();
            

            GC.Collect();
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void calcRaster_btn_Click(object sender, RoutedEventArgs e)
        {
            CalcRasterWindow dialog = new CalcRasterWindow();
            if (dialog.ShowDialog() == true)
            {
                bandPaths = dialog.BandPaths;
                path = dialog.path;
                bandNames[0] = (string)dialog.ch1_lbl.Content;
                bandNames[1] = (string)dialog.ch2_lbl.Content;
                bandNames[2] = (string)dialog.ch3_lbl.Content;
                calcRaster_btn.IsEnabled = false;
                Formula = dialog.Formula;
                satellite = dialog.sat;
                TryStartOperation("CalculateRaster");
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                UpdateCommandState();
                return;
            }

            
        }

        private void hist_fromFile_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (!string.IsNullOrEmpty(imageInfo.path))
            {
                string initialDirectory = Directory.Exists(imageInfo.path)
                    ? imageInfo.path
                    : System.IO.Path.GetDirectoryName(imageInfo.path);
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    ofd.InitialDirectory = initialDirectory;
            }
            ChooseSatelliteWindow chooseSatellite = new ChooseSatelliteWindow();
            
            if (ofd.ShowDialog() == true && chooseSatellite.ShowDialog() == true)
            {
                Histogram.MakeFromFile(ofd.FileName, chooseSatellite.camera);
                
            }
            else return;
        }

        private void openHDF_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (!string.IsNullOrEmpty(imageInfo.path))
            {
                string initialDirectory = Directory.Exists(imageInfo.path)
                    ? imageInfo.path
                    : System.IO.Path.GetDirectoryName(imageInfo.path);
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    ofd.InitialDirectory = initialDirectory;
            }
            if (ofd.ShowDialog() == true)
            {
                //var dataset = Gdal.Open(ofd.FileName, Access.GA_ReadOnly);
            }
            
        }

        private void CropImage_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureImageLoaded("Обрезку"))
                return;

            if (makingWrapper)
            {
                zoom_border.isActive = true;
                makingWrapper = false;
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;
                crop_btn.ClearValue(Button.BackgroundProperty);
                SetStatus("Готово");
            }
            else
            {
                zoom_border.isActive = false;
                makingWrapper = true;
                settingModel = false;
                setModel_btn.ClearValue(Button.BackgroundProperty);
                crop_btn.Background = System.Windows.Media.Brushes.PaleGreen;
                SetStatus("Выделите область изображения для обрезки.");
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureImageLoaded("Сохранение"))
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "TIFF (*.tif)|*.tif|PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg";
            sfd.FileName = "hsi_result.tif";
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    Cv2.ImWrite(sfd.FileName, imageInfo.GetMat());
                    SetStatus("Изображение сохранено: " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    UiDialogHelper.ShowError(this, "Не удалось сохранить изображение.", ex);
                }
            }
        }
    }
}
