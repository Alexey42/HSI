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
            get { return byte.Parse(usersRed.Text); }
        }
        public byte Green
        {
            get { return byte.Parse(usersGreen.Text); }
        }
        public byte Blue
        {
            get { return byte.Parse(usersBlue.Text); }
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
            byte value;
            if (!byte.TryParse(usersRed.Text, out value) ||
                !byte.TryParse(usersGreen.Text, out value) ||
                !byte.TryParse(usersBlue.Text, out value))
            {
                UiDialogHelper.ShowWarning(this, "Цвет эталона задается числами от 0 до 255 для R, G и B.");
                return;
            }

            if (string.IsNullOrWhiteSpace(modelName.Text))
            {
                UiDialogHelper.ShowWarning(this, "Введите имя эталона.");
                return;
            }

            this.DialogResult = true;
        }
    }
}
