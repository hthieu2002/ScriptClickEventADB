using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptClickEventADB
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer _deviceCheckTimer;
        private System.Windows.Forms.Timer _automationTimer;
        private Dictionary<string, CancellationTokenSource> _deviceCancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private Dictionary<string, Task> _automationTasks = new Dictionary<string, Task>();
        private Dictionary<string, Task> _stopTasks = new Dictionary<string, Task>(); // New dictionary for stop tasks
        private RandomCoordinateGenerator _coordinateGenerator;
        private List<string> _deviceIds = new List<string>();
        private int _swipeStartX;
        private int _swipeStartY;
        private int _swipeEndX;
        private int _swipeEndY;
        private int _tapX;
        private int _tapY;

        public Form1()
        {
            InitializeComponent();

            _coordinateGenerator = new RandomCoordinateGenerator();

            InitializeDeviceTable(); // Initialize the device table

            // Initialize the timer to check for new devices
            _deviceCheckTimer = new System.Windows.Forms.Timer();
            _deviceCheckTimer.Interval = 5000; // Check every 5 seconds
            _deviceCheckTimer.Tick += DeviceCheckTimer_Tick;
            _deviceCheckTimer.Start();

            _automationTimer = new System.Windows.Forms.Timer();
            _automationTimer.Interval = 3000; // Interval for automation
            _automationTimer.Tick += OnAutomationTimerTick;

            dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDevices.AllowUserToResizeColumns = true;
            dgvDevices.AllowUserToResizeRows = true;
            dgvDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDevices.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
            dgvDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            //dgvDevices.Dock = DockStyle.Fill;

        }

        private void InitializeDeviceTable()
        {
            dgvDevices.Columns.Add("DeviceId", "Device ID");
            dgvDevices.Columns.Add("Status", "Status");
            dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Select full row
            dgvDevices.MultiSelect = true; // Allow multiple row selection

            // Ensure columns cannot be automatically sorted
            foreach (DataGridViewColumn column in dgvDevices.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable; // Prevent auto sorting
            }
        }

        private async void DeviceCheckTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var deviceIds = await GetDeviceIdsAsync();

                if (dgvDevices == null)
                {
                    MessageBox.Show("DataGridView is not initialized.");
                    return;
                }

                // Check and add new devices to DataGridView
                foreach (var deviceId in deviceIds)
                {
                    bool exists = dgvDevices.Rows.Cast<DataGridViewRow>()
                        .Any(row => row.Cells["DeviceId"].Value?.ToString() == deviceId);

                    if (!exists)
                    {
                        dgvDevices.Rows.Add(deviceId, "Detected");
                    }
                }

                // Remove disconnected devices
                for (int i = dgvDevices.Rows.Count - 1; i >= 0; i--)
                {
                    var deviceId = dgvDevices.Rows[i].Cells["DeviceId"].Value?.ToString();
                    if (deviceId != null && !deviceIds.Contains(deviceId))
                    {
                        dgvDevices.Rows.RemoveAt(i);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void GenerateRandomCoordinates()
        {
            var coordinates = _coordinateGenerator.GetRandomSwipeCoordinates();
            _swipeStartX = coordinates.StartX;
            _swipeStartY = coordinates.StartY;
            _swipeEndX = coordinates.EndX;
            _swipeEndY = coordinates.EndY;

            _tapX = _coordinateGenerator.GetRandomSwipeTapX();
            _tapY = _coordinateGenerator.GetRandomSwipeTapY();
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (dgvDevices.SelectedRows.Count == 0)
            {
                log.Text = "Please select at least one device from the table.";
                return;
            }

            // Initialize and store CancellationTokenSource for each device
            _deviceCancellationTokens.Clear();
            _automationTasks.Clear(); // Clear previous automation tasks

            foreach (DataGridViewRow row in dgvDevices.SelectedRows)
            {
                var deviceId = row.Cells["DeviceId"].Value.ToString();
                row.Cells["Status"].Value = "Running"; // Update device status

                var cts = new CancellationTokenSource();
                _deviceCancellationTokens[deviceId] = cts;

                var task = Task.Run(() => StartAutomationForDevice(deviceId, cts.Token), cts.Token);
                _automationTasks[deviceId] = task;
            }

            _automationTimer.Start();
        }

        private async Task StartAutomationForDevice(string deviceId, CancellationToken token)
        {
            GenerateRandomCoordinates();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(3000, token); // Perform automation task every 3 seconds

                    // Perform the automation actions
                    await PerformTapAndSwipeAsync(deviceId);
                }
            }
            catch (TaskCanceledException)
            {
                // Handle task cancellation
                await StopAutomationForDeviceAsync(deviceId); // Update this line
            }
        }

        private async Task PerformTapAndSwipeAsync(string deviceId)
        {
            try
            {
                await ADBService.SwipeAsync(deviceId, _swipeStartX, _swipeStartY, _swipeEndX, _swipeEndY);
            }
            catch (Exception ex)
            {
                log.Text = $"An error occurred for device {deviceId}: {ex.Message}";
            }
        }

        private async Task<List<string>> GetDeviceIdsAsync()
        {
            var deviceIds = new List<string>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = "devices",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split('\t');
                    if (parts.Length > 0)
                    {
                        var deviceId = parts[0].Trim();
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            deviceIds.Add(deviceId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Text = $"Error occurred while getting device IDs: {ex.Message}";
            }

            return deviceIds;
        }

        private async Task StopAllAutomationAsync()
        {
            _automationTimer?.Stop();

            // Stop all current devices
            var stopTasks = _deviceCancellationTokens.Keys.Select(deviceId => StopAutomationForDeviceAsync(deviceId)).ToList();
            await Task.WhenAll(stopTasks); // Wait for all stop tasks to complete

            _deviceCancellationTokens.Clear();

            log.Text = "Automation stopped.";
        }

        private async Task StopAutomationForDeviceAsync(string deviceId)
        {
            if (_deviceCancellationTokens.TryGetValue(deviceId, out var cts))
            {
                cts.Cancel();
                if (_automationTasks.TryGetValue(deviceId, out var task))
                {
                    await Task.WhenAny(task, Task.Delay(5000)); // Wait for task to complete or timeout
                }
                _deviceCancellationTokens.Remove(deviceId);
                _automationTasks.Remove(deviceId);
            }

            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (row.Cells["DeviceId"].Value?.ToString() == deviceId)
                {
                    row.Cells["Status"].Value = "Stopped";
                }
            }
        }

        private void OnAutomationTimerTick(object sender, EventArgs e)
        {
            // No need to stop timer if no devices are running; handled in btnStart_Click
            if (_deviceCancellationTokens.Count == 0)
            {
                _automationTimer.Stop();
                return;
            }

            GenerateRandomCoordinates();

            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                var deviceId = row.Cells["DeviceId"].Value?.ToString();
                if (deviceId != null && _deviceCancellationTokens.ContainsKey(deviceId))
                {
                    // Trigger the task for each running device
                    _ = PerformTapAndSwipeAsync(deviceId);
                }
            }
        }

        private void btnStop_Click_1(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            // Initiate stopping all automations
            _ = StopAllAutomationAsync();
            btnStop.Enabled = true;
        }
    }
}
