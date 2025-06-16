using System;
using System.Threading.Tasks;
using DDSWebAPI.Examples;

namespace DDSWebAPI.Demo
{
    /// <summary>
    /// DDSWebAPI 功能展示主控台程式
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DDSWebAPI 完整功能展示 ===");
            Console.WriteLine("包含安全性功能、效能控制和設定檔管理");
            Console.WriteLine();

            try
            {
                var example = new ConsoleExample();
                await example.RunExampleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程式執行錯誤: {ex.Message}");
                Console.WriteLine($"錯誤詳細: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("程式結束，按任意鍵退出...");
            Console.ReadKey();
        }
    }
}
