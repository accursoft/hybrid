using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Client.Properties;

using SyncClient;

namespace Client
{
    public partial class SyncManager : Window
    {
        public SyncManager()
        {
            InitializeComponent();
        }

        void online_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainWindow(true);

            if (!Settings.Default.Online)
                Synchronise();
            else {
                main.Synchronising(true);
                Task.Factory.StartNew(delegate {
                    Sync.Synchronize();
                    main.Synchronising(false);
                });
            }

            main.ShowDialog();
            Synchronise();
            Settings.Default.Online = true;
            Settings.Default.Save();
        }

        void offline_Click(object sender, RoutedEventArgs e)
        {
            if (!Sync.IsProvisioned()) {
                MessageBox.Show("The database has not been provisioned yet.\nPlease synchronise.", "Offline database not provisioned", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            new MainWindow(false).ShowDialog();
            Settings.Default.Online = false;
            Settings.Default.Save();
        }

        void Synchronise()
        {
            Synchronising.Visibility = Visibility.Visible;
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(Sync.Synchronize));
            Synchronising.Visibility = Visibility.Hidden;
        }

        void synchronise_Click(object sender, RoutedEventArgs e)
        {
            Synchronise();
        }
    }
}