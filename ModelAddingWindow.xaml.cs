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
    /// Логика взаимодействия для ModelAdding.xaml
    /// </summary>
    public partial class ModelAddingWindow : Window
    {
        public byte Red {
            get { return Convert.ToByte(usersRed.Text); }
        }
        public byte Green
        {
            get { return Convert.ToByte(usersGreen.Text); }
        }
        public byte Blue
        {
            get { return Convert.ToByte(usersBlue.Text); }
        }
        public string ModelName
        {
            get { return modelName.Text; }
        }

        public ModelAddingWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
