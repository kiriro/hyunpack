using System;
using System.IO;
using System.Text;

namespace hyunpack
{
    class Program
    {
        private static bool DEBUG_MODE = false;
        private static string HEADER_MAGIC = "HyPack";
        static void Main(string[] args)
        {
            // 检查参数（目标文件）
            // Check argument (target file)
            if (!DEBUG_MODE && (args.Length < 1 || String.IsNullOrWhiteSpace(args[0])) )
            {
                Console.WriteLine("ERROR: No target file specified.");
                return;
            }
            else
            {
                //args = new string[] { "E:\\YureMasterTrial\\Game.pak" };
            }

            // 确认文件
            // confirm the file was specified
            string baseDirectory = Path.GetDirectoryName(args[0]);
            string targetDirectory = null;
            string targetFileName = Path.GetFileName(args[0]);
            if (String.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = ".";
            }
            
            // 读取文件
            // Reading file
            FileStream hypackIn = null;
            try
            {
                hypackIn = new FileStream(DEBUG_MODE ? args[0] : targetFileName, FileMode.Open, FileAccess.Read);
                if (!hypackIn.CanRead)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not read specified file. (" + ex.GetType() + ")");
                return;
            }

            // 创建目标文件夹
            // prepare target directory(folder)
            try
            {
                targetDirectory = baseDirectory + Path.DirectorySeparatorChar + targetFileName.Substring(0, targetFileName.LastIndexOf(".pak"));
                Directory.CreateDirectory(targetDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not create target folder. (" + ex.GetType() + ")");
                return;
            }

            // 处理.pak文件 ============================================================================================
            // Processing .pak file ====================================================================================

            // 解析文件头
            // Analyzing file header
            byte[] hdrMagic = new byte[6];
            byte[] hdrVersion = new byte[2];
            byte[] hdrFiletableAddress = new byte[4];
            byte[] hdrFiles = new byte[4];
            hypackIn.Read(hdrMagic, 0, hdrMagic.Length);
            string hdrMagicString = Encoding.ASCII.GetString(hdrMagic);
            if (!HEADER_MAGIC.Equals(hdrMagicString))
            {
                // 不是有效的文件
                // Not a valid file.
                Console.WriteLine("ERROR: Invalid file.");
                return;
            }
            hypackIn.Read(hdrVersion, 0, hdrVersion.Length);
            hypackIn.Read(hdrFiletableAddress, 0, hdrFiletableAddress.Length);
            hypackIn.Read(hdrFiles, 0, hdrFiles.Length);
            int addrFiletable = BitConverter.ToInt32(hdrFiletableAddress, 0);
            int totalFiles = BitConverter.ToInt32(hdrFiles, 0);
            int baseAddress = 0x10;

            // 解析文件内容
            // Analyzing file content
            long currentPosition = 0L;
            byte[] ftbMainName = new byte[21];
            byte[] ftbCatName = new byte[3];
            byte[] ftbStartAt = new byte[4];
            byte[] ftbSize = new byte[4];
            byte[] ftbSize2 = new byte[4];
            byte[] ftbUnknown = new byte[4];
            byte[] ftbTimestamp = new byte[8]; // for original file was packed before
            hypackIn.Position = baseAddress + addrFiletable;
            Console.WriteLine("Files: " + totalFiles);
            Console.WriteLine("File name                     Size      Position    Status");
            Console.WriteLine("======================================================================");
            byte[] fileContent = null;
            FileStream contentWriter = null;
            for (int i = 0; i < totalFiles; i++)
            {
                // 读取文件索引信息
                // Reading file indexer
                hypackIn.Read(ftbMainName, 0, ftbMainName.Length);
                hypackIn.Read(ftbCatName, 0, ftbCatName.Length);
                hypackIn.Read(ftbStartAt, 0, ftbStartAt.Length);
                hypackIn.Read(ftbSize, 0, ftbSize.Length);
                hypackIn.Read(ftbSize2, 0, ftbSize2.Length);
                hypackIn.Read(ftbUnknown, 0, ftbUnknown.Length);
                hypackIn.Read(ftbTimestamp, 0, ftbTimestamp.Length);
                string filename = Encoding.ASCII.GetString(ftbMainName).TrimEnd('\0') + "." + Encoding.ASCII.GetString(ftbCatName).TrimEnd('\0');
                int filesize = BitConverter.ToInt32(ftbSize, 0);
                int filepos = BitConverter.ToInt32(ftbStartAt, 0);
                // 打印文件索引信息
                // Print out information of current index
                Console.Write(filename.PadRight(30, ' '));
                Console.Write(filesize < 1024 ? filesize.ToString() + " B\t" : (filesize / 1024).ToString() + " KiB\t");
                Console.Write(filepos.ToString("X8").PadRight(12, ' '));
                // 暂存当前地址
                // Hold current position of .pak file
                currentPosition = hypackIn.Position;
                // 设置文件起始地址
                // Setting start position of data file
                hypackIn.Position = baseAddress + filepos;

                // 抽取文件
                // Extracting file
                try
                {
                    fileContent = new byte[filesize];
                    hypackIn.Read(fileContent, 0, filesize);
                    // 创建目标文件
                    // Create target file
                    contentWriter = new FileStream(
                        targetDirectory + Path.DirectorySeparatorChar + filename,
                        FileMode.OpenOrCreate,
                        FileAccess.Write);
                    contentWriter.Write(fileContent, 0, filesize);
                    contentWriter.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine();
                    Console.WriteLine("ERROR: Could not write file. (" + ex.GetType() + ")");
                    Console.WriteLine("(Additional information below)");
                    Console.WriteLine(ex.StackTrace);
                    break;
                }
                finally
                {
                    if (contentWriter != null)
                    {
                        contentWriter.Close();
                    }
                }
                // 输出处理状态，并恢复位置指针
                // Print out "OK" and resume position for next index
                Console.WriteLine("OK");
                hypackIn.Position = currentPosition;
            }
            // 关闭.pak文件
            // Close .pak file
            hypackIn.Close();
        }
    }
}
