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
        private Database? database;

        internal MsiApplicationInfo ReadApplicationInfo(string contentFile) {
            if (!File.Exists(contentFile))
                throw new FileNotFoundException($"File not found: {contentFile}");

            using (this.database = new Database(contentFile, DatabaseOpenMode.ReadOnly)) {
                var msiApplicationInfo = new MsiApplicationInfo {
                    Name = RetrieveProductName(),
                    MsiInfo = new MsiInfo {
                        MsiProductCode = ReadProperty("ProductCode"),
                        MsiProductVersion = ReadProperty("ProductVersion"),
                        MsiPackageCode = RetrievePackageCode(),
                        MsiUpgradeCode = ReadProperty("UpgradeCode", false),
                        MsiPublisher = RetrievePublisher(),
                        MsiIsMachineInstall = !UserInstall(),
                        MsiIsUserInstall = UserInstall(),
                        MsiIncludesServices = IncludesServices(),
                        MsiIncludesODBCDataSource = IncludesODBCDataSource(),
                        MsiContainsSystemRegistryKeys = ContainsSystemRegistryKeys(),
                        MsiContainsSystemFolders = ContainsSystemFolders()
                    }
                };

                var allusers = this.ReadProperty("ALLUSERS", false);
                if(string.IsNullOrEmpty(allusers)) {
                    msiApplicationInfo.MsiInfo.MsiExecutionContext = ExecutionContext.User;
                }
                else {
                    switch (allusers) {
                        case "1":
                            msiApplicationInfo.MsiInfo.MsiExecutionContext = ExecutionContext.System;
                            break;
                        case "2":
                            msiApplicationInfo.MsiInfo.MsiExecutionContext = ExecutionContext.Any;
                            break;
                        default:
                            throw new InvalidDataException("Invalid ALLUSERS property value");
                    }
                }

                if(msiApplicationInfo.MsiInfo.MsiExecutionContext == ExecutionContext.User) {
                    msiApplicationInfo.MsiInfo.MsiRequiresLogon = true;
                }

                var reboot = this.ReadProperty("REBOOT", false);
                if(!string.IsNullOrEmpty(reboot) && reboot[0] == 'F') {
                    msiApplicationInfo.MsiInfo.MsiRequiresReboot = true;
                }

                return msiApplicationInfo;
            }
        }

        private string RetrievePropertyWithSummaryInfo(string propertyName, int summaryId) {
            if (this.database == null)
                throw new InvalidOperationException("No opened MSI database");

            // Try retrieving the property directly from the database
            string propertyValue = ReadProperty(propertyName, false);
            if (!string.IsNullOrEmpty(propertyValue))
                return propertyValue;

            // If the property is not found, try retrieving the equivalent from the summary information
            try {
                using (SummaryInfo summaryInfo = new SummaryInfo(this.database.FilePath, false)) {
                    switch (summaryId) {
                        case 2: // Title
                            return summaryInfo.Title;
                        case 3: // Subject
                            return summaryInfo.Subject;
                        case 4: // Author
                            return summaryInfo.Author;
                        case 5: // Keywords
                            return summaryInfo.Keywords;
                        case 6: // Comments
                            return summaryInfo.Comments;
                        case 7: // Template
                            return summaryInfo.Template;
                        case 8: // Last Saved By
                            return summaryInfo.LastSavedBy;
                        case 9: // Revision Number
                            return summaryInfo.RevisionNumber;
                        case 14: // Number of Pages
                            return summaryInfo.PageCount.ToString();
                        case 15: // Number of Words
                            return summaryInfo.WordCount.ToString();
                        case 16: // Number of Characters
                            return summaryInfo.CharacterCount.ToString();
                        case 19: // Security
                            return summaryInfo.Security.ToString();
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

        private string ReadProperty(string propName, bool mandatory = true) {
            using (View view = this.database.OpenView($"SELECT `Value` FROM `Property` WHERE `Property` = '{propName}'")) {
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

        private View Query(string table, string columns) {
            try {
                var query = $"SELECT {columns} FROM `{table}`";
                var view = this.database.OpenView(query);
                view.Execute();
                return view;
            }
            catch (BadQuerySyntaxException ex) {
                throw new ArgumentException($"Table not found: {table}", nameof(table), ex);
            }
            catch (InvalidHandleException ex) {
                throw new ArgumentException($"Table not found: {table}", nameof(table), ex);
            }
        }

        private bool TableContainsRecords(string table, string column) {
            try {
                return Query(table, column).Fetch() != null;
            }
            catch (ArgumentException) {
            }
            catch (COMException) {
            }

            return false;
        }

        private bool IncludesODBCDataSource() => this.TableContainsRecords("ODBCDataSource", "DataSource");

        private bool IncludesServices() => this.TableContainsRecords("ServiceInstall", "ServiceInstall");

        private string RetrieveProductName() => this.RetrievePropertyWithSummaryInfo("ProductName", 3);

        private string RetrievePackageCode() => this.RetrievePropertyWithSummaryInfo("PackageCode", 9);

        private string RetrievePublisher() => this.RetrievePropertyWithSummaryInfo("Manufacturer", 4);

        private bool ContainsSystemRegistryKeys() => this.ContainsRegistrySystemKeys("Registry") || this.ContainsRegistrySystemKeys("RemoveRegistry");

        private bool UserInstall() {
            string allUsers = ReadProperty("ALLUSERS", false);
            string msiInstallPerUser = ReadProperty("MSIINSTALLPERUSER", false);

            return !string.Equals(allUsers, "1", StringComparison.OrdinalIgnoreCase) &&
                   (!string.Equals(allUsers, "2", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(msiInstallPerUser));
        }

        private bool ContainsRegistrySystemKeys(string table) {
            try {
                var view = this.Query(table, "Root");

                for (var record = view.Fetch(); record != null; record = view.Fetch()) {
                    var rootValue = record.GetString(1);

                    if (string.Equals(rootValue, "2", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(rootValue, "3", StringComparison.OrdinalIgnoreCase) ||
                        (string.Equals(rootValue, "-1", StringComparison.OrdinalIgnoreCase) && !this.UserInstall())) {
                        return true;
                    }
                }
            }
            catch (ArgumentException) {
            }
            catch (COMException) {
            }

            return false;
        }

        private bool ContainsSystemFolders() {
            var view = this.Query("Directory", "Directory");

            foreach (var record in FetchRecords(view)) {
                var directoryName = record.GetString(1);

                var systemFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
            "AdminToolsFolder", "CommonAppDataFolder", "FontsFolder",
            "System16Folder", "System64Folder", "SystemFolder",
            "TempFolder", "WindowsFolder", "WindowsVolume"
        };

                if (systemFolders.Contains(directoryName)) {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<Record> FetchRecords(View view) {
            for (var record = view.Fetch(); record != null; record = view.Fetch()) {
                yield return record;
            }
        }

    }
}
