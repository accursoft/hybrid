using System.Windows;
using System.Windows.Controls;

using Model;

namespace Client
{
    public partial class MainWindow : Window
    {
        ViewModel vm;

        private MainWindow()
        {
            InitializeComponent();
            vm = (ViewModel)FindResource("vm");
        }

        public MainWindow(bool online) : this()
        {
            vm.Online = online;
        }

        private void ordersDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            ViewModel.InitialiseOrder((Order)e.NewItem);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            vm.Save();
        }
    }
}