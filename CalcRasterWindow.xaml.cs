using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using Ookii.Dialogs.Wpf;
using HSI.SatelliteInfo;

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для AddImage.xaml
    /// </summary>
    public partial class CalcRasterWindow : Window
    {
        public string path = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1"; // В релизе оставить ""
        public string[] BandPaths = new string[3];
        public string Camera;
        private VistaFolderBrowserDialog openFileDialog;
        public Satellite sat;
        public string Formula;

        public CalcRasterWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs ez)
        {
            ListBoxItem l = (ListBoxItem)cam.SelectedItem;
            Camera = l.Content.ToString();
            Formula = formula.Text;

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
            }
        }

        private void ChooseDirectory_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
            ofd.RootFolder = Environment.SpecialFolder.Recent;
            //ofd.SelectedPath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1";
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
            if (ch1.Text != "...")
            {
                BandPaths[0] = sat.FindBandByNumber(ch1.Text);
                ch1_lbl.Content = sat.GetBandNameByNumber(ch1.Text); // В релизе убрать
            }
            if (ch2.Text != "...")
            {
                BandPaths[1] = sat.FindBandByNumber(ch2.Text);
                ch2_lbl.Content = sat.GetBandNameByNumber(ch2.Text); // В релизе убрать
            }
            if (ch3.Text != "...")
            {
                BandPaths[2] = sat.FindBandByNumber(ch3.Text);
                ch3_lbl.Content = sat.GetBandNameByNumber(ch3.Text); // В релизе убрать
            }
        }

        private void channel_btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (path != "")
                ofd.InitialDirectory = path;

            if (ofd.ShowDialog() == true)
            {
                file = ofd.FileName;
                path = file.Substring(0, file.LastIndexOf('\\'));
            }

            if (button.Name == "ch1_btn")
            {
                BandPaths[0] = file;
                ch1_lbl.Content = sat.GetBandNameByFilename(file);
                ch1.Text = "...";
            }
            else if (button.Name == "ch2_btn")
            {
                BandPaths[1] = file;
                ch2_lbl.Content = sat.GetBandNameByFilename(file);
                ch2.Text = "...";
            }
            else if (button.Name == "ch3_btn")
            {
                BandPaths[2] = file;
                ch3_lbl.Content = sat.GetBandNameByFilename(file);
                ch3.Text = "...";
            }
        }

        private void ch1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ch1_lbl == null || ch2_lbl == null || ch3_lbl == null) return;
            var ch = (TextBox)sender;
            if (ch.Text == "...") return;
            string name = sat.GetBandNameByNumber(ch.Text);
            switch (ch.Name)
            {
                case "ch1":
                    ch1_lbl.Content = name;
                    break;
                case "ch2":
                    ch2_lbl.Content = name;
                    break;
                case "ch3":
                    ch3_lbl.Content = name;
                    break;
            }
        }

        void AddSymbol(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string op = (string)button.Content;

            formula.Text += op + " ";

        }
    }
}
