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
using System.IO;

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
        public int Mode = 1;

        public EMDWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            ListBoxItem l = (ListBoxItem)cam.SelectedItem;
            Camera = l.Content.ToString();
            Mode = int.Parse(mode.Text);

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

            if (sat.name != "Aviris")
            {
                UiDialogHelper.ShowWarning(this, "EMD сейчас реализован только для AVIRIS.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                UiDialogHelper.ShowWarning(this, "Выберите папку со снимком AVIRIS.");
                return false;
            }

            if (!int.TryParse(mode.Text, out Mode) || Mode < 1)
            {
                UiDialogHelper.ShowWarning(this, "Введите номер моды EMD: целое число больше 0.");
                return false;
            }

            try
            {
                sat.SetDirectory(path);
            }
            catch (Exception ex)
            {
                UiDialogHelper.ShowError(this, "Не удалось прочитать данные AVIRIS.", ex);
                return false;
            }

            return true;
        }
    }
}
