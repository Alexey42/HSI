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
using System.Globalization;

namespace HSI
{
    /// <summary>
    /// Логика взаимодействия для Segment.xaml
    /// </summary>
    public partial class SegmentWindow : Window
    {
        public float tr1, tr2;
        public int tr3;

        public SegmentWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs ez)
        {
            if (!TryParsePositiveFloat(t1.Text, out tr1) ||
                !TryParsePositiveFloat(t2.Text, out tr2) ||
                !int.TryParse(t3.Text, out tr3) ||
                tr3 < 1)
            {
                UiDialogHelper.ShowWarning(this, "Введите параметры сегментации: sigma > 0, K > 0, min >= 1.");
                return;
            }

            this.DialogResult = true;
        }

        private bool TryParsePositiveFloat(string text, out float value)
        {
            text = text.Replace(",", ".");
            return float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) && value > 0;
        }
    }
}
