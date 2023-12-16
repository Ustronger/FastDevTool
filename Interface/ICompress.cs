using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastDevTool.Interface {
    internal interface ICompress {
        void DecompressFileAll(string filePath, string outPath, string password = "");

        void DecompressFileAllAndDel(string filePath, string outPath, string password = "");

        bool Decompress(string filePath, string outPath, string password = "");

    }
}
