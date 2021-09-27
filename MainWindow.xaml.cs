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
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
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
using com.sgcombo.RpnLib;
using HSI.SatelliteInfo;

namespace HSI
{

    public partial class MainWindow : System.Windows.Window
    {
        bool makingWrapper = false;
        int[] wrapedArea;
        System.Windows.Point mouseStart;
        ImageInfo imageInfo;
        List<Model> models;
        BackgroundWorker backgroundWorker;
        string Formula = "";
        float classifyThreshold = 0;
        public Satellite satellite;
        string path;
        public string[] bandPaths = new string[3];
        public string[] bandNames = new string[3];

        public MainWindow()
        {
            InitializeComponent();
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();
            models = new List<Model>();
            imageInfo = new ImageInfo();
            wrapedArea = new int[4];
            backgroundWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ImageAdding dialog = new ImageAdding();
            if (dialog.ShowDialog() == true)
            {
                bandPaths = dialog.BandPaths;
                path = dialog.path;
                bandNames[0] = (string)dialog.ch1_lbl.Content;
                bandNames[1] = (string)dialog.ch2_lbl.Content;
                bandNames[2] = (string)dialog.ch3_lbl.Content;
                addImage_btn.IsEnabled = false;
                
                satellite = dialog.sat;
            }
            else
                return;

            scr_img_scale.ScaleX = 0.05;
            scr_img_scale.ScaleY = 0.05;
            backgroundWorker.RunWorkerAsync("AddImage");
        }

        Vec3b[] AddImage()
        {
            Mat band = null;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                band = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                band.GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            backgroundWorker.ReportProgress(50);

            Vec3b[] vecs = new Vec3b[arrayLength];
            for (int i = 0; i < arrayLength; i++)
                vecs[i] = new Vec3b((byte)(bytes[2][i] * satellite.brightCoef), (byte)(bytes[1][i] * satellite.brightCoef),
                    (byte)(bytes[0][i] * satellite.brightCoef));
            Mat res = new Mat(band.Rows, band.Cols, MatType.CV_8UC3, vecs);
            bytes = null;
       
            backgroundWorker.ReportProgress(100);
            imageInfo = new ImageInfo(res, satellite, path, bandPaths, bandNames);

            return vecs;
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

        Vec3b[] ClassifyBarycentric()
        {
            if (models.Count == 0) return null;

            int[] coverCount = new int[models.Count];
            List<Matrix> inverted_matrix = new List<Matrix>();
            foreach (var model in models)
            {
                Matrix m = new Matrix(3, 3);
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        m[i, j] = model.data[j, i];

                Matrix im = m.CreateInvertibleMatrix();
                if (im == null)
                {
                    m[1, 0]++; m[0, 2]++; m[2, 1]++;
                    im = m.CreateInvertibleMatrix();
                }
                inverted_matrix.Add(im);
            }

            int progress = 0;
            Vec3b[] array = imageInfo.GetBytes();
            
            Parallel.For(0, array.Length, (i) =>
            {
                if (array[i][0] != 0 || array[i][1] != 0 || array[i][2] != 0)
                {
                    Matrix r = new Matrix(3, 1);
                    r[0, 0] = array[i][0]; r[1, 0] = array[i][1]; r[2, 0] = array[i][2];

                    for (int k = 0; k < models.Count; k++)
                    {
                        Matrix res = inverted_matrix[k] * r;
                        if (Math.Abs(res[0, 0]) < classifyThreshold && Math.Abs(res[1, 0]) < classifyThreshold 
                            && Math.Abs(res[2, 0]) < classifyThreshold)
                        {
                            coverCount[k]++;
                            array[i] = new Vec3b(models[k].userColor.B, models[k].userColor.G, models[k].userColor.R);
                            break;
                        }
                    }
                }
                progress++;
                if (progress % (array.Length / 100) == 0)
                    backgroundWorker.ReportProgress(progress / array.Length / 100);
            });
            backgroundWorker.ReportProgress(100);
            Mat mat = new Mat(imageInfo.height, imageInfo.width, MatType.CV_8UC3, array);
            //Cv2.ImWrite("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\22.tif", mat);
            imageInfo = new ImageInfo(mat, satellite, path, bandPaths, bandNames);
            var resolution = imageInfo.satellite.GetResolution("");
            for (int i = 0; i < models.Count; i++)
            {
                models[i].coverPixels = coverCount[i];
                models[i].coverMetres = Convert.ToInt64(coverCount[i] * resolution * resolution);
                models[i].coverPercentage = coverCount[i] * 100d / array.Length;
            }

            return array;
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
                scr_img.Source = imageInfo.GetBS();
            }
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
                    if (Math.Abs(meanR[0] - mean[2]) > Math.Abs(wrapedBytes[i][0] - mean[2]))
                    {
                        meanR[0] = wrapedBytes[i][0];
                        meanR[1] = wrapedBytes[i][1];
                        meanR[2] = wrapedBytes[i][2];
                    }
                    if (Math.Abs(meanG[1] - mean[1]) > Math.Abs(wrapedBytes[i][1] - mean[1]))
                    {
                        meanG[0] = wrapedBytes[i][0];
                        meanG[1] = wrapedBytes[i][1];
                        meanG[2] = wrapedBytes[i][2];
                    }
                    if (Math.Abs(meanB[2] - mean[0]) > Math.Abs(wrapedBytes[i][2] - mean[0]))
                    {
                        meanB[0] = wrapedBytes[i][0];
                        meanB[1] = wrapedBytes[i][1];
                        meanB[2] = wrapedBytes[i][2];
                    }
                }

                ModelAdding dialog = new ModelAdding();
                if (dialog.ShowDialog() == true)
                {
                    SetModel(meanR, meanG, meanB, dialog.Red, dialog.Green, dialog.Blue, dialog.ModelName.Replace(" ", "_"));
                }
                else
                    return;
            }
        }

        private void classify_btn_Click(object sender, RoutedEventArgs e)
        {
            classify_btn.IsEnabled = false;
            Classify dialog = new Classify();
            if (dialog.ShowDialog() == true)
            {
                classifyThreshold = dialog.Threshold;
                backgroundWorker.RunWorkerAsync("ClassifyBarycentric");
            }
            else
                return;
        }

        private void setModel_btn_Click(object sender, RoutedEventArgs e)
        {

            if (makingWrapper)
            {
                setModel_btn.Background = System.Windows.Media.Brushes.LightGray;
                zoom_border.isActive = true;
                makingWrapper = false;
                rect_for_wrap.Width = 0;
                rect_for_wrap.Height = 0;
            }
            else
            {
                setModel_btn.Background = System.Windows.Media.Brushes.PaleGreen;
                zoom_border.isActive = false;
                makingWrapper = true;
            }
        }

        private void GetStatistic_btn(object sender, RoutedEventArgs e)
        {

        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Tuple<string, Vec3b[]> res;

            switch ((string)e.Argument)
            {
                case "ClassifyBarycentric":
                    res = new Tuple<string, Vec3b[]>("ClassifyBarycentric", ClassifyBarycentric());
                    e.Result = res;
                    break;
                case "AddImage":
                    res = new Tuple<string, Vec3b[]>("AddImage", AddImage());
                    e.Result = res;
                    break;
                case "CalculateRaster":
                    res = new Tuple<string, Vec3b[]>("CalculateRaster", CalculateRaster());
                    e.Result = res;
                    break;
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var res = (Tuple<string, Vec3b[]>)e.Result;
            if (res.Item2 == null) return;

            Tuple<BitmapSource, FileStream> obj = null;
            string savePath = "";

            if (res.Item1 == "ClassifyBarycentric") {
                PrepareAndSaveStats("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\3.txt");
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\22.tif";
                classify_btn.IsEnabled = true;
            }
            if (res.Item1 == "AddImage")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\11.tif";
                addImage_btn.IsEnabled = true;
            }
            if (res.Item1 == "CalculateRaster")
            {
                savePath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\44.tif";
                calcRaster_btn.IsEnabled = true;
            }

            Cv2.ImWrite(savePath, imageInfo.GetMat());
            scr_img.Source = imageInfo.GetBS();
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void calcRaster_btn_Click(object sender, RoutedEventArgs e)
        {
            CalcRaster dialog = new CalcRaster();
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
            }
            else
                return;

            backgroundWorker.RunWorkerAsync("CalculateRaster");
        }

        Vec3b[] CalculateRaster()
        {
            int progress = 0;
            Mat band = null;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                band = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                band.GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            Formula = Formula.Replace("Ch1", "x").Replace("Ch2", "y").Replace("Ch3", "z");
            var comp = new RPNExpression(Formula);
            var RPNString = comp.Prepare();
            Vec3b[] vecs = new Vec3b[arrayLength];

            Stopwatch sw = new Stopwatch();
            sw.Start();
            /*Parallel.For(0, arrayLength, (i) =>
            {
                if (bytes[0][i] + bytes[1][i] + bytes[2][i] != 0)
                {
                    List<RPNArguments> arguments = new List<RPNArguments>() {
                        new RPNArguments("x", bytes[0][i]),
                        new RPNArguments("y", bytes[1][i]),
                        new RPNArguments("z", bytes[2][i]) };
                    double temp = (double)comp.Calculate(arguments);
                    vecs[i][2] = (byte)((1 + temp) * 200);
                    vecs[i][1] = (byte)((1 - temp) * 200);
                    vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);
                }

                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            });*/
            sw.Stop();
            var time = sw.Elapsed.TotalSeconds;
            for (int i = 0; i < arrayLength; i++)
            {
                if (bytes[0][i] + bytes[1][i] == 0)
                    continue;
                
                double m = bytes[0][i];
                double n = bytes[1][i];
                double temp = (m - n) / (m + n);
                vecs[i][2] = (byte)((1 + temp) * 200);
                vecs[i][1] = (byte)((1 - temp) * 200);
                vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);

                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            }

            imageInfo = new ImageInfo(new Mat(band.Rows, band.Cols, MatType.CV_8UC3, vecs), satellite, path, bandPaths, bandNames);

            backgroundWorker.ReportProgress(100);

            return vecs;
        }

        private void hist_fromFile_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (imageInfo.path != "")
                ofd.InitialDirectory = imageInfo.path;
            ChooseSatellite chooseSatellite = new ChooseSatellite();
            
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
            
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
