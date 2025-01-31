using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StudioDrydock.AppStoreConnect.Cli
{
    internal class AssetDatabase
    {
        private readonly DirectoryInfo m_Dir;
        private readonly List<(string Hash, FileInfo FileInfo)> m_Files = new();

        public AssetDatabase(DirectoryInfo dir)
        {
            m_Dir = dir;

            foreach (var fi in m_Dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                m_Files.Add((HashFile(fi), fi));
            }
        }

        public FileInfo? FindFileByHashOrName(string? hash, string? name)
        {
            var file = m_Files.FirstOrDefault(x => x.FileInfo.Name == name && x.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

            if (file.FileInfo != null)
            {
                return file.FileInfo;
            }

            file = m_Files.FirstOrDefault(x => x.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

            if (file.FileInfo != null)
            {
                return file.FileInfo;
            }

            if (name != null)
            {
                return new FileInfo(Path.Combine(m_Dir.FullName, name));
            }

            return null;
        }

        public FileInfo GetFileByName(string name, out string hash)
        {
            var fi = new FileInfo(Path.Combine(m_Dir.FullName, name));
            hash = HashFile(fi);
            return fi;
        }

        private string HashFile(FileInfo fi)
        {
            using var md5 = MD5.Create();
            using var stream = fi.OpenRead();
            return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
        }
    }
}
