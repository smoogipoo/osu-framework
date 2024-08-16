// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Framework.Utils;

namespace osu.Framework.Testing
{
    public class InMemoryStorage : Storage
    {
        protected override IFileSystem FileSystem => fs;

        private readonly MockFileSystem fs;
        private readonly GameHost? host;

        public InMemoryStorage(string path, GameHost? host = null, MockFileSystem? fs = null)
            : base(path)
        {
            this.host = host;
            this.fs = fs ?? new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = BasePath });
        }

        public override bool Exists(string path) => FileSystem.File.Exists(GetFullPath(path));

        public override bool ExistsDirectory(string path) => FileSystem.Directory.Exists(GetFullPath(path));

        public override void DeleteDirectory(string path)
        {
            path = GetFullPath(path);

            // handles the case where the directory doesn't exist, which will throw a DirectoryNotFoundException.
            if (FileSystem.Directory.Exists(path))
                FileSystem.Directory.Delete(path, true);
        }

        public override void Delete(string path)
        {
            path = GetFullPath(path);

            if (FileSystem.File.Exists(path))
                FileSystem.File.Delete(path);
        }

        public override void Move(string from, string to)
        {
            // Retry move operations as it can fail on windows intermittently with IOExceptions:
            // The process cannot access the file because it is being used by another process.
            General.AttemptWithRetryOnException<IOException>(() => FileSystem.File.Move(GetFullPath(from), GetFullPath(to), true));
        }

        public override IEnumerable<string> GetDirectories(string path) => getRelativePaths(FileSystem.Directory.GetDirectories(GetFullPath(path)));

        public override IEnumerable<string> GetFiles(string path, string pattern = "*") => getRelativePaths(FileSystem.Directory.GetFiles(GetFullPath(path), pattern));

        private IEnumerable<string> getRelativePaths(IEnumerable<string> paths)
        {
            string basePath = FileSystem.Path.GetFullPath(GetFullPath(string.Empty));
            return paths.Select(FileSystem.Path.GetFullPath).Select(path =>
            {
                if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"\"{path}\" does not start with \"{basePath}\" and is probably malformed");

                return path.AsSpan(basePath.Length).TrimStart(FileSystem.Path.DirectorySeparatorChar).ToString();
            });
        }

        public override string GetFullPath(string path, bool createIfNotExisting = false)
        {
            path = path.Replace(FileSystem.Path.AltDirectorySeparatorChar, FileSystem.Path.DirectorySeparatorChar);

            string basePath = FileSystem.Path.GetFullPath(BasePath).TrimEnd(FileSystem.Path.DirectorySeparatorChar);
            string resolvedPath = FileSystem.Path.GetFullPath(FileSystem.Path.Combine(basePath, path));

            if (!resolvedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"\"{resolvedPath}\" traverses outside of \"{basePath}\" and is probably malformed");

            if (createIfNotExisting) FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(resolvedPath).AsNonNull());
            return resolvedPath;
        }

        public override bool OpenFileExternally(string filename) =>
            host?.OpenFileExternally(GetFullPath(filename)) == true;

        public override bool PresentFileExternally(string filename) =>
            host?.PresentFileExternally(GetFullPath(filename)) == true;

        public override Stream? GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
        {
            path = GetFullPath(path, access != FileAccess.Read);

            ArgumentException.ThrowIfNullOrEmpty(path);

            switch (access)
            {
                case FileAccess.Read:
                    if (!FileSystem.File.Exists(path)) return null;

                    return FileSystem.File.Open(path, FileMode.Open, access, FileShare.Read);

                default:
                    // this was added to work around some hardware writing zeroes to a file
                    // before writing actual content, causing corrupt files to exist on disk.
                    // as of .NET 6, flushing is very expensive on macOS so this is limited to only Windows.
                    if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                        return new FlushingStream(path, mode, access);

                    return new FileStream(path, mode, access);
            }
        }

        public override Storage GetStorageForDirectory(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            if (path.Length > 0 && !path.EndsWith(FileSystem.Path.DirectorySeparatorChar))
                path += FileSystem.Path.DirectorySeparatorChar;

            // create non-existing path.
            string fullPath = GetFullPath(path, true);

            return (Storage)Activator.CreateInstance(GetType(), fullPath, host, fs)!;
        }
    }
}
