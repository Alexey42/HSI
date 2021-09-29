using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace HSI
{
    public static class OpenSaveHelper
    {
        public static Tuple<BitmapSource, FileStream> SaveTifImage(string path, int width, int height, double dpiX, double dpiY, System.Windows.Media.PixelFormat format, byte[] pixels, int stride)
        {
            FileStream str = new FileStream(path, FileMode.OpenOrCreate);
            /*List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>
            {
                Colors.Red,
                Colors.Green,
                Colors.Blue
            };
            BitmapPalette myPalette = new BitmapPalette(colors);*/
            BitmapSource bitmapSource = BitmapSource.Create(width, height, dpiX, dpiY, format, null, pixels, stride);
            TiffBitmapEncoder encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Compression = TiffCompressOption.None;
            encoder.Save(str);

            Tuple<BitmapSource, FileStream> result = new Tuple<BitmapSource, FileStream>(bitmapSource, str);

            return result;
        }

        public static Mat BandToBitmap(string path)
        {
            string type = path.Substring(path.Length - 3, 3).ToLower();
            if (type == "tif" || type == "iff")
            {
                return Cv2.ImRead(path, ImreadModes.Grayscale);
            }
            else 
                return Cv2.ImRead(path, ImreadModes.Grayscale);
        }

        public static BitmapSource BandToBitmap_TIF(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            TiffBitmapDecoder decoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        public static BitmapSource BandToBitmap_JPEG(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            JpegBitmapDecoder decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }
    }
}
