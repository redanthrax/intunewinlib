using System.IO.Compression;
using System.IO.Packaging;
using System.Reflection;
using System.Text;

namespace IntuneWinLib {
    internal static class Compress {
        private static Uri FakeRelativeUri = new Uri("FakeUri", UriKind.Relative);

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

            if (noCompression) {
                CompressFromDirectory(folder, destinationFile, CompressionLevel.NoCompression, includeBaseDirectory);
            }
            else {
                CompressFromDirectory(folder, destinationFile, CompressionLevel.Fastest, includeBaseDirectory, useExistingZipFile);
            }
        }

        private static void CompressFromDirectory(
            string sourceDirectoryName,
            string destinationArchiveFileName,
            CompressionLevel compressionLevel,
            bool includeBaseDirectory) {
            // Ensure the source directory is in full path format
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            // Ensure the destination file is in full path format and the directory exists
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);
            var destinationDirectory = Path.GetDirectoryName(destinationArchiveFileName);
            if (!Directory.Exists(destinationDirectory)) {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Use the ZipFile class to create the zip archive
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);

            // If including empty directories, post-process the zip file
            if (includeBaseDirectory) {
                IncludeEmptyDirectories(sourceDirectoryName, destinationArchiveFileName);
            }
        }

        private static void IncludeEmptyDirectories(string sourceDirectoryName, string destinationArchiveFileName) {
            using (ZipArchive zipArchive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Update)) {
                var allDirectories = Directory.GetDirectories(sourceDirectoryName, "*", SearchOption.AllDirectories);
                foreach (var directory in allDirectories) {
                    if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0) {
                        string directoryNameInArchive = directory.Substring(sourceDirectoryName.Length).Replace('\\', '/') + "/";
                        if (zipArchive.GetEntry(directoryNameInArchive) == null) {
                            zipArchive.CreateEntry(directoryNameInArchive);
                        }
                    }
                }
            }
        }

        private static void CompressFromDirectory(
            string sourceDirectoryName,
            string destinationArchiveFileName,
            CompressionLevel? compressionLevel,
            bool includeBaseDirectory,
            bool useExistingZipFile) {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            // Determine the mode based on whether to append to the existing file or create a new one
            ZipArchiveMode mode = useExistingZipFile ? ZipArchiveMode.Update : ZipArchiveMode.Create;

            using (ZipArchive destination = ZipFile.Open(destinationArchiveFileName, mode)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
                string baseFolder = includeBaseDirectory && directoryInfo.Parent != null
                    ? directoryInfo.Parent.FullName
                    : directoryInfo.FullName;

                foreach (FileSystemInfo item in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)) {
                    string relativePath = item.FullName.Substring(baseFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (item is FileInfo fileInfo) {
                        ZipArchiveEntry entry = destination.CreateEntryFromFile(fileInfo.FullName, relativePath, compressionLevel ?? CompressionLevel.Optimal);
                    }
                    else if (item is DirectoryInfo dir && IsDirEmpty(dir)) {
                        // Create an entry for an empty directory
                        destination.CreateEntry(relativePath + Path.DirectorySeparatorChar);
                    }
                }

                // Optionally, include the base directory itself if it's empty
                if (includeBaseDirectory && IsDirEmpty(directoryInfo)) {
                    destination.CreateEntry(directoryInfo.Name + Path.DirectorySeparatorChar);
                }
            }
        }

        public static void Extract(string packageFile, string outputDirectory) {
            if (!File.Exists(packageFile))
                throw new FileNotFoundException($"Package file '{packageFile}' not found.");

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using (ZipArchive archive = ZipFile.OpenRead(packageFile)) {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    // Skip if the entry is a directory
                    if (entry.FullName.EndsWith(Path.DirectorySeparatorChar))
                        continue;

                    string destinationPath = Path.GetFullPath(Path.Combine(outputDirectory, entry.FullName));

                    // Ensure that the destination path is within the output directory
                    if (!destinationPath.StartsWith(outputDirectory, StringComparison.Ordinal))
                        throw new IOException("Attempt to extract to an outside directory.");

                    // Create the directory of the file if it does not exist
                    string directoryPath = Path.GetDirectoryName(destinationPath);
                    if (directoryPath != null && !Directory.Exists(directoryPath)) {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Extract the file
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }

        private static bool IsDirEmpty(DirectoryInfo possiblyEmptyDir) =>
            !possiblyEmptyDir.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).Any<FileSystemInfo>();
    }
}
