using FastDevTool.Interface;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Path = System.IO.Path;


namespace FastDevTool.Helper {
    internal class CompressFliter {
        private static readonly List<string> _fomatSupport = new() {
            ".rar",
            ".zip",
            ".gz", ".tar", ".tgz", "tar.gz", ".bz", ".bz2", ".xz", ".lz", ".txz", ".bzip2", ".tbz2", ".tbz", ".gzip", ".tpz",
            ".7z", ".cab", ".001",
            ".rpm"
            };
        private uint minSize = 0;
        private uint maxSize = uint.MaxValue;
        public List<string> FomatFliter { get; set; }
        public List<string> FileFliter { get; set; }
        public uint MinSize { get => minSize; set { minSize = value < maxSize ? value : minSize; } }
        public uint MaxSize { get => maxSize; set { maxSize = value > minSize ? value : maxSize; } }

        public CompressFliter(List<string> fomatFliter, List<string> fileFliter, uint minSize, uint maxSize) {
            FomatFliter = fomatFliter;
            FileFliter = fileFliter;
            MinSize = minSize;
            MaxSize = maxSize;
        }

        public CompressFliter() {
            FomatFliter = new(_fomatSupport);
            FileFliter = new List<string>();
        }

        public bool FliterOut(string filePath) {
            FileInfo fileInfo;
            if (!File.Exists(filePath) || !FomatFliter.Contains(Path.GetExtension(filePath))) {
                return false;
            }
            if (FileFliter.Count > 0 && !FileFliter.Contains(filePath)) {
                return false;
            }
            try {
                fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < MinSize || fileInfo.Length > MaxSize) {
                    return false;
                }
            } catch (Exception e) {
                Logger.Error(e.Message);
                return false;
            }
            return true;
        }

    }
    internal class CompressHelper : ICompress {

        public Dictionary<string, int> dic = new();
        private static SpinLock taskLock = new();
        private static uint maxTaskNum = 16;
        private static uint curTaskNum = 0;
        private static uint taskNum = 0;


        /// <summary>
        /// 精简解压路径，压缩包内只有一个文件时会直接解压到压缩包所在文件夹
        /// </summary>
        public static int IsTidyPath { get; set; }
        public static uint MaxTaskNum { get => maxTaskNum; set { maxTaskNum = value > 0 ? value : maxTaskNum; } }
        public static CompressFliter Fliter { get; set; } = new CompressFliter();

        private delegate void DecompressWokerDlg(string filePath, string outPath, string password, bool isDelete);

        private static bool InputVerify(bool isFile, ref string inPath, ref string outPath) {
            if (isFile) {
                if (!File.Exists(inPath)) {
                    Logger.Error(string.Format("File:{0} doesn't exit.", inPath));
                    return false;
                }
            } else {
                if (!Directory.Exists(inPath)) {
                    Logger.Error(string.Format("Directory:{0} doesn't exit.", inPath));
                    return false;
                }
            }

            // 如果outPath为空，默认解压到同名目录下
            outPath = Directory.Exists(outPath) ? outPath : GetFullPathWithoutExtention(inPath);

            return true;
        }

        private static string GetBaseName(string inPath) {
            var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(inPath));
            while (Fliter.FomatFliter.Contains(Path.GetExtension(name))) {
                name = Path.GetFileNameWithoutExtension(name);
            }
            return name;
        }

        public static string GetFullPathWithoutExtention(string filePath) {
            if (Path.EndsInDirectorySeparator(filePath)) return filePath;

            return Path.Combine(Directory.GetParent(filePath)!.FullName, GetBaseName(filePath));
        }

        /// <summary>
        /// 调用时parentOutPath不一定存在，因为可能在解压时才会创建，但调用时要保证路径格式合法；inPath一定存在且不能是根目录
        /// </summary>
        /// <param name="inPath"></param>
        /// <param name="parentOutPath"></param>
        /// <returns></returns>
        private static string CombineOutPath(string inPath, string parentOutPath) {
            if (!Path.IsPathFullyQualified(inPath)) {
                return string.Empty;
            }
            if (!Path.IsPathFullyQualified(parentOutPath)) {
                return GetFullPathWithoutExtention(inPath);
            }
            return Path.Combine(parentOutPath, GetBaseName(inPath));
        }


        /// <summary>
        /// 获取一个空闲的task资源
        /// </summary>
        /// <returns>成功：true；失败：false</returns>
        private static bool GetTask() {
            bool lockFlag = false;
            taskLock.Enter(ref lockFlag);
            if (curTaskNum >= maxTaskNum) {
                taskLock.Exit();
                return false;
            }
            curTaskNum++;
            //Logger.Info(format:"GetTask {0}", args: "")

            taskNum++;

            taskLock.Exit();
            return true;
        }

        /// <summary>
        /// 释放task计数，调用此接口释放task计数，不会减到负数
        /// </summary>
        private static void PutTask() {
            bool lockTaken = false;
            taskLock.Enter(ref lockTaken);
            if (curTaskNum > 0) {
                curTaskNum--;
            } else {
                Logger.Warn("task ref err.");
            }
            taskLock.Exit();
        }

        private bool DecompressSingle(string filePath, string outPath, string password, bool isDelete = false) {
#if DEBUG
            if (dic.ContainsKey(filePath)) {
                dic[filePath]++;
            } else {
                dic.Add(filePath, 1);
            }
#endif
            if (!Directory.Exists(outPath)) {
                Directory.CreateDirectory(outPath);
            }

            try {
                /* 带password的解压方法性能更差，哪怕password为空 */
                if (string.IsNullOrEmpty(password)) {
                    using var extractor = new SevenZipExtractor(filePath);
                    extractor.ExtractArchive(outPath);
                } else {
                    using var extractor = new SevenZipExtractor(filePath, password);
                    extractor.ExtractArchive(outPath);
                }
                if (isDelete) {
                    File.Delete(filePath);
                }
            } catch (Exception e) {
                Logger.Error(format: "Decompress {0} failed, due to: {1}.", args: new[] { Path.GetFileName(filePath), e.Message });
                return false;
            }
            Logger.Info(format: "Decompress {0} succeed.", args: Path.GetFileName(filePath));

            // 如果解压出来的文件仍然是一个压缩文件，且与原文件同名，那就继续解压
            var files = Directory.GetFiles(outPath);
            if (files.Length == 1 && Fliter.FliterOut(files[0]) && GetBaseName(files[0]) == GetBaseName(filePath)) {
                DecompressSingle(files[0], outPath, password, true);
            }
            return true;
        }
        
        private static void ExecDecompressAsyn(DecompressWokerDlg woker, string inPath, string outPath, string password, bool isDelete) {
            woker(inPath, outPath, password, isDelete);
            PutTask();
        }

        private static bool TryDecompressAsyn(DecompressWokerDlg woker, string inPath, string outPath, string password, bool isDelete) {
            if (GetTask()) {
                Task.Run(() => ExecDecompressAsyn(woker, inPath, outPath, password, isDelete));
                return true;
            } else {
                woker(inPath, outPath, password, false);
                return false;
            }
        }

        private void DecompressAllAdapter(bool isFile, string inPath, string outPath, string password, bool isDelete = false) {
            dic.Clear();
            curTaskNum = 0;
            taskNum = 0;

            if (!InputVerify(isFile, ref inPath, ref outPath)) {
                return;
            }

            DecompressAllWoker(inPath, outPath, password, false);
            WaitAllTaskDone();

            Logger.Info("taskNum:" + taskNum.ToString() + "/" + maxTaskNum.ToString());
        }

        private void DecompressAllWoker(string inPath, string outPath, string password, bool isDelete) {
            if (Fliter.FliterOut(inPath)) {
                if (DecompressSingle(inPath, outPath, password, isDelete)) {
                    DecompressAllWoker(outPath, outPath, password, true);
                }
            } else if (Directory.Exists(inPath)) {
                foreach (var dir in Directory.GetDirectories(inPath)) {
                    TryDecompressAsyn(DecompressAllWoker, dir, CombineOutPath(dir, outPath), password, isDelete);
                }
                foreach (var file in Directory.GetFiles(inPath)) {
                    // 这里前置判断一下，避免起不必要的线程
                    if (Fliter.FliterOut(file)) {
                        TryDecompressAsyn(DecompressAllWoker, file, CombineOutPath(file, outPath), password, isDelete);
                    }
                }
            }
        }

        private static void WaitAllTaskDone() {
            do {
                Thread.Sleep(10);
            }
            while (curTaskNum > 0);
        }



        public CompressHelper(string libPath) {
            SevenZipBase.SetLibraryPath(libPath);
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="outPath">解压到文件夹路径</param>
        /// <param name="password">密码</param>
        public bool Decompress(string filePath, string outPath, string password = "") {
            if (!Fliter.FliterOut(filePath)) {
                return false;
            }

            return DecompressSingle(filePath, outPath, password);
        }

        /// <summary>
        /// 递归解压整个压缩文件，保留原文件
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="outPath">解压到文件夹路径</param>
        /// <param name="password">密码</param>
        public void DecompressFileAll(string filePath, string outPath = "", string password = "") {
            DecompressAllAdapter(true, filePath, outPath, password, false);
        }

        /// <summary>
        /// 递归解压整个压缩文件，删除原文件
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="outPath">解压到文件夹路径</param>
        /// <param name="password">密码</param>
        public void DecompressFileAllAndDel(string filePath, string outPath = "", string password = "") {
            DecompressAllAdapter(true, filePath, outPath, password, true);
        }

        /// <summary>
        /// 递归解压整个文件夹的压缩文件，删除原文件
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="outPath">解压到文件夹路径</param>
        /// <param name="password">密码</param>
        public void DecompressDirectoryAllAndDel(string dirPath, string outPath = "", string password = "") {
            DecompressAllAdapter(false, dirPath, outPath, password, true);
        }

        /// <summary>
        /// 递归解压整个文件夹的压缩文件，保留原文件
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="outPath">解压到文件夹路径</param>
        /// <param name="password">密码</param>
        public void DecompressDirectoryAll(string dirPath, string outPath = "", string password = "") {
            DecompressAllAdapter(false, dirPath, outPath, password, false);
        }
    }
}

