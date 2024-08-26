using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptClickEventADB
{
    public static class ADBService
    {

        public static async Task TapAsync(string deviceId, int x, int y)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"-s {deviceId} shell input tap {x} {y}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Đọc đầu ra và lỗi của lệnh ADB
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                // Hiển thị thông tin lỗi nếu có
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Lỗi: {error}");
                }

                // Hiển thị kết quả đầu ra
                Console.WriteLine($"Kết quả Tap: {output}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thực hiện thao tác chạm: {ex.Message}");
            }
        }

        // Phương thức thực hiện thao tác vuốt từ (x1, y1) đến (x2, y2)
        public static async Task SwipeAsync(string deviceId, int x1, int y1, int x2, int y2)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"-s {deviceId} shell input swipe {x1} {y1} {x2} {y2}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Đọc đầu ra và lỗi của lệnh ADB
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                // Hiển thị thông tin lỗi nếu có
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Lỗi: {error}");
                }

                // Hiển thị kết quả đầu ra
                Console.WriteLine($"Kết quả Swipe: {output}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thực hiện thao tác vuốt: {ex.Message}");
            }
        }
    }
}
