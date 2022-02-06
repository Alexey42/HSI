using HSI.SatelliteInfo;
using Ookii.Dialogs.Wpf;
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
using System.Windows.Shapes;

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для EMDWindow.xaml
    /// </summary>
    public partial class EMDWindow : Window
    {
        public string path = ""; // В релизе оставить ""
        public string Camera;
        private VistaFolderBrowserDialog openFileDialog;
        public Satellite sat;

        public EMDWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem l = (ListBoxItem)cam.SelectedItem;
            Camera = l.Content.ToString();

            Parse(openFileDialog);

            this.DialogResult = true;
        }

        private void cam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem item = (sender as ListBox).SelectedItem as ListBoxItem;
            switch (item.Content)
            {
                case "Landsat 8":
                    sat = new Landsat8();
                    break;
                case "Sentinel 2":
                    sat = new Sentinel2();
                    break;
                case "Aviris":
                    sat = new Aviris();
                    break;
            }
        }

        private void ChooseDirectory_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
            ofd.RootFolder = Environment.SpecialFolder.Recent;
            if (ofd.ShowDialog() == true)
            {
                openFileDialog = ofd;
                path = ofd.SelectedPath;
                chosenDirectory_lbl.Content = path;
                sat.SetDirectory(path);
            }

        }

        void Parse(VistaFolderBrowserDialog ofd)
        {
            if (Camera == "Landsat 8") sat.SetDirectory("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1"); // В релизе этой строки быть не должно
            if (Camera == "Sentinel 2") sat.SetDirectory("D:\\HSI_images\\S2B_MSIL1C_20190602T080619_N0207_R078_T38VMH_20190602T102902.SAFE"); // В релизе этой строки быть не должно
            if (Camera == "Aviris") sat.SetDirectory(@"D:\HSI_images\f080611t01p00r07rdn_c"); // В релизе этой строки быть не должно
        }
    }
}
