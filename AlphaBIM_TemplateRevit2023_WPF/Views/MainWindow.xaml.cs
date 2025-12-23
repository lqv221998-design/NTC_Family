using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

using NTC.FamilyManager.ViewModels;

namespace NTC.FamilyManager.Views
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private MainViewModel _viewModel;

    	public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Nếu Enter, ta sẽ gọi LoginCommand từ ViewModel thay vì đóng Dialog
                if (_viewModel.LoginCommand.CanExecute(null))
                {
                    _viewModel.LoginCommand.Execute(null);
                }
            }

            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://newtecons.vn/");
            }
            catch (Exception)
            {
            }
        }

        #region Copy Title bar

        private void OpenWebSite(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://newtecons.vn/");
            }
            catch (Exception)
            {
            }
        }

        private void CustomDevelopment(object sender, RoutedEventArgs e)
        {
            try
            {
                // Newtecons BIM Department or similar
                Process.Start("https://newtecons.vn/");
            }
            catch (Exception)
            {
            }
        }

        private void Feedback(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("mailto:bim@newtecons.vn");
            }
            catch (Exception)
            {
            }
        }

        #endregion Copy Title bar

    }
}
