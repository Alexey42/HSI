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

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для AddImage.xaml
    /// </summary>
    public partial class ImageAdding : Window
    {
        public string path;
        public string[] BandPaths = new string[3];
        public string Camera;
        private VistaFolderBrowserDialog openFileDialog;

        public ImageAdding()
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
            }
            
        }

        void ParseLandsat8(VistaFolderBrowserDialog ofd)
        {
            string infoPath;
            //var directory = Directory.GetFiles(/*ofd.FileName*/ofd.SelectedPath);
            var directory = Directory.GetFiles("D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1");
            foreach (var x in directory)
            {
                if (x.ToLower().EndsWith("mtl.xml")) infoPath = x;
                if (x.ElementAt(x.Length - 5) == ch1.Text[0]) BandPaths[0] = x;
                if (x.ElementAt(x.Length - 5) == ch2.Text[0]) BandPaths[1] = x;
                if (x.ElementAt(x.Length - 5) == ch3.Text[0]) BandPaths[2] = x;
            }

            //XDocument xml = XDocument.Load(infoPath);
            /*int width = (from x in xml.Root.Descendants()
                         where x.Name == "REFLECTIVE_SAMPLES"
                         select Convert.ToInt32(x.Value)).First();
            int height = (from x in xml.Root.Descendants()
                          where x.Name == "REFLECTIVE_LINES"
                          select Convert.ToInt32(x.Value)).First();*/
        }
    }
}
