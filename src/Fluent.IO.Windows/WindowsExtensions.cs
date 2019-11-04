using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

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
            foreach (var p in path.ToStringArray().Where(p => !Directory.Exists(p)))
            {
                File.Decrypt(p);
            }
            return path;
        }

        /// <summary>
        /// Encrypts all files in the set.
        /// </summary>
        /// <returns>The set</returns>
        public static Path Encrypt(this Path path)
        {
            foreach (var p in path.ToStringArray().Where(p => !Directory.Exists(p)))
            {
                File.Encrypt(p);
            }
            return path;
        }

        /// <summary>
        /// The access control security information for the first path in the collection.
        /// </summary>
        /// <returns>The security information</returns>
        public static FileSystemSecurity AccessControl(this Path path)
        {
            var firstPath = path.ToStringArray().FirstOrDefault();
            if (firstPath == null) throw new InvalidOperationException("Can't get access control from an empty path.");
            return Directory.Exists(firstPath)
                ? new DirectoryInfo(firstPath).GetAccessControl()
                : (FileSystemSecurity)new FileInfo(firstPath).GetAccessControl();
        }

        /// <summary>
        /// The access control security information for the first path in the collection.
        /// </summary>
        /// <param name="action">An action that gets called for each path in the set.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, Action<FileSystemSecurity> action)
            => AccessControl(path, (p, fss) => action(fss));

        /// <summary>
        /// The access control security information for the first path in the collection.
        /// </summary>
        /// <param name="action">An action that gets called for each path in the set.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, Action<Path, FileSystemSecurity> action)
        {
            foreach (var p in path.ToStringArray())
            {
                action(new Path(p, path),
                    Directory.Exists(p)
                        ? new DirectoryInfo(p).GetAccessControl()
                        : (FileSystemSecurity)new FileInfo(p).GetAccessControl()
                    );
            }
            return path;
        }

        /// <summary>
        /// Sets the access control security on all files and directories in the set.
        /// </summary>
        /// <param name="security">The security to apply.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, FileSystemSecurity security) => AccessControl(path, p => security);

        /// <summary>
        /// Sets the access control security on all files and directories in the set.
        /// </summary>
        /// <param name="securityFunction">A function that returns the security for each path.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, Func<Path, FileSystemSecurity> securityFunction)
        {
            foreach (var p in path.ToStringArray())
            {
                if (Directory.Exists(p))
                {
                    new DirectoryInfo(p).SetAccessControl(
                        (DirectorySecurity)securityFunction(new Path(p, path)));
                }
                else
                {
                    new FileInfo(p).SetAccessControl(
                        (FileSecurity)securityFunction(new Path(p, path)));
                }
            }
            return path;
        }
    }
}
