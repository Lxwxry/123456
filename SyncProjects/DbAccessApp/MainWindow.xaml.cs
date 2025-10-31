using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DbAccessApp
{
    public partial class MainWindow : Window
    {
        private readonly SemaphoreSlim _dbConnectionSemaphore = new SemaphoreSlim(4);
        private readonly ReaderWriterLockSlim _configRwLock = new ReaderWriterLockSlim();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddLog(string text)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {text}";
            Dispatcher.Invoke(() =>
            {
                LogList.Items.Insert(0, line);
                if (LogList.Items.Count > 500) LogList.Items.RemoveAt(LogList.Items.Count - 1);
            });
        }

        private async void BtnStartDbOps_Click(object sender, RoutedEventArgs e)
        {
            AddLog("Starting 8 simulated DB tasks (Semaphore 4)...");
            TxtStatus.Text = "Running DB tasks";
            List<Task> tasks = new List<Task>();
            for (int i = 1; i <= 8; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() => SimulatedDbConnectionAsync(id)));
            }
            await Task.WhenAll(tasks);
            AddLog("All DB tasks finished.");
            TxtStatus.Text = "Ready";
        }

        private async Task SimulatedDbConnectionAsync(int id)
        {
            AddLog($"DB#{id}: waiting for connection (semaphore 4)...");
            await _dbConnectionSemaphore.WaitAsync();
            try
            {
                AddLog($"DB#{id}: connection acquired.");
                _configRwLock.EnterReadLock();
                try
                {
                    AddLog($"DB#{id}: reading config...");
                    await Task.Delay(200);
                }
                finally
                {
                    _configRwLock.ExitReadLock();
                }
                await Task.Delay(500 + id * 100);
                if (id % 5 == 0)
                {
                    _configRwLock.EnterWriteLock();
                    try
                    {
                        AddLog($"DB#{id}: writing config (Writer)...");
                        await Task.Delay(300);
                    }
                    finally
                    {
                        _configRwLock.ExitWriteLock();
                    }
                }
                AddLog($"DB#{id}: finished.");
            }
            catch (Exception ex)
            {
                AddLog($"DB#{id}: error - {ex.Message}");
            }
            finally
            {
                _dbConnectionSemaphore.Release();
                AddLog($"DB#{id}: connection semaphore released.");
            }
        }

        private void BtnSimReadConfig_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                _configRwLock.EnterReadLock();
                try
                {
                    AddLog("Manual config read simulation...");
                    Thread.Sleep(300);
                    AddLog("Config read finished.");
                }
                finally
                {
                    _configRwLock.ExitReadLock();
                }
            });
        }

        private void BtnSimWriteConfig_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                bool got = _configRwLock.TryEnterWriteLock(2000);
                if (!got)
                {
                    AddLog("Failed to acquire WriterLock for config (timeout).");
                    return;
                }
                try
                {
                    AddLog("Manual config write simulation...");
                    Thread.Sleep(500);
                    AddLog("Config write finished.");
                }
                finally
                {
                    if (_configRwLock.IsWriteLockHeld)
                        _configRwLock.ExitWriteLock();
                }
            });
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogList.Items.Clear();
            AddLog("Logs cleared by user.");
        }
    }
}
