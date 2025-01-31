using System.Reflection;
using System.IO.Packaging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Globalization;
using System.Xml.Serialization;
using System.Xml;

namespace IntuneWinLib {
    public class Intune {
        internal const string PackageFileExtension = ".intunewin";
        internal const string ToolVersion = "1.8.5.0";

        /// <summary>
        /// Create the .intunewin package
        /// </summary>
        /// <param name="folder">The folders where the manifest and setup files exist.</param>
        /// <param name="setupFile">The full path to the setup file.</param>
        /// <param name="outputFolder">The folder for the destinition of the .intunewin file.</param>
        /// <param name="tempDir">Temp directory for local storage. Optional.</param>
        /// <returns>Exit Code</returns>
        public static string CreatePackage(string folder, string setupFile, string outputFolder, string tempDir = "") {
#if DEBUG
            //This is for calling the method to attach and debug via pwsh
            Thread.Sleep(10000);
#endif
            if (!Directory.Exists(folder))
                throw new Exception($"Folder \"{folder}\" does not exist!");

            if (!File.Exists(setupFile))
                throw new Exception($"File \"{setupFile}\" does not exist!");

            if (!Directory.Exists(outputFolder))
                throw new Exception($"Output Folder ${outputFolder} does not exist!");

            if (string.IsNullOrEmpty(tempDir)) Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var outputFileName = GetOutputFileName(setupFile, outputFolder);
            try {
                var winPackPath = Path.Combine(tempDir, "IntuneWinPackage");
                var contentsPath = Path.Combine(winPackPath, "Contents");
                var dstPath = Path.Combine(contentsPath, $"{outputFileName}.intunewin");

                Compress.Folder(folder, dstPath, false, false);

                var length = new FileInfo(dstPath).Length;
                var fileInfo = new FileInfo(setupFile);
                var ext = fileInfo.Extension != null ? fileInfo.Extension.ToLowerInvariant() : string.Empty;
                var appInfo = !ext.Contains(".msi") ? (ApplicationInfo)new CustomApplicationInfo() : (ApplicationInfo) new MsiUtil().ReadApplicationInfo(setupFile);
                appInfo.FileName = $"{outputFileName}.intunewin";
                appInfo.Name = string.IsNullOrEmpty(appInfo.Name) ? Path.GetFileName(setupFile) : appInfo.Name;
                appInfo.UnencryptedContentSize = length;
                appInfo.ToolVersion = ToolVersion;
                appInfo.SetupFile = setupFile.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar);
                appInfo.EncryptionInfo = Encrypt.EncryptFile(dstPath);
                var metaDataPath = Path.Combine(winPackPath, "Metadata");
                var detectionFile = Path.Combine(metaDataPath, "Detection.xml");
                if (!Directory.Exists(metaDataPath)) Directory.CreateDirectory(metaDataPath);

                var xml = appInfo.ToXml();
                using (FileStream fileStream = File.Open(detectionFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    byte[] bytes = Encoding.UTF8.GetBytes(xml);
                    fileStream.Write(bytes, 0, bytes.Length);
                }

                Compress.Folder(winPackPath, outputFileName, true, true);
            }
            finally {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }

            return outputFileName;
        }

        public static void ExtractPackage(string packageFile, string outputFolder) {
#if DEBUG
            //This is for calling the method to attach and debug via pwsh
            Thread.Sleep(10000);
#endif
            if (!File.Exists(packageFile))
                throw new Exception($"Package file \"{packageFile}\" does not exist!");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try {
                // Decompress the package
                var winPackPath = Path.Combine(tempDir, "IntuneWinPackageExtracted");
                Directory.CreateDirectory(winPackPath);
                Compress.Extract(packageFile, winPackPath);

                // Extract metadata and read application info
                var metaDataPath = Path.Combine(winPackPath, "IntuneWinPackage", "Metadata");
                var detectionFile = Path.Combine(metaDataPath, "Detection.xml");
                ApplicationInfo appInfo = ReadApplicationInfoFromXml(detectionFile);

                // Decrypt the file
                var contentsPath = Path.Combine(winPackPath, "IntuneWinPackage", "Contents");
                var encryptedFilePath = Path.Combine(contentsPath, "IntunePackage.intunewin");
                var decryptedFilePath = Path.Combine(outputFolder, appInfo.FileName);
                if (!File.Exists(detectionFile))
                    throw new FileNotFoundException($"The file {detectionFile} was not found.");

                Encrypt.DecryptFile(encryptedFilePath, appInfo.EncryptionInfo);
                CopyFolder(winPackPath, outputFolder);
            }
            finally {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        public static void CopyFolder(string sourceFolder, string destFolder) {
            // Create the destination folder if it does not exist
            Directory.CreateDirectory(destFolder);

            // Copy each file into the new directory
            foreach (string filePath in Directory.GetFiles(sourceFolder)) {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destFolder, fileName);
                File.Copy(filePath, destFilePath, true); // true to overwrite if file already exists
            }

            // Copy each subdirectory using recursion
            foreach (string subdirectoryPath in Directory.GetDirectories(sourceFolder)) {
                string subdirectoryName = Path.GetFileName(subdirectoryPath);
                string destSubdirectoryPath = Path.Combine(destFolder, subdirectoryName);
                CopyFolder(subdirectoryPath, destSubdirectoryPath);
            }
        }

        private static ApplicationInfo ReadApplicationInfoFromXml(string xmlFilePath) {
            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException($"The file {xmlFilePath} was not found.");

            var serializer = new XmlSerializer(typeof(ApplicationInfo));
            string xmlContent = File.ReadAllText(xmlFilePath);

            // Check if the XML declaration is missing
            if (!xmlContent.TrimStart().StartsWith("<?xml")) {
                // Add XML declaration
                xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xmlContent;
            }

            using (var reader = new StringReader(xmlContent)) {
                return serializer.Deserialize(reader) as ApplicationInfo;
            }
        }

        internal static string GetOutputFileName(string setupFile, string outputFolder) {
            return Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(setupFile)}.intunewin");
        }

    }
}
