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
            tr1 = float.Parse(t1.Text.Replace(".", ","), System.Globalization.NumberStyles.Any);
            tr2 = float.Parse(t2.Text.Replace(".", ","), System.Globalization.NumberStyles.Any);
            tr3 = int.Parse(t3.Text);
            this.DialogResult = true;
        }
    }
}
