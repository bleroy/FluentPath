using System.IO;
using System.Linq;

namespace Fluent.IO.Windows
{
    public static class WindowsExtensions
    {
        /// <summary>
        /// Decrypts all files in the set.
        /// </summary>
        /// <returns>The set</returns>
        public static Path Decrypt(this Path path)
        {
            foreach (var singlePath in path.ToStringArray().Where(p => !Directory.Exists(p)))
            {
                File.Decrypt(singlePath);
            }
            return path;
        }

        /// <summary>
        /// Encrypts all files in the set.
        /// </summary>
        /// <returns>The set</returns>
        public static Path Encrypt(this Path path)
        {
            foreach (var singlePath in path.ToStringArray().Where(p => !Directory.Exists(p)))
            {
                File.Encrypt(singlePath);
            }
            return path;
        }
    }
}
