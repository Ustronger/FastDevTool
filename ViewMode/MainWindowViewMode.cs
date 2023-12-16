using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastDevTool.Helper;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace FastDevTool.ViewMode {
    internal partial class MainWindowViewMode : ObservableObject
    {
        private readonly CompressHelper cp = new(@"7z.dll");

        [ObservableProperty]
        private string? filePath;

        [ObservableProperty]
        private string? log;

        [ObservableProperty]
        private string threadNum;

        [RelayCommand]
        private void ClearLog() {
            Log = string.Empty;
        }

        private void Decompress(string filePath) {
            //string outPath = CompressHelper.GetFullPathWithoutExtention(filePath);
            //string outPath = "P:\\Code\\VSreop\\data\\新建文件夹\\xx";
            //Logger.Info(outPath);
            //Directory.CreateDirectory(outPath);
            //cp.Decompress(filePath, outPath);

            cp.DecompressFileAll(filePath, "");
        }

        [RelayCommand]
        private async Task Uncompress(string path) 
        {
            try {
                CompressHelper.MaxTaskNum = UInt32.Parse(ThreadNum);
            } catch (Exception e) {
                Logger.Warn(format:"Set thread: {0} num failed, e: {1}", args:new[]{ThreadNum, e.Message});
            }

            long t1 = DateTime.Now.Ticks;
            await Task.Run(() => Decompress(path));
            Logger.Info("cost:" + ((DateTime.Now.Ticks - t1)/10000).ToString() + "ms");
            var dir = CompressHelper.GetFullPathWithoutExtention(path);
            if (Directory.Exists(dir)) {
                Process.Start("explorer.exe", dir);
            }
#if DEBUG
            //foreach (var key in cp.dic) {
            //    Logger.Info(key.Key + ", " + key.Value.ToString());
            //}
#endif
        }

        public MainWindowViewMode() {
            Logger.iniLog(this);
        }

    }
}
