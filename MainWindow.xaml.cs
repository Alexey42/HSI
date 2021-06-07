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
using System.Runtime.InteropServices;

namespace HSI
{

    public partial class MainWindow : Window
    {
        WriteableBitmap wb;
        Bitmap b;

        public MainWindow()
        {
            InitializeComponent();
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int numOfBands = 3;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "HSI | *.tif;*.tiff";
            ofd.InitialDirectory = "C:\\Users\\55000\\Downloads\\LC08_L2SP_174021_20200621_20200823_02_T1";
            ofd.Multiselect = true;
            //if (ofd.ShowDialog() == true)
            //{
            XmlDocument xmldoc = new XmlDocument();
            string infoPath = "";
            string[] bandPaths = new string[numOfBands];
            var directory = Directory.GetFiles(ofd.InitialDirectory);
            foreach (var x in directory)
            {
                if (x.ToLower().EndsWith("mtl.xml")) infoPath = x; //xmldoc.Load(x);
                if (x.ElementAt(x.Length - 5) == '6') bandPaths[0] = x;
                if (x.ElementAt(x.Length - 5) == '4') bandPaths[1] = x;
                if (x.ElementAt(x.Length - 5) == '2') bandPaths[2] = x;
            }

            XmlReader xml = XmlReader.Create(infoPath);

            BitmapSource[] bands = new BitmapSource[numOfBands];
            Parallel.For(0, numOfBands, (i) => {
                bands[i] = BandToBitmap_TIF(bandPaths[i]);
            });

            int width = bands[0].PixelWidth;
            int height = bands[0].PixelHeight;
            int bytesPerPixel = (bands[0].Format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int arrayLength = stride * height;

            byte[] arr1 = new byte[arrayLength];
            byte[] arr2 = new byte[arrayLength];
            byte[] arr3 = new byte[arrayLength];
            //byte[] tiffArray = System.IO.File.ReadAllBytes(imagePath1);     
            //System.IO.File.WriteAllBytes(ofd.InitialDirectory + "\\TiffFile.tif", tiffArray);
            bitmap1.CopyPixels(arr1, stride, 0);
            bitmap2.CopyPixels(arr2, stride, 0);
            bitmap3.CopyPixels(arr3, stride, 0);
            
            bytesPerPixel = 6;
            stride = width * bytesPerPixel;
            arrayLength = stride * height;
            var res = new byte[arrayLength];

            Parallel.ForEach(Enumerable.Range(0, arrayLength * 2 / bytesPerPixel - bytesPerPixel).Select(i => i += 2), i =>
            {               
                int index1 = i / 2 * bytesPerPixel; 
                int index2 = i + 1 == arr1.Length? i : i + 1; 

                res[index1] = arr1[index2];
                res[index1 + 1] = arr1[index2 + 1];
                res[index1 + 2] = arr2[index2];
                res[index1 + 3] = arr2[index2 + 1];
                res[index1 + 4] = arr3[index2];
                res[index1 + 5] = arr3[index2 + 1];
            });

            using (FileStream str = new FileStream(ofd.InitialDirectory + "\\1.tif", FileMode.OpenOrCreate))
            {
                List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>
                {
                    Colors.Red,
                    Colors.Green,
                    Colors.Blue
                };

                BitmapPalette myPalette = new BitmapPalette(colors);
                var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb48, null, res, stride);
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(result));
                encoder.Save(str);
                img.Source = result;
                
                //result.Dispose();
            }
            //FileStream band1str = new FileStream(ofd.InitialDirectory + "\\1.tif", FileMode.Open, FileAccess.Read, FileShare.Read);
            //TiffBitmapDecoder dec = new TiffBitmapDecoder(band1str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            //var band1src = dec.Frames[0];
            //img.Source = band1src;
        }

        BitmapSource BandToBitmap_TIF(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            TiffBitmapDecoder decoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "HSI | *.tif;*.tiff";
            ofd.InitialDirectory = "D:\\HSI_курсовая\\LC08_L2SP_175020_20201002_20201007_02_T1";
            if (ofd.ShowDialog() == true)
            {
                Stream src = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var d = new TiffBitmapDecoder(src, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bms = d.Frames[0];
                b = new Bitmap(ofd.FileName);
                wb = new WriteableBitmap(bms);
                
                img.Source = wb;
            }
        }

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement element = (IInputElement)sender;
            int x = (int)e.GetPosition(element).X;
            int y = (int)e.GetPosition(element).Y;
            //Int32Rect rect = new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight);

            
            int bytesPerPixel = (wb.Format.BitsPerPixel + 7) / 8;
            int stride = wb.BackBufferStride;
            int arrayLength = stride * wb.PixelHeight;
            byte[] originalImage = new byte[arrayLength];
            wb.CopyPixels(originalImage, stride, 0);
            var c = b.GetPixel(x, y);
            //label1.Content = originalImage[0] + "," + originalImage[1] + "," + originalImage[2] + "," + originalImage[3];
        }

        private void scr_img_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var im = sender as System.Windows.Controls.Image;
            var st = im.RenderTransform as ScaleTransform;
            double zoom = e.Delta > 0 ? 0.2 : -0.2;
            st.ScaleX += zoom;
            st.ScaleY += zoom;
        }
    }
}
