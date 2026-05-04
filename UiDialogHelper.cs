using System;
using System.Windows;

namespace HSI
{
    public static class UiDialogHelper
    {
        public static void ShowInfo(Window owner, string message)
        {
            MessageBox.Show(owner, message, "HSI", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowWarning(Window owner, string message)
        {
            MessageBox.Show(owner, message, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void ShowError(Window owner, string message)
        {
            MessageBox.Show(owner, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowError(Window owner, string message, Exception exception)
        {
            MessageBox.Show(owner, message + Environment.NewLine + Environment.NewLine + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
