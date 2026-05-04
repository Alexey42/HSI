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
    public partial class ClassifyWindow : Window
    {
        public float Threshold = 8;
        public string Method = "";

        public ClassifyWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            string thresholdText = threshold_textbox.Text.Replace(",", ".");
            if (!float.TryParse(thresholdText, NumberStyles.Any, CultureInfo.InvariantCulture, out Threshold) || Threshold <= 0)
            {
                UiDialogHelper.ShowWarning(this, "Введите положительный порог классификации. Можно использовать точку или запятую.");
                return;
            }

            var item = (ComboBoxItem)method_list.SelectedItem;
            if (item == null)
            {
                UiDialogHelper.ShowWarning(this, "Выберите метод классификации.");
                return;
            }

            Method = item.Name;
            this.DialogResult = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
