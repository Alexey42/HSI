using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HSI.SatelliteInfo;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Runtime.InteropServices;

namespace HSI
{
    public class ImageInfo : IDisposable
    {
        Mat mat;
        public int width;
        public int height;
        public double dpi;
        public int bpp;
        public int stride;
        public string path;
        public int[] hist1 = new int[256];
        public int[] hist2 = new int[256];
        public int[] hist3 = new int[256];
        public Satellite satellite;
        public string[] bandPaths = new string[3];
        public string[] bandNames = new string[3];

        public ImageInfo() { } 

        public ImageInfo(Mat m, string _path)
        {
            mat = m;
            width = m.Cols;
            height = m.Rows;
            path = _path;
            //BS = BitmapSourceConverter.ToBitmapSource(mat);
            //dpi = BS.DpiX;
            //bpp = (BS.Format.BitsPerPixel + 7) / 8;
            //stride = BS.PixelWidth * bpp;
        }

        public ImageInfo(Mat m, Satellite s, string p, string[] paths, string[] names)
        {
            mat = m;
            path = p;
            satellite = s;
            bandPaths = paths;
            bandNames = names;
            width = m.Cols;
            height = m.Rows;
            //BS = BitmapSourceConverter.ToBitmapSource(mat);
            //dpi = BS.DpiX;
            //bpp = (BS.Format.BitsPerPixel + 7) / 8;
            //stride = BS.PixelWidth * bpp;
            
        }

        public Mat GetMat()
        {
            return mat;
        }

        public BitmapSource GetBS()
        {
            return mat.ToBitmapSource();
        }

        public BitmapImage GetBI()
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path);
            image.EndInit();
            return image;
        }

        public Vec3b[] GetBytes()
        {
            Vec3b[] pixels;
            mat.GetArray(out pixels);
            return pixels;
        }

        public void SetValues(int _w, int _h, double _dpi, int _bpp, int _s)
        {
            width = _w; height = _h; dpi = _dpi; bpp = _bpp; stride = _s;
        }


        public void Dispose()
        {
            if (mat != null)
                mat.Dispose();
            path = null;
            satellite = null;
            bandPaths = null;
            bandNames = null;
            GC.Collect();
        }
    }
}
