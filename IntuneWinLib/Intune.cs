using System.Reflection;
using System.IO.Packaging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Globalization;

namespace IntuneWinLib {
    public class Intune {
        internal const string PackageFileExtension = ".intunewin";
        internal const string ToolVersion = "1.8.5.0";
        private const string CatalogFolderPrefix = "Cat_C029B285-222F-460B-8ECA-2CD7A8A424B7";

        /// <summary>
        /// Create the .intunewin package
        /// </summary>
        /// <param name="folder">The folders where the manifest and setup files exist.</param>
        /// <param name="setupFile">The full path to the setup file.</param>
        /// <param name="outputFolder">The folder for the destinition of the .intunewin file.</param>
        /// <returns>Exit Code</returns>
        public static void CreatePackage(string folder, string setupFile, string outputFolder) {
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

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var outputFileName = GetOutputFileName(setupFile, outputFolder);
            try {
                var winPackPath = Path.Combine(tempDir, "IntuneWinPackage");
                var contentsPath = Path.Combine(winPackPath, "Contents");
                var dstPath = Path.Combine(contentsPath, "IntunePackage.intunewin");

                Compress.Folder(folder, dstPath, false, false);

                var length = new FileInfo(dstPath).Length;
                var fileInfo = new FileInfo(setupFile);
                var ext = fileInfo.Extension != null ? fileInfo.Extension.ToLowerInvariant() : string.Empty;
                var appInfo = !ext.Contains(".msi") ? (ApplicationInfo)new CustomApplicationInfo() : (ApplicationInfo) new ;
                appInfo.FileName = "IntunePackage.intunewin";
                appInfo.Name = string.IsNullOrEmpty(appInfo.Name) ? Path.GetFileName(setupFile) : appInfo.Name;
                appInfo.UnencryptedContentSize = length;
                appInfo.ToolVersion = ToolVersion;
                appInfo.SetupFile = setupFile.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar);
                appInfo.EncryptionInfo = Encrypt.EncryptFile(dstPath);
                var metaDataPath = Path.Combine(winPackPath, "Metadata");
                var detectionFile = Path.Combine(metaDataPath, "Detection.xml");
                var xml = appInfo.ToXml();
                if (!Directory.Exists(metaDataPath)) Directory.CreateDirectory(metaDataPath);
                using (FileStream fileStream = File.Open(detectionFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    byte[] bytes = Encoding.UTF8.GetBytes(xml);
                    fileStream.Write(bytes, 0, bytes.Length);
                }

                Compress.Folder(winPackPath, outputFileName, true, true);
            }
            finally {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        internal static string GetOutputFileName(string setupFile, string outputFolder) => Path.Combine(outputFolder, string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}{1}", (object)Path.GetFileNameWithoutExtension(setupFile), (object)".intunewin"));
    }
}
