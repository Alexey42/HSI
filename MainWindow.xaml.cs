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

        public MainWindow()
        {
            InitializeComponent();
            models = new List<Model>();
            imageInfo = new ImageInfo();
            wrapedArea = new int[4];
            backgroundWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
            scr_img_scale.ScaleX = 0.05;
            scr_img_scale.ScaleY = 0.05;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            addImage_btn.IsEnabled = false;
            ImageAddingWindow dialog = new ImageAddingWindow();
            if (dialog.ShowDialog() == true)
            {
                bandPaths = dialog.BandPaths;
                path = dialog.path;
                bandNames[0] = (string)dialog.ch1_lbl.Content;
                bandNames[1] = (string)dialog.ch2_lbl.Content;
                bandNames[2] = (string)dialog.ch3_lbl.Content;
                
                satellite = dialog.sat;

                backgroundWorker.RunWorkerAsync("AddImage");
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                addImage_btn.IsEnabled = true;
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

            StringBuilder sb = new StringBuilder();
            sb.Append("<Button x:Name=\"" + name + "Button\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x = \"http://schemas.microsoft.com/winfx/2006/xaml\"><StackPanel Orientation=\"Vertical\" Width=\"120\" Height=\"60\">" +
                        "<Label x:Name = \"" + name + "Name\" Padding=\"15 0\" Content = \"name\" Width = \"120\" RenderTransformOrigin = \"0.5,0.5\" Height = \"20\" FontSize = \"16\" />" +
                        "<Label x:Name = \"" + name + "UserColor\" Background = \"Blue\" Width = \"120\" RenderTransformOrigin = \"0.5,0.5\" Height = \"20\" />" +
                        "<Label x:Name = \"" + name + "Color\" Background = \"Red\" Width = \"120\" RenderTransformOrigin = \"0.5,0.5\" Height = \"20\" />" +
                    "</StackPanel></Button>");
            Button button = (Button)XamlReader.Parse(sb.ToString());
            button.Click += DeleteModel;
            modelsPanel.Children.Add(button);
            var finName = name + "Name";
            var btn = FindName("btn");
            var lbl = (Label)button.FindName(finName);
            lbl.Content = name;

            finName = name + "UserColor";
            lbl = (Label)button.FindName(finName);
            lbl.Background = new SolidColorBrush(model.userColor);

            finName = name + "Color";
            lbl = (Label)button.FindName(finName);
            lbl.Background = new SolidColorBrush(model.color);

        }

        void DeleteModel(object sender, RoutedEventArgs e)
        {
            var ui = (UIElement)sender;
            var elem = (Button)sender;
            models.Remove(models.Find((x) => { return x.name + "Button" == elem.Name; }));
            modelsPanel.Children.Remove(ui);
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

            ofd.InitialDirectory = "D:\\HSI_курсовая\\LC08_L2SP_175020_20201002_20201007_02_T1";
            if (ofd.ShowDialog() == true)
            {
                imageInfo = new ImageInfo(Cv2.ImRead(ofd.FileName), ofd.FileName);
                scr_img.Source = imageInfo.GetBI();
            }
        }

        private void ClearCanvas(object sender, RoutedEventArgs e)
        {
            imageInfo.Dispose();
            scr_img.Source = null;
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
                setModel_btn.Background = System.Windows.Media.Brushes.LightGray;
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

                int wrapedX = wrapedArea[2] - wrapedArea[0];
                int wrapedY = wrapedArea[3] - wrapedArea[1];
                int wrapedSize = wrapedX * wrapedY;
                if (wrapedSize == 0)
                    return;

                Vec3b[] wrapedBytes;
                int[] meanR = { 0, 0, 0 }, meanG = { 0, 0, 0 }, meanB = { 0, 0, 0 };
                Mat part = imageInfo.GetMat().SubMat(wrapedArea[1], wrapedArea[3], wrapedArea[0], wrapedArea[2]);
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

                if (settingModel)
                {
                    ModelAddingWindow dialog = new ModelAddingWindow();
                    if (dialog.ShowDialog() == true)
                    {
                        SetModel(meanR, meanG, meanB, dialog.Red, dialog.Green, dialog.Blue, dialog.ModelName.Replace(" ", "_"));
                    }
                    settingModel = false;
                }
                else
                {
                    VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
                    ofd.RootFolder = Environment.SpecialFolder.Recent;
                    if (ofd.ShowDialog() == true)
                    {
                        Mat res = new Mat(wrapedY, wrapedX, imageInfo.GetMat().Type(), wrapedBytes);
                        imageInfo.Dispose();
                        imageInfo = new ImageInfo(res, satellite, ofd.SelectedPath + "\\croped.tif", bandPaths, bandNames);
                        Cv2.ImWrite(ofd.SelectedPath + "\\croped.tif", res);
                        //OpenSaveHelper.SaveTifImage(savePath, res.Item2);
                        scr_img.Source = imageInfo.GetBI();
                        scr_img.UpdateLayout();
                    }
                    
                }
            }
        }

        private void classify_btn_Click(object sender, RoutedEventArgs e)
        {
            classify_btn.IsEnabled = false;
            ClassifyWindow dialog = new ClassifyWindow();
            if (dialog.ShowDialog() == true)
            {
                classifyMethod = dialog.Method;
                classifyThreshold = dialog.Threshold;
                backgroundWorker.RunWorkerAsync(classifyMethod);
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                classify_btn.IsEnabled = true;
                return;
            }
        }

        private void segment_btn_Click(object sender, RoutedEventArgs e)
        {
            segment_btn.IsEnabled = false;
            SegmentWindow dialog = new SegmentWindow();
            if (dialog.ShowDialog() == true)
            {
                segSigma = dialog.tr1;
                segK = dialog.tr2;
                segMin = dialog.tr3;
                backgroundWorker.RunWorkerAsync("GBSegmentationSpectralAngle");
            }
            else
            {
                segment_btn.IsEnabled = true;
                return;
            }
        }

        private void setModel_btn_Click(object sender, RoutedEventArgs e)
        {
            if (makingWrapper)
            {
                settingModel = false;
                setModel_btn.Background = System.Windows.Media.Brushes.LightGray;
                zoom_border.isActive = true;
                makingWrapper = false;
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;
            }
            else
            {
                settingModel = true;
                setModel_btn.Background = System.Windows.Media.Brushes.PaleGreen;
                zoom_border.isActive = false;
                makingWrapper = true;
            }
        }

        private void GetStatistic_btn(object sender, RoutedEventArgs e)
        {

        }

        private void EMD_btn_Click(object sender, RoutedEventArgs e)
        {
            EMD_btn.IsEnabled = false;
            EMDWindow dialog = new EMDWindow();
            if (dialog.ShowDialog() == true)
            {
                path = dialog.path;
                satellite = dialog.sat;
                backgroundWorker.RunWorkerAsync("EMD");
            }
            else
            {
                EMD_btn.IsEnabled = true;
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
                    //Mat m = new Mat("C:\\Users\\55000\\Downloads\\test.jpg");
                    Mat m = new Mat("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\croped.tif");
                    res = new Tuple<string, Mat>("GBSegmentationSpectralAngle", 
                        SegmentImage.SegmentAngle(imageInfo.GetMat(), segSigma, segK, segMin, out ccsNum, backgroundWorker));
                    e.Result = res;
                    break;
                case "EMD":
                    res = new Tuple<string, Mat>("EMD",
                        EMDImage.EMD(path, backgroundWorker, satellite));
                    e.Result = res;
                    break;
            }
            backgroundWorker.ReportProgress(0);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var res = (Tuple<string, Mat>)e.Result;
            if (res.Item2 == null) return;

            string savePath = "";

            if (res.Item1 == "ClassifyBarycentric" || res.Item1 == "ClassifyAngle" || res.Item1 == "ClassifyEuclid") {
                PrepareAndSaveStats("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\3.txt");
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\22.tif";
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
                classify_btn.IsEnabled = true;
            }
            if (res.Item1 == "AddImage")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\11.tif";
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
                addImage_btn.IsEnabled = true;
            }
            if (res.Item1 == "CalculateRaster")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\44.tif";
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
                calcRaster_btn.IsEnabled = true;
            }
            if (res.Item1 == "GBSegmentationSpectralAngle")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\5.tif";
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
                segment_btn.IsEnabled = true;
            }
            if (res.Item1 == "EMD")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\6.tif";
                imageInfo.Dispose();
                imageInfo = new ImageInfo(res.Item2, satellite, savePath, bandPaths, bandNames);
                EMD_btn.IsEnabled = true;
            }
            
            Cv2.ImWrite(savePath, res.Item2);
            //OpenSaveHelper.SaveTifImage(savePath, res.Item2);
            scr_img.Source = imageInfo.GetBI();
            scr_img.UpdateLayout();
            

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
                backgroundWorker.RunWorkerAsync("CalculateRaster");
                //scr_img_scale.ScaleX = 0.05;
                //scr_img_scale.ScaleY = 0.05;
            }
            else
            {
                calcRaster_btn.IsEnabled = true;
                return;
            }

            
        }

        private void hist_fromFile_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (imageInfo.path != "")
                ofd.InitialDirectory = imageInfo.path;
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
            if (imageInfo.path != "")
                ofd.InitialDirectory = imageInfo.path;
            if (ofd.ShowDialog() == true)
            {
                //var dataset = Gdal.Open(ofd.FileName, Access.GA_ReadOnly);
            }
            
        }

        private void CropImage_Click(object sender, RoutedEventArgs e)
        {
            if (makingWrapper)
            {
                zoom_border.isActive = true;
                makingWrapper = false;
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;
            }
            else
            {
                zoom_border.isActive = false;
                makingWrapper = true;
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
