using System;
using System.Threading;
using System.Windows;

namespace DbAccessApp
{
    public partial class App : Application
    {
        private Mutex? _instanceMutex;
        private const string MutexName = "Local\DbAccessApp_SingleInstance_Mutex_02112025";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool createdNew;
            try
            {
                _instanceMutex = new Mutex(true, MutexName, out createdNew);
            }
            catch
            {
                createdNew = false;
            }
            if (!createdNew)
            {
                MessageBox.Show("Another instance is running. The application will exit.", "Instance running", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }
            MainWindow w = new MainWindow();
            w.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
