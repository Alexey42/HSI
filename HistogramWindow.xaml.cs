using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для HistogramWindow.xaml
    /// </summary>
    public partial class HistogramWindow : Window
    {
        Bitmap bitmap;
        int[] hist;

        public HistogramWindow(Bitmap b, int[] data, string name)
        {
            InitializeComponent();

            bitmap = b;
            hist = data;
            Title = name;
            img.Width = bitmap.Width;
            img.Height = bitmap.Height;
            img.Source = Imaging.CreateBitmapSourceFromHBitmap(b.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); ;
            info_lbl.Background = new SolidColorBrush(Colors.White);
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
            ofd.RootFolder = Environment.SpecialFolder.Recent;
            //ofd.SelectedPath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1";
            if (ofd.ShowDialog() == true)
            {
                bitmap.Save(ofd.SelectedPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            UIElement element = (UIElement)sender;
            int x = (int)e.GetPosition(element).X;
            int y = (int)e.GetPosition(element).Y;
            if (x >= bitmap.Width || x < 0 || y >= bitmap.Height || y < 0)
                return;
            var t = bitmap.GetPixel(x, y).Name;
            if (bitmap.GetPixel(x, y).Name != "ff000000")
            {
                info_lbl.Content = x + ", " + hist[x];
            }
        }
    }
}
