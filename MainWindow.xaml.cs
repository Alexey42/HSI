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
        Bitmap bitmap;
        byte[] bitmapBytes;
        bool makingWrapper = false;
        int[] wrapedArea;
        System.Windows.Point mouseStart;
        ImageInfo imageInfo;
        List<Model> models;
        BackgroundWorker backgroundWorker;
        string Formula = "";
        Satellite sat;

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
                imageInfo.bandPaths = dialog.BandPaths;
                imageInfo.path = dialog.path;
                imageInfo.bandNames[0] = (string)dialog.ch1_lbl.Content;
                imageInfo.bandNames[1] = (string)dialog.ch2_lbl.Content;
                imageInfo.bandNames[2] = (string)dialog.ch3_lbl.Content;
                addImage_btn.IsEnabled = false;
                sat = dialog.sat;
            }
            else
                return;

            scr_img_scale.ScaleX = 0.05;
            scr_img_scale.ScaleY = 0.05;
            backgroundWorker.RunWorkerAsync("AddImage");
        }

        byte[] AddImage()
        {
            int numOfBands = 3;
            BitmapSource[] bands = new BitmapSource[numOfBands];

            Parallel.For(0, numOfBands, (i) => {
                bands[i] = BandToBitmap_TIF(imageInfo.bandPaths[i]);
            });

            double dpi = bands[0].DpiX;
            int width = bands[0].PixelWidth;
            int height = bands[0].PixelHeight;
            int bytesPerPixel = (bands[0].Format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int arrayLength = stride * height;
            backgroundWorker.ReportProgress(10);

            List<byte[]> pixelArrays = new List<byte[]>();
            for (int i = 0; i < numOfBands; i++)
                pixelArrays.Add(new byte[arrayLength]);

            Parallel.For(0, numOfBands, (i) => {
                bands[i].CopyPixels(pixelArrays[i], stride, 0);
            });
            backgroundWorker.ReportProgress(60);
            //byte[] tiffArray = System.IO.File.ReadAllBytes(imagePath1);     
            //System.IO.File.WriteAllBytes(ofd.InitialDirectory + "\\TiffFile.tif", tiffArray);

            bytesPerPixel = 6;
            stride = width * bytesPerPixel;
            arrayLength = stride * height;
            bitmapBytes = new byte[arrayLength];

            for (int i = 0; i < arrayLength * 2 / bytesPerPixel - bytesPerPixel; i += 2)
            {
                int index1 = i / 2 * bytesPerPixel;

                if (pixelArrays[0][i + 1] > 0)
                {
                    imageInfo.hist1[(byte)(pixelArrays[0][i + 1] * 2.2)]++;
                    bitmapBytes[index1 + 1] = (byte)(pixelArrays[0][i + 1] * 2.2);
                }
                if (pixelArrays[1][i + 1] > 0)
                {
                    imageInfo.hist2[(byte)(pixelArrays[1][i + 1] * 2.2)]++;
                    bitmapBytes[index1 + 3] = (byte)(pixelArrays[1][i + 1] * 2.2);
                }
                if (pixelArrays[2][i + 1] > 0)
                {
                    imageInfo.hist3[(byte)(pixelArrays[2][i + 1] * 2.2)]++;
                    bitmapBytes[index1 + 5] = (byte)(pixelArrays[2][i + 1] * 2.2);
                }
            }

            Histogram.Make(256, 300, imageInfo.hist1, Histogram.ColorFromBandName(imageInfo.bandNames[0]))
                .Save("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\Hist" + imageInfo.bandNames[0] + ".jpeg", ImageFormat.Jpeg);
            Histogram.Make(256, 300, imageInfo.hist2, Histogram.ColorFromBandName(imageInfo.bandNames[1]))
                .Save("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\Hist" + imageInfo.bandNames[1] + ".jpeg", ImageFormat.Jpeg);
            Histogram.Make(256, 300, imageInfo.hist3, Histogram.ColorFromBandName(imageInfo.bandNames[2]))
                .Save("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\Hist" + imageInfo.bandNames[2] + ".jpeg", ImageFormat.Jpeg);

            backgroundWorker.ReportProgress(100);
            imageInfo.SetValues(bitmapBytes, width, height, dpi, bytesPerPixel, stride);

            return bitmapBytes;
        }

        BitmapSource BandToBitmap_TIF(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            TiffBitmapDecoder decoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        System.Windows.Media.Color GetRandomColor()
        {
            var randomBytes = new Byte[3];
            new Random().NextBytes(randomBytes);
            return System.Windows.Media.Color.FromRgb(randomBytes[0], randomBytes[1], randomBytes[2]);
        }

        BitmapSource SaveImage(bool visualize, string path, int width, int height, double dpiX, double dpiY, System.Windows.Media.PixelFormat format, byte[] pixels, int stride)
        {
            BitmapSource result;
            using (FileStream str = new FileStream(path, FileMode.OpenOrCreate))
            {
                /*List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>
                {
                    Colors.Red,
                    Colors.Green,
                    Colors.Blue
                };
                BitmapPalette myPalette = new BitmapPalette(colors);*/
                result = BitmapSource.Create(width, height, dpiX, dpiY, format, null, pixels, stride);
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(result));
                encoder.Compression = TiffCompressOption.None;
                encoder.Save(str);

                if (visualize)
                    bitmap = new Bitmap(str);
            }

            return result;
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

        private void SetModel(int[] maxR, int[] maxG, int[] maxB, byte usersRed, byte usersGreen, byte usersBlue, string name)
        {
            Model model = new Model();
            model.data = new int[,] {
                { maxR[0], maxR[1], maxR[2] },
                { maxG[0], maxG[1], maxG[2] },
                { maxB[0], maxB[1], maxB[2] }
            };
            model.color = System.Windows.Media.Color.FromRgb((byte)maxR[0], (byte)maxG[1], (byte)maxB[2]);
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
            var t = name + "Name";
            var btn = FindName("btn");
            var lbl = (Label)button.FindName(t);
            lbl.Content = name;

            t = name + "UserColor";
            lbl = (Label)button.FindName(t);
            lbl.Background = new SolidColorBrush(model.userColor);

            t = name + "Color";
            lbl = (Label)button.FindName(t);
            lbl.Background = new SolidColorBrush(model.color);

        }

        void DeleteModel(object sender, RoutedEventArgs e)
        {
            var ui = (UIElement)sender;
            var elem = (Button)sender;
            models.Remove(models.Find((x) => { return x.name + "Button" == elem.Name; }));
            modelsPanel.Children.Remove(ui);
        }

        byte[] ClassifyBarycentric()
        {
            if (models.Count < 1) return null;

            int[] coverCount = new int[models.Count];
            /*int X1 = maxR[0], Y1 = maxR[1], Z1 = maxR[2];
            int X2 = maxG[0], Y2 = maxG[1], Z2 = maxG[2];
            int X3 = maxB[0], Y3 = maxB[1], Z3 = maxB[2];*/
            List<Matrix> inverted_matrix = new List<Matrix>();
            foreach (var model in models)
            {
                Matrix m = new Matrix(3, 3);
                m[0, 0] = model.data[0, 0]; m[0, 1] = model.data[1, 0]; m[0, 2] = model.data[2, 0];
                m[1, 0] = model.data[0, 1]; m[1, 1] = model.data[1, 1]; m[1, 2] = model.data[2, 1];
                m[2, 0] = model.data[0, 2]; m[2, 1] = model.data[1, 2]; m[2, 2] = model.data[2, 2];

                Matrix im = m.CreateInvertibleMatrix();
                if (im == null)
                {
                    m[1, 0]++;
                    m[0, 2]++;
                    m[2, 1]++;
                    im = m.CreateInvertibleMatrix();
                }
                inverted_matrix.Add(im);
            }

            int progress = 0;
            Parallel.For(0, bitmapBytes.Length / 6, (i) =>
            {
                i *= 6;
                if (i + 5 < bitmapBytes.Length)
                {
                    /*int X = bitmapBytes[i + 1];
                    int Y = bitmapBytes[i + 3];
                    int Z = bitmapBytes[i + 5];*/
                    if (bitmapBytes[i + 1] != 0 || bitmapBytes[i + 3] != 0 || bitmapBytes[i + 5] != 0)
                    {
                        Matrix r = new Matrix(3, 1);
                        r[0, 0] = bitmapBytes[i + 1];
                        r[1, 0] = bitmapBytes[i + 3];
                        r[2, 0] = bitmapBytes[i + 5];

                        for (int k = 0; k < models.Count; k++)
                        {
                            Matrix res = inverted_matrix[k] * r;
                            if (/*(res[0, 0] >= 0 || res[1, 0] >= 0 || res[2, 0] >= 0) &&*/
                                (Math.Abs(res[0, 0]) < 8 && Math.Abs(res[1, 0]) < 8 && Math.Abs(res[2, 0]) < 8))
                            {
                                coverCount[k]++;
                                bitmapBytes[i + 1] = models[k].userColor.R;
                                bitmapBytes[i + 3] = models[k].userColor.G;
                                bitmapBytes[i + 5] = models[k].userColor.B;
                                break;
                            }
                        }
                    }
                    progress++;
                    if (progress % (bitmapBytes.Length / 600) == 0)
                        backgroundWorker.ReportProgress(progress / (bitmapBytes.Length / 600));
                }
            });
            backgroundWorker.ReportProgress(100);

            var resolution = sat.GetResolution();
            for (int i = 0; i < models.Count; i++)
            {
                models[i].coverPixels = coverCount[i];
                models[i].coverMetres = Convert.ToInt64(coverCount[i] * resolution * resolution);
                models[i].coverPercentage = coverCount[i] * 100d / (bitmapBytes.Length / 6d);
            }

            imageInfo.processedBytes = bitmapBytes;

            return bitmapBytes;
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
            if (type == ".jpeg")
                ofd.Filter = "HSI | *.jpg;*.jpeg";
                
            ofd.InitialDirectory = "D:\\HSI_курсовая\\LC08_L2SP_175020_20201002_20201007_02_T1";
            if (ofd.ShowDialog() == true)
            {
                BitmapFrame frame = null;
                if (type == ".tif")
                {
                    FileStream stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    BitmapDecoder decoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    frame = decoder.Frames[0];
                }
                if (type == ".jpeg")
                {
                    FileStream stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    BitmapDecoder decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    frame = decoder.Frames[0];
                }
                var stride = (int)frame.Width * (frame.Format.BitsPerPixel + 7) / 8;
                scr_img.Source = frame;
                bitmapBytes = new byte[(int)frame.Height * stride];
                frame.CopyPixels(bitmapBytes, stride, 0);
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

                byte[] wrapedBytes = new byte[wrapedSize * 3];
                int c = 0;
                int[] maxR = { 0, 0, 0 }, maxG = { 0, 0, 0 }, maxB = { 0, 0, 0 };
                long totalR = 0, totalG = 0, totalB = 0;

                for (int k = 0; k < wrapedY; k++)
                {
                    int start = (wrapedArea[0] + (wrapedArea[1] + k) * bitmap.Width) * 6;
                    for (int i = start + 1; i < start + wrapedX * 6; i += 2)
                    {
                        wrapedBytes[c] = imageInfo.processedBytes[i];
                        c++;
                    }
                }

                for (int i = 0; i < c - 3; i += 3)
                {
                    totalR += wrapedBytes[i];
                    totalG += wrapedBytes[i + 1];
                    totalB += wrapedBytes[i + 2];
                }
                int averageR = (int)(totalR / (c / 3));
                int averageG = (int)(totalG / (c / 3));
                int averageB = (int)(totalB / (c / 3));
                for (int i = 0; i < c - 3;)
                {
                    if (Math.Abs(maxR[0] - averageR) > Math.Abs(wrapedBytes[i] - averageR))
                    {
                        maxR[0] = wrapedBytes[i];
                        maxR[1] = wrapedBytes[i + 1];
                        maxR[2] = wrapedBytes[i + 2];
                    }
                    i++;
                    if (Math.Abs(maxG[1] - averageG) > Math.Abs(wrapedBytes[i] - averageG))
                    {
                        maxG[0] = wrapedBytes[i - 1];
                        maxG[1] = wrapedBytes[i];
                        maxG[2] = wrapedBytes[i + 1];
                    }
                    i++;
                    if (Math.Abs(maxB[2] - averageB) > Math.Abs(wrapedBytes[i] - averageB))
                    {
                        maxB[0] = wrapedBytes[i - 2];
                        maxB[1] = wrapedBytes[i - 1];
                        maxB[2] = wrapedBytes[i];
                    }
                    i++;
                }

                ModelAdding dialog = new ModelAdding();
                if (dialog.ShowDialog() == true)
                {
                    SetModel(maxR, maxG, maxB, dialog.Red, dialog.Green, dialog.Blue, dialog.ModelName.Replace(" ", "_"));
                }
                else
                    return;
            }
        }

        private void classify_btn_Click(object sender, RoutedEventArgs e)
        {
            classify_btn.IsEnabled = false;
            backgroundWorker.RunWorkerAsync("ClassifyBarycentric");

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
            Tuple<string, byte[]> res;

            switch ((string)e.Argument)
            {
                case "ClassifyBarycentric":
                    res = new Tuple<string, byte[]>("ClassifyBarycentric", ClassifyBarycentric());
                    e.Result = res;
                    break;
                case "AddImage":
                    res = new Tuple<string, byte[]>("AddImage", AddImage());
                    e.Result = res;
                    break;
                case "CalculateRaster":
                    res = new Tuple<string, byte[]>("CalculateRaster", CalculateRaster());
                    e.Result = res;
                    break;
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var res = (Tuple<string, byte[]>)e.Result;

            switch (res.Item1)
            {
                case "ClassifyBarycentric":
                    PrepareAndSaveStats("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\3.txt");
                    scr_img.Source = SaveImage(true, "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\2.tif", imageInfo.width, imageInfo.height, imageInfo.dpi, imageInfo.dpi, PixelFormats.Rgb48, res.Item2, 6 * imageInfo.width);
                    classify_btn.IsEnabled = true;
                    break;
                case "AddImage":
                    scr_img.Source = SaveImage(true, "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\1.tif", imageInfo.width, imageInfo.height, imageInfo.dpi, imageInfo.dpi, PixelFormats.Rgb48, res.Item2, imageInfo.stride);
                    addImage_btn.IsEnabled = true;
                    break;
                case "CalculateRaster":
                    scr_img.Source = SaveImage(true, "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1\\4.tif", imageInfo.width, imageInfo.height, imageInfo.dpi, imageInfo.dpi, PixelFormats.Rgb48, res.Item2, imageInfo.stride);
                    calcRaster_btn.IsEnabled = true;
                    break;
            }


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
                imageInfo.bandPaths = dialog.BandPaths;
                imageInfo.path = dialog.path;
                imageInfo.bandNames[0] = (string)dialog.ch1_lbl.Content;
                imageInfo.bandNames[1] = (string)dialog.ch2_lbl.Content;
                imageInfo.bandNames[2] = (string)dialog.ch3_lbl.Content;
                calcRaster_btn.IsEnabled = false;
                Formula = dialog.Formula;
                sat = dialog.sat;
            }
            else
                return;

            backgroundWorker.RunWorkerAsync("CalculateRaster");
        }

        byte[] CalculateRaster()
        {
            int progress = 0;
            int numOfBands = 3;
            BitmapSource[] bands = new BitmapSource[numOfBands];

            Parallel.For(0, numOfBands, (i) => {
                bands[i] = BandToBitmap_TIF(imageInfo.bandPaths[i]);
            });

            double dpi = bands[0].DpiX;
            int width = bands[0].PixelWidth;
            int height = bands[0].PixelHeight;
            int bytesPerPixel = (bands[0].Format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int arrayLength = stride * height;

            List<byte[]> pixelArrays = new List<byte[]>();
            for (int i = 0; i < numOfBands; i++)
                pixelArrays.Add(new byte[arrayLength]);

            Parallel.For(0, numOfBands, (i) => {
                bands[i].CopyPixels(pixelArrays[i], stride, 0);
            });

            bytesPerPixel = 6;
            stride = width * bytesPerPixel;
            arrayLength = stride * height;
            var bitmapBytes = new byte[arrayLength];

            Formula = Formula.Replace("Ch1", "x").Replace("Ch2", "y").Replace("Ch3", "z");
            var comp = new RPNExpression(Formula);
            var RPNString = comp.Prepare();
            if (bitmapBytes == null)
                bitmapBytes = new byte[arrayLength];

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //Parallel.For(0, arrayLength * 2 / bytesPerPixel - bytesPerPixel, (i) =>
            /*for (int i = 0; i < arrayLength * 2 / bytesPerPixel - bytesPerPixel; i++)
            {
                if (pixelArrays[0][i] + pixelArrays[1][i] + pixelArrays[2][i] != 0)
                {
                    int index1 = i / 2 * bytesPerPixel;
                    List<RPNArguments> arguments = new List<RPNArguments>() {
                        new RPNArguments("x", pixelArrays[0][i]),
                        new RPNArguments("y", pixelArrays[1][i]),
                        new RPNArguments("z", pixelArrays[2][i]) };
                    double temp = (double)comp.Calculate(arguments);
                    bitmapBytes[index1 + 1] = (byte)((1 + temp) * 200);
                    bitmapBytes[index1 + 3] = (byte)((1 - temp) * 200);
                    bitmapBytes[index1 + 5] = 0;//(byte)Math.Abs(temp * 200);
                }

                progress++;
                if (progress % ((arrayLength * 2 / bytesPerPixel - bytesPerPixel) / 100) == 0)
                    backgroundWorker.ReportProgress(progress / ((arrayLength * 2 / bytesPerPixel - bytesPerPixel) / 100));
            }*///);
            sw.Stop();
            var time = sw.Elapsed.TotalSeconds;
            for (int i = 0; i < arrayLength * 2 / bytesPerPixel - bytesPerPixel; i++)
            {
                if (pixelArrays[0][i] + pixelArrays[1][i] == 0)
                    continue;
                int index1 = i / 2 * bytesPerPixel;
                
                double m = pixelArrays[0][i];
                double n = pixelArrays[1][i];              
                double temp = (m - n) / (m + n);
                bitmapBytes[index1 + 1] = (byte)((1 + temp) * 200);
                bitmapBytes[index1 + 3] = (byte)((1 - temp) * 200);
                bitmapBytes[index1 + 5] = 0;//(byte)Math.Abs(temp * 200);

                progress++;
                if (progress % ((arrayLength * 2 / bytesPerPixel - bytesPerPixel) / 100) == 0)
                    backgroundWorker.ReportProgress(progress / ((arrayLength * 2 / bytesPerPixel - bytesPerPixel) / 100));
            }

            backgroundWorker.ReportProgress(100);
            imageInfo.SetValues(bitmapBytes, width, height, dpi, bytesPerPixel, stride);

            return bitmapBytes;
        }

        private void hist_fromFile_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (imageInfo.path != "")
                ofd.InitialDirectory = imageInfo.path;

            if (ofd.ShowDialog() == true)
            {
                Histogram.MakeFromFile(ofd.FileName, 256, 300);

            }
            else return;
        }

    }
}
