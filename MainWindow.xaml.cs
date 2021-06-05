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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "HSI | *.tif;*.tiff";
            ofd.InitialDirectory = "D:\\HSI_images\\LC08_L2SP_175020_20201002_20201007_02_T1";
            ofd.Multiselect = true;
            //if (ofd.ShowDialog() == true)
            //{
                string imagePath1 = "", imagePath2 = "", imagePath3 = "";
                var directory = Directory.GetFiles(ofd.InitialDirectory);
                foreach (var x in directory)
                {
                    if (x.ElementAt(x.Length - 5) == '5') imagePath3 = x;
                    if (x.ElementAt(x.Length - 5) == '4') imagePath2 = x;
                    if (x.ElementAt(x.Length - 5) == '3') imagePath1 = x;
                }
                Stream band1str = null;
                Stream band2str = null;
                Stream band3str = null;
                //Parallel.Invoke(()=>
                //{
                //    band1str = new FileStream(ofd.FileNames[0], FileMode.Open, FileAccess.Read, FileShare.Read);
                //}, () =>
                //{
                //    band2str = new FileStream(ofd.FileNames[1], FileMode.Open, FileAccess.Read, FileShare.Read);
                //}, () =>
                //{
                //    band3str = new FileStream(ofd.FileNames[2], FileMode.Open, FileAccess.Read, FileShare.Read);
                //});
                //TiffBitmapDecoder decoder;
                //BitmapSource band1src = null;
                //BitmapSource band2src = null;
                //BitmapSource band3src = null;               
                
                //Parallel.Invoke(() =>
                //{
                //    band1str = new FileStream(ofd.FileNames[0], FileMode.Open, FileAccess.Read, FileShare.Read);
                //    decoder = new TiffBitmapDecoder(band1str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                //    band1src = decoder.Frames[0];  
                //}, () =>
                //{
                //    band2str = new FileStream(ofd.FileNames[1], FileMode.Open, FileAccess.Read, FileShare.Read);
                //    decoder = new TiffBitmapDecoder(band2str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                //    band2src = decoder.Frames[0];                  
                //}, () =>
                //{
                //    band3str = new FileStream(ofd.FileNames[2], FileMode.Open, FileAccess.Read, FileShare.Read);
                //    decoder = new TiffBitmapDecoder(band3str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                //    band3src = decoder.Frames[0];                
                //});
                
                //WriteableBitmap wb1 = new WriteableBitmap(band1src);
                //WriteableBitmap wb2 = new WriteableBitmap(band2src);
                //WriteableBitmap wb3 = new WriteableBitmap(band3src);
                //img.Source = bms;
                //b = new Bitmap(ofd.FileName);
                //wb = new WriteableBitmap(band1src);
                
                //byte[] arr1 = new byte[arrayLength];
                //byte[] arr2 = new byte[arrayLength];
                //byte[] arr3 = new byte[arrayLength];
                //band1src.CopyPixels(arr1, stride, 0);
                //band2src.CopyPixels(arr2, stride, 0);
                //band3src.CopyPixels(arr3, stride, 0);
                
                Bitmap bitmap1 = new Bitmap(imagePath1);
                Bitmap bitmap2 = new Bitmap(imagePath2);
                Bitmap bitmap3 = new Bitmap(imagePath3);

                //Dataset band1 = Gdal.Open(ofd.FileNames[0], Access.GA_ReadOnly);
                /*Dataset band1 = Gdal.Open(imagePath1, Access.GA_ReadOnly);
                Band r = band1.GetRasterBand(1);
                int[] arr1 = new int[r.XSize * r.YSize];
                r.ReadRaster(0, 0, r.XSize, r.XSize, arr1, r.XSize, r.XSize, 0, 0);
                //Dataset band2 = Gdal.Open(ofd.FileNames[1], Access.GA_ReadOnly);
                Dataset band2 = Gdal.Open(imagePath2, Access.GA_ReadOnly);
                r = band2.GetRasterBand(1);
                byte[] arr2 = new byte[r.XSize * r.YSize];
                r.ReadRaster(0, 0, r.XSize, r.XSize, arr2, r.XSize, r.XSize, 0, 0);
                //Dataset band3 = Gdal.Open(ofd.FileNames[2], Access.GA_ReadOnly);
                Dataset band3 = Gdal.Open(imagePath3, Access.GA_ReadOnly);
                r = band3.GetRasterBand(1);
                byte[] arr3 = new byte[r.XSize * r.YSize];
                r.ReadRaster(0, 0, r.XSize, r.XSize, arr3, r.XSize, r.XSize, 0, 0);*/
                //int bytesPerPixel = (band1src.Format.BitsPerPixel + 7) / 8;
                //int stride = 4 * ((band1src.PixelWidth * bytesPerPixel + 3) / 4);
                //int arrayLength = stride * band1src.PixelHeight;
                         
                int w = bitmap1.Width;
                int h = bitmap1.Height;
                //Bitmap res = new Bitmap(w, h);
                //BitmapData bitmapData = res.LockBits(new System.Drawing.Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                /*string[] options = new string[] { "BLOCKXSIZE=" + w, "BLOCKYSIZE=" + h };
                using (Dataset res = Gdal.GetDriverByName("GTiff").Create(ofd.InitialDirectory + "\\1", w, h, 3, DataType.GDT_Byte, options)) {
                    double[] geoTransformerData = new double[6];
                    band1.GetGeoTransform(geoTransformerData);
                    res.SetGeoTransform(geoTransformerData);
                    res.SetProjection(band1.GetProjection());
                    Band outRedBand = res.GetRasterBand(1);
                    Band outGreenBand = res.GetRasterBand(2);
                    Band outBlueBand = res.GetRasterBand(3);
                    //res.AddBand(DataType.GDT_Float32, options);
                
                    outRedBand.WriteRaster(0, 0, w, h, arr1, w, h, 0, 0);
                    outGreenBand.WriteRaster(0, 0, w, h, arr2, w, h, 0, 0);
                    outBlueBand.WriteRaster(0, 0, w, h, arr3, w, h, 0, 0);
                    res.FlushCache();
                }*/
                
                              
                //Parallel.For(0, band1.Width, (j) => {
                
                using (FileStream stream = new FileStream(ofd.InitialDirectory + "\\1.tif", FileMode.Create))
                {
                    var result = new Bitmap(w, h);
                    for (int i = 0; i < w; i++)
                        for (int j = 0; j < h; j++)
                        {
                            //int index = i * w + j;
                            var color = System.Drawing.Color.FromArgb(bitmap1.GetPixel(i, j).R, bitmap2.GetPixel(i, j).R,
                                bitmap3.GetPixel(i, j).R);
                            result.SetPixel(i, j, color);
                        }
                    var encoder = new TiffBitmapEncoder();
                    var res_src = Imaging.CreateBitmapSourceFromHBitmap(result.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    encoder.Frames.Add(BitmapFrame.Create(res_src));
                    encoder.Save(stream);
                    
                    result.Dispose();
                }
            //});

            band1str = new FileStream(ofd.InitialDirectory + "\\1.tif", FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = new TiffBitmapDecoder(band1str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            var band1src = decoder.Frames[0];

            //var res_src = Imaging.CreateBitmapSourceFromHBitmap(res.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            img.Source = band1src;
            //}
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
