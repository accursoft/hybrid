using System.Windows;

using SyncClient;

namespace Client
{
    public partial class SyncManager : Window
    {
        public SyncManager()
        {
            InitializeComponent();
        }

        private void Synchronize(object sender, RoutedEventArgs e)
        {
            Sync.Synchronize();
        }

        private void online_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow(true).ShowDialog();
        }

        private void offline_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.IsProvisioned())
                new MainWindow(false).ShowDialog();
            else
                MessageBox.Show("The database has not been provisioned yet.\nPlease synchronise.", "Offline database not provisioned", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}