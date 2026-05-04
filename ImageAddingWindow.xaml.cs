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
using System.Globalization;

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для AddImage.xaml
    /// </summary>
    public partial class ImageAddingWindow : Window
    {
        public string path = ""; // В релизе оставить ""
        public string[] BandPaths = new string[3];
        public string Camera;
        private VistaFolderBrowserDialog openFileDialog;
        public Satellite sat;

        public ImageAddingWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            ListBoxItem l = (ListBoxItem)cam.SelectedItem;
            Camera = l.Content.ToString();

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
            if (sat == null)
            {
                UiDialogHelper.ShowWarning(this, "Сначала выберите спутник/камеру.");
                return;
            }

            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
            ofd.RootFolder = Environment.SpecialFolder.Recent;
            //ofd.SelectedPath = "D:\\HSI_images\\LC08_L2SP_174021_20200621_20200823_02_T1";
            if (ofd.ShowDialog() == true)
            {
                openFileDialog = ofd;
                path = ofd.SelectedPath;
                chosenDirectory_lbl.Content = path;
                try
                {
                    sat.SetDirectory(path);
                }
                catch (Exception ex)
                {
                    UiDialogHelper.ShowError(this, "Не удалось прочитать выбранную папку спутникового снимка.", ex);
                }
            }

        }

        bool ValidateInput()
        {
            if (sat == null || cam.SelectedItem == null)
            {
                UiDialogHelper.ShowWarning(this, "Выберите спутник/камеру.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                UiDialogHelper.ShowWarning(this, "Выберите папку со снимком.");
                return false;
            }

            try
            {
                sat.SetDirectory(path);
                FillBandPath(0, ch1.Text, ch1_lbl);
                FillBandPath(1, ch2.Text, ch2_lbl);
                FillBandPath(2, ch3.Text, ch3_lbl);
            }
            catch (Exception ex)
            {
                UiDialogHelper.ShowError(this, "Не удалось подобрать каналы. Проверьте папку и номера каналов.", ex);
                return false;
            }

            for (int i = 0; i < BandPaths.Length; i++)
            {
                bool fileChannelExpected = sat.GetFormat() != "img";
                if (string.IsNullOrWhiteSpace(BandPaths[i]) || (fileChannelExpected && !File.Exists(BandPaths[i])))
                {
                    UiDialogHelper.ShowWarning(this, "Канал " + (i + 1) + " не выбран или файл не найден.");
                    return false;
                }
            }

            return true;
        }

        void FillBandPath(int index, string channelText, Label label)
        {
            if (channelText != "...")
            {
                BandPaths[index] = sat.FindBandByNumber(channelText);
                label.Content = sat.GetBandNameByNumber(channelText);
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
            else
                return;

            if (button.Name == "ch1_btn")
            {
                BandPaths[0] = file;
                ch1_lbl.Content = sat != null ? sat.GetBandNameByFilename(file) : "";
                ch1.Text = "...";
            }
            else if (button.Name == "ch2_btn")
            {
                BandPaths[1] = file;
                ch2_lbl.Content = sat != null ? sat.GetBandNameByFilename(file) : "";
                ch2.Text = "...";
            }
            else if (button.Name == "ch3_btn")
            {
                BandPaths[2] = file;
                ch3_lbl.Content = sat != null ? sat.GetBandNameByFilename(file) : "";
                ch3.Text = "...";
            }
        }

        private void ch1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ch1_lbl == null || ch2_lbl == null || ch3_lbl == null) return;
            var ch = (TextBox)sender;
            if (ch.Text == "..." || sat == null) return;
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
    }
}
