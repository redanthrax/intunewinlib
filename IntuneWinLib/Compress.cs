using System.IO.Compression;
using System.IO.Packaging;
using System.Reflection;
using System.Text;

namespace IntuneWinLib {
    internal static class Compress {
        private static Uri FakeRelativeUri = new Uri("FakeUri", UriKind.Relative);

        /// <summary>
        /// Create a compressed "zipped" folder
        /// </summary>
        /// <param name="folder">The folder to compress.</param>
        /// <param name="destinationFile">The compressed file to create.</param>
        /// <param name="noCompression">Use no compression or fastest compression.</param>
        /// <param name="includeBaseDirectory">Include the folder specified in the compressed file.</param>
        internal static void Folder(
            string folder,
            string destinationFile,
            bool noCompression,
            bool includeBaseDirectory,
            bool useExistingZipFile = false) {
            var dirName = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            if(File.Exists(destinationFile)) File.Delete(destinationFile);

            var dirInfo = new DirectoryInfo(folder);
            if (noCompression) {
                CompressFromDirectory(folder, destinationFile, CompressionLevel.NoCompression, includeBaseDirectory);
            }
            else {
                CompressFromDirectory(folder, destinationFile, CompressionLevel.Fastest, includeBaseDirectory, useExistingZipFile);
            }
        }

        private static void CompressFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel? compressionLevel, bool includeBaseDirectory) {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            HashSet<string> addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (FileStream zipToOpen = new FileStream(destinationArchiveFileName, FileMode.OpenOrCreate)) {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)) {
                    DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
                    string baseFolder = directoryInfo.FullName;

                    if (includeBaseDirectory && directoryInfo.Parent != null) {
                        baseFolder = directoryInfo.Parent.FullName;
                    }

                    foreach (FileSystemInfo fileSystemInfo in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)) {
                        string entryName = fileSystemInfo.FullName.Substring(baseFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        if (fileSystemInfo is FileInfo fileInfo) {
                            // Create entry for file
                            CompressionLevel effectiveCompressionLevel = compressionLevel ?? CompressionLevel.Optimal;
                            archive.CreateEntryFromFile(fileInfo.FullName, entryName, effectiveCompressionLevel);
                            addedEntries.Add(entryName);
                        }
                        else if (fileSystemInfo is DirectoryInfo dirInfo && IsDirEmpty(dirInfo)) {
                            // Create entry for directory (if necessary and if it's empty)
                            string dirEntryName = entryName + Path.DirectorySeparatorChar;
                            if (!addedEntries.Contains(dirEntryName)) {
                                archive.CreateEntry(dirEntryName);
                                addedEntries.Add(dirEntryName);
                            }
                        }
                    }

                    if (includeBaseDirectory) {
                        string rootEntryName = directoryInfo.Name + Path.DirectorySeparatorChar;
                        if (!addedEntries.Contains(rootEntryName)) {
                            archive.CreateEntry(rootEntryName);
                            addedEntries.Add(rootEntryName);
                        }
                    }
                }
            }
        }


        private static void CompressFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel? compressionLevel, bool includeBaseDirectory, bool useExistingZipFile) {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);
            ZipArchiveMode mode = useExistingZipFile ? ZipArchiveMode.Update : ZipArchiveMode.Create;

            HashSet<string> addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, mode, Encoding.UTF8)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
                string baseFolder = includeBaseDirectory && directoryInfo.Parent != null ? directoryInfo.Parent.FullName : directoryInfo.FullName;

                foreach (FileSystemInfo fileSystemInfo in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)) {
                    string entryName = fileSystemInfo.FullName.Substring(baseFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (fileSystemInfo is FileInfo fileInfo) {
                        // Create entry for file
                        CompressionLevel effectiveCompressionLevel = compressionLevel ?? CompressionLevel.Optimal;
                        archive.CreateEntryFromFile(fileInfo.FullName, entryName, effectiveCompressionLevel);
                        addedEntries.Add(entryName);
                    }
                    else if (fileSystemInfo is DirectoryInfo dirInfo && IsDirEmpty(dirInfo)) {
                        // Create entry for empty directory (if necessary)
                        string dirEntryName = entryName + Path.DirectorySeparatorChar;
                        if (!addedEntries.Contains(dirEntryName)) {
                            archive.CreateEntry(dirEntryName);
                            addedEntries.Add(dirEntryName);
                        }
                    }
                }

                if (includeBaseDirectory) {
                    string rootEntryName = directoryInfo.Name + Path.DirectorySeparatorChar;
                    if (!addedEntries.Contains(rootEntryName)) {
                        archive.CreateEntry(rootEntryName);
                        addedEntries.Add(rootEntryName);
                    }
                }
            }
        }


        private static void Entry(
            Package package,
            FileInfo file,
            string entryName,
            CompressionOption compressionOption
            ) {
            if (package == null) throw new ArgumentNullException(nameof(package));

            if (file == null) throw new ArgumentNullException(nameof(file));

            var relativeUri = entryName != null ? 
                new Uri(entryName, UriKind.Relative) :
                throw new ArgumentNullException(nameof(entryName));

            var partUri = (Uri)PackUriHelper.CreatePartUri(FakeRelativeUri)
                .GetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, (Binder)null, new Type[1] {
                    typeof(string)
                }, (ParameterModifier[])null).Invoke(new object[1] {
                    (object)GetStringForPartUriFromAnyUri(new Uri(new Uri("http://defaultcontainer/"), relativeUri))
                });

            var part = package.CreatePart(partUri, string.Empty, compressionOption);
            using (Stream stream1 = (Stream)File.Open(file.FullName, FileMode.Open,
                FileAccess.Read, FileShare.Read)) {
                using (Stream stream2 = part.GetStream()) {
                    stream1.CopyTo(stream2, 2097152);
                }
            }
        }

        private static string GetStringForPartUriFromAnyUri(Uri partUri) {
            Uri uri;
            if (!partUri.IsAbsoluteUri) {
                uri = new Uri(partUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.Relative);
            }
            else {
                UriComponents components = UriComponents.Path | UriComponents.KeepDelimiter;
                if (partUri.AbsoluteUri.Contains("#"))
                    components |= UriComponents.Fragment;
                uri = new Uri(partUri.GetComponents(components, UriFormat.SafeUnescaped), UriKind.Relative);
            }

            return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.Unescaped);
        }

        private static bool IsDirEmpty(DirectoryInfo possiblyEmptyDir) =>
            !possiblyEmptyDir.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).Any<FileSystemInfo>();
    }
}
