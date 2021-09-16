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
    public partial class CalcRaster : Window
    {
        public string path;
        public string[] BandPaths = new string[3];
        public string Camera;
        public string Formula;
        private VistaFolderBrowserDialog openFileDialog;
        public Satellite sat;

        public CalcRaster()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem l = (ListBoxItem)cam.SelectedItem;
            Camera = l.Content.ToString();
            switch (Camera)
            {
                case "Landsat 8":
                    ParseLandsat8(openFileDialog);
                    break;
            }
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
                sat.SetDirectory(Directory.GetFiles(path));
            }

        }

        void ParseLandsat8(VistaFolderBrowserDialog ofd)
        {
            //var directory = Directory.GetFiles(/*ofd.FileName*/ofd.SelectedPath);
            var directory = Directory.GetFiles("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1");
            sat.SetDirectory(directory); // В релизе этих трёх строчек быть не должно

            if (ch1.Text != "...")
            {
                BandPaths[0] = sat.FindBandByInt(int.Parse(ch1.Text));
                ch1_lbl.Content = sat.GetBandNameByInt(int.Parse(ch1.Text));
            }
            if (ch2.Text != "...")
            {
                BandPaths[1] = sat.FindBandByInt(int.Parse(ch2.Text));
                ch2_lbl.Content = sat.GetBandNameByInt(int.Parse(ch2.Text));
            }
            if (ch3.Text != "...")
            {
                BandPaths[2] = sat.FindBandByInt(int.Parse(ch3.Text));
                ch3_lbl.Content = sat.GetBandNameByInt(int.Parse(ch3.Text));
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
                file = ofd.FileName;

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
            int res = 0;
            int.TryParse(ch.Text, out res);
            string name = sat.GetBandNameByInt(res);
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
