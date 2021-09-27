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
    /// Логика взаимодействия для ChooseSatellite.xaml
    /// </summary>
    public partial class ChooseSatellite : Window
    {
        public string camera = "Landsat 8";

        public ChooseSatellite()
        {
            InitializeComponent();
        }

        private void cam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem item = (sender as ListBox).SelectedItem as ListBoxItem;
            camera = (string)item.Content;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
