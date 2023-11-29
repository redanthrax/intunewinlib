using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using WixToolset.Dtf.WindowsInstaller;

namespace IntuneWinLib {
    internal class MsiUtil {
        private const int PID_SUBJECT = 3;
        private const int PID_AUTHOR = 4;
        private const int PID_REVNUMBER = 9;
        private Database database;

        internal MsiApplicationInfo ReadApplicationInfo(string contentFile) {
            if (!File.Exists(contentFile))
                throw new FileNotFoundException($"File not found: {contentFile}");

            using (var database = new Database(contentFile, DatabaseOpenMode.ReadOnly)) {
                var msiApplicationInfo = new MsiApplicationInfo {
                    Name = RetrieveProductName(),
                    MsiInfo = new MsiInfo {
                        MsiProductCode = ReadProperty(database, "ProductCode"),
                        MsiProductVersion = ReadProperty(database, "ProductVersion"),
                        MsiPackageCode = RetrievePackageCode(database),
                        MsiUpgradeCode = ReadProperty(database, "UpgradeCode", false),
                        MsiPublisher = RetrievePublisher(database),
                        // ... other properties as before
                    }
                };

                // ... rest of the method logic as before ...

                return msiApplicationInfo;
            }
        }

        private string RetrievePropertyWithSummaryInfo(string propertyName, int summaryId) {
            if (this.database == null)
                throw new InvalidOperationException("No opened MSI database");

            // Try retrieving the property directly from the database
            string propertyValue = ReadProperty(this.database, propertyName, false);
            if (!string.IsNullOrEmpty(propertyValue))
                return propertyValue;

            // If the property is not found, try retrieving the equivalent from the summary information
            try {
                using (SummaryInfo summaryInfo = new SummaryInfo(this.database.FilePath, false)) {
                    switch (summaryId) {
                        case 2: // Example for Title
                            return summaryInfo.Title;
                        case 3: // Example for Subject
                            return summaryInfo.Subject;
                        case 4: // Example for Author
                            return summaryInfo.Author;
                        // Add other cases as needed
                        default:
                            return null;
                    }
                }
            }
            catch (COMException) {
                // Handle or log the exception as needed
                return null;
            }
        }

        private string ReadProperty(Database database, string propName, bool mandatory = true) {
            using (View view = database.OpenView($"SELECT `Value` FROM `Property` WHERE `Property` = '{propName}'")) {
                view.Execute();
                using (Record record = view.Fetch()) {
                    if (record != null)
                        return record.GetString(1);
                    else if (mandatory)
                        throw new ArgumentException($"Property not found: {propName}");
                    else
                        return null;
                }
            }
        }

        private string RetrieveProductName() => this.RetrievePropertyWithSummaryInfo("ProductName", 3);
    }
}
