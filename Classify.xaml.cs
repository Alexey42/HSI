using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Логика взаимодействия для Classify.xaml
    /// </summary>
    public partial class Classify : Window
    {
        public float Threshold = 8;
        public string Method = "";

        public Classify()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Threshold = float.Parse(threshold_textbox.Text, CultureInfo.InvariantCulture.NumberFormat);
            var item = (ComboBoxItem)method_list.SelectedItem;
            Method = item.Name;
            this.DialogResult = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
