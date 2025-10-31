using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FileAccessApp
{
    public partial class MainWindow : Window
    {
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_ops.txt");
        private readonly SemaphoreSlim _fileWriteSemaphore = new SemaphoreSlim(3);
        private readonly ReaderWriterLockSlim _fileRwLock = new ReaderWriterLockSlim();

        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(_filePath))
                File.WriteAllText(_filePath, $"Log start at {DateTime.Now:O}{Environment.NewLine}");
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

        private async void BtnStartFileOps_Click(object sender, RoutedEventArgs e)
        {
            AddLog("Starting 5 background file write tasks...");
            TxtStatus.Text = "Running file tasks";
            List<Task> tasks = new List<Task>();
            for (int i = 1; i <= 5; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() => FileWriteTaskAsync(id)));
            }
            await Task.WhenAll(tasks);
            AddLog("All file tasks finished.");
            TxtStatus.Text = "Ready";
        }

        private async Task FileWriteTaskAsync(int taskId)
        {
            AddLog($"Task {taskId}: waiting for semaphore (max 3)...");
            await _fileWriteSemaphore.WaitAsync();
            try
            {
                AddLog($"Task {taskId}: writing...");
                string content = $"Task {taskId} writes at {DateTime.Now:O}{Environment.NewLine}";
                await File.AppendAllTextAsync(_filePath, content, Encoding.UTF8);
                AddLog($"Task {taskId}: write completed.");
                await Task.Delay(300 + taskId * 100);
            }
            catch (Exception ex)
            {
                AddLog($"Task {taskId}: write error - {ex.Message}");
            }
            finally
            {
                _fileWriteSemaphore.Release();
                AddLog($"Task {taskId}: semaphore released.");
            }
        }

        private async void BtnClearFile_Click(object sender, RoutedEventArgs e)
        {
            AddLog("Attempting to clear file with WriterLock...");
            TxtStatus.Text = "Clearing file...";
            await Task.Run(() =>
            {
                bool gotLock = false;
                try
                {
                    gotLock = _fileRwLock.TryEnterWriteLock(TimeSpan.FromSeconds(3));
                    if (!gotLock)
                    {
                        AddLog("Failed to acquire WriterLock for clear (timeout).");
                        return;
                    }
                    File.WriteAllText(_filePath, $"File cleared at {DateTime.Now:O}{Environment.NewLine}");
                    AddLog("File cleared (WriterLock).");
                }
                catch (Exception ex)
                {
                    AddLog($"Clear error: {ex.Message}");
                }
                finally
                {
                    if (gotLock && _fileRwLock.IsWriteLockHeld)
                        _fileRwLock.ExitWriteLock();
                }
            });
            TxtStatus.Text = "Ready";
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLog($"Failed to open file: {ex.Message}");
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogList.Items.Clear();
            AddLog("Logs cleared by user.");
        }
    }
}
