using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gusheshe.Data
{
    public static class SQLiteHelper
    {
        private static readonly string DbFile =
            Path.Combine(Application.StartupPath, "school.db");

        private static readonly string ConnString =
            $"Data Source={DbFile};Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbFile))
            {
                SQLiteConnection.CreateFile(DbFile);
            }

            using (SQLiteConnection conn = new SQLiteConnection(ConnString))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    // Create all tables
                    cmd.CommandText = GetDatabaseSchema();
                    cmd.ExecuteNonQuery();
                }

               
            }

            // Perform any necessary upgrades
            UpgradeDatabase();
        }

        private static string GetDatabaseSchema()
        {
            return @"
                -- Students table (existing)
                CREATE TABLE IF NOT EXISTS Students (
                    StudentId      INTEGER PRIMARY KEY AUTOINCREMENT,
                    AdmissionNo    TEXT    UNIQUE,
                    FirstName      TEXT    NOT NULL,
                    LastName       TEXT    NOT NULL,
                    Class          TEXT    NOT NULL,
                    DOB            TEXT,
                    Gender         TEXT,
                    GuardianName   TEXT,
                    Contact        TEXT,
                    Address        TEXT,
                    ExpectedFees   REAL    NOT NULL DEFAULT 0
                );

                -- Academic Terms table (NEW)
                CREATE TABLE IF NOT EXISTS AcademicTerms (
                    TermId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TermName TEXT NOT NULL,
                    StartDate DATE NOT NULL,
                    EndDate DATE NOT NULL,
                    IsActive BOOLEAN DEFAULT 0,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- Student Fee Structure per Term (NEW)
                CREATE TABLE IF NOT EXISTS StudentTermFees (
                    StudentTermFeeId INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    TermId INTEGER NOT NULL,
                    ExpectedFees REAL NOT NULL DEFAULT 0,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
                    FOREIGN KEY (TermId) REFERENCES AcademicTerms(TermId),
                    UNIQUE(StudentId, TermId)
                );

                -- Enhanced Fee Payments with Term support
                CREATE TABLE IF NOT EXISTS FeePayments (
                    PaymentId  INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId  INTEGER NOT NULL,
                    TermId     INTEGER,
                    AmountPaid REAL NOT NULL,
                    DatePaid   TEXT NOT NULL,
                    Description TEXT,
                    IsSynced   INTEGER DEFAULT 0,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
                    FOREIGN KEY (TermId) REFERENCES AcademicTerms(TermId)
                );

                -- Term-specific Fee Accounts (NEW)
                CREATE TABLE IF NOT EXISTS TermFeeAccounts (
                    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    TermId INTEGER NOT NULL,
                    ExpectedFees REAL NOT NULL DEFAULT 0,
                    TotalPaid REAL DEFAULT 0,
                    Balance REAL DEFAULT 0,
                    LastPaymentDate DATETIME,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
                    FOREIGN KEY (TermId) REFERENCES AcademicTerms(TermId),
                    UNIQUE(StudentId, TermId)
                );

                -- Legacy Fee Accounts (keep for backward compatibility)
                CREATE TABLE IF NOT EXISTS FeeAccounts (
                    StudentId        INTEGER PRIMARY KEY,
                    TotalPaid        REAL    NOT NULL DEFAULT 0,
                    LastPaymentDate  TEXT,
                    Arrears          REAL    NOT NULL DEFAULT 0,
                    FOREIGN KEY (StudentId) REFERENCES Students(StudentId)
                );

                -- Daily Sales Summary (NEW)
                CREATE TABLE IF NOT EXISTS DailySales (
                    SalesId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SalesDate DATE NOT NULL,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    TransactionCount INTEGER NOT NULL DEFAULT 0,
                    GeneratedBy TEXT,
                    GeneratedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(SalesDate)
                );

                -- Uniform Items (existing)
                CREATE TABLE IF NOT EXISTS UniformItems (
                    ItemId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemName TEXT,
                    Price    REAL,
                    Quantity INTEGER
                );

                -- Uniform Sales (existing)
                CREATE TABLE IF NOT EXISTS UniformSales (
                    SaleId      INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemName    TEXT,
                    Quantity    INTEGER,
                    UnitPrice   REAL,
                    Total       REAL,
                    FirstName   TEXT,
                    LastName    TEXT,
                    Class       TEXT, 
                    DateSold    TEXT
                );

                -- General Payments (existing)
                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER,
                    Amount REAL NOT NULL,
                    PaymentType TEXT NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    Description TEXT,
                    FOREIGN KEY (StudentId) REFERENCES Students (StudentId)
                );

                -- Create indexes for better performance
                CREATE INDEX IF NOT EXISTS idx_student_term_fees ON StudentTermFees(StudentId, TermId);
                CREATE INDEX IF NOT EXISTS idx_term_fee_accounts ON TermFeeAccounts(StudentId, TermId);
                CREATE INDEX IF NOT EXISTS idx_fee_payments_term ON FeePayments(TermId, DatePaid);
                CREATE INDEX IF NOT EXISTS idx_fee_payments_student ON FeePayments(StudentId, DatePaid);
                CREATE INDEX IF NOT EXISTS idx_daily_sales_date ON DailySales(SalesDate);
                CREATE INDEX IF NOT EXISTS idx_students_class ON Students(Class);
                CREATE INDEX IF NOT EXISTS idx_uniform_sales_date ON UniformSales(DateSold);
            ";
        }


        private static void UpgradeDatabase()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(ConnString))
                {
                    conn.Open();

                    // Check if we need to add TermId column to FeePayments
                    bool needsTermIdUpgrade = false;
                    using (SQLiteCommand cmd = new SQLiteCommand("PRAGMA table_info(FeePayments)", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        bool hasTermId = false;
                        while (reader.Read())
                        {
                            if (reader["name"].ToString().Equals("TermId", StringComparison.OrdinalIgnoreCase))
                            {
                                hasTermId = true;
                                break;
                            }
                        }
                        needsTermIdUpgrade = !hasTermId;
                    }

                    if (needsTermIdUpgrade)
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(conn))
                        {
                            cmd.CommandText = "ALTER TABLE FeePayments ADD COLUMN TermId INTEGER";
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Check if we need to add CreatedDate column to FeePayments
                    bool needsCreatedDateUpgrade = false;
                    using (SQLiteCommand cmd = new SQLiteCommand("PRAGMA table_info(FeePayments)", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        bool hasCreatedDate = false;
                        while (reader.Read())
                        {
                            if (reader["name"].ToString().Equals("CreatedDate", StringComparison.OrdinalIgnoreCase))
                            {
                                hasCreatedDate = true;
                                break;
                            }
                        }
                        needsCreatedDateUpgrade = !hasCreatedDate;
                    }

                    if (needsCreatedDateUpgrade)
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(conn))
                        {
                            cmd.CommandText = "ALTER TABLE FeePayments ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP";
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Migrate existing data to new structure if needed
                    MigrateExistingData(conn);
                }
            }
            catch (Exception ex)
            {
                // Log upgrade errors but don't crash the application
                MessageBox.Show($"Database upgrade warning: {ex.Message}", "Upgrade Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void MigrateExistingData(SQLiteConnection conn)
        {
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    // Check if migration is needed
                    cmd.CommandText = "SELECT COUNT(*) FROM StudentTermFees";
                    int existingTermFees = Convert.ToInt32(cmd.ExecuteScalar());

                    if (existingTermFees == 0)
                    {
                        // Get the first (active) term
                        cmd.CommandText = "SELECT TermId FROM AcademicTerms WHERE IsActive = 1 ORDER BY TermId LIMIT 1";
                        var activeTermId = cmd.ExecuteScalar();

                        if (activeTermId == null)
                        {
                            cmd.CommandText = "SELECT TermId FROM AcademicTerms ORDER BY TermId LIMIT 1";
                            activeTermId = cmd.ExecuteScalar();
                        }

                        if (activeTermId != null)
                        {
                            int termId = Convert.ToInt32(activeTermId);

                            // Migrate student fees to term-based structure
                            cmd.CommandText = @"
                                INSERT OR IGNORE INTO StudentTermFees (StudentId, TermId, ExpectedFees)
                                SELECT StudentId, @termId, ExpectedFees 
                                FROM Students 
                                WHERE ExpectedFees > 0";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@termId", termId);
                            int migratedStudents = cmd.ExecuteNonQuery();

                            // Migrate fee accounts to term-based structure
                            cmd.CommandText = @"
                                INSERT OR IGNORE INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, LastPaymentDate)
                                SELECT 
                                    s.StudentId, 
                                    @termId, 
                                    s.ExpectedFees,
                                    COALESCE(fa.TotalPaid, 0),
                                    COALESCE(fa.Arrears, s.ExpectedFees),
                                    fa.LastPaymentDate
                                FROM Students s
                                LEFT JOIN FeeAccounts fa ON s.StudentId = fa.StudentId
                                WHERE s.ExpectedFees > 0";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@termId", termId);
                            cmd.ExecuteNonQuery();

                            // Update existing payments with term information
                            cmd.CommandText = "UPDATE FeePayments SET TermId = @termId WHERE TermId IS NULL";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@termId", termId);
                            int updatedPayments = cmd.ExecuteNonQuery();

                            if (migratedStudents > 0)
                            {
                                MessageBox.Show($"Migration completed!\n\n" +
                                              $"• Migrated {migratedStudents} students to term-based fees\n" +
                                              $"• Updated {updatedPayments} payment records\n" +
                                              $"• Set up accounts for current term\n\n" +
                                              "Your system now supports multiple terms!",
                                              "Database Migration Complete",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Migration error: {ex.Message}", "Migration Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(ConnString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Gets the currently active term ID
        /// </summary>
        public static int GetActiveTermId()
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SQLiteCommand("SELECT TermId FROM AcademicTerms WHERE IsActive = 1 LIMIT 1", conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }

                    // If no active term, return the first term
                    cmd.CommandText = "SELECT TermId FROM AcademicTerms ORDER BY TermId LIMIT 1";
                    result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the database version for upgrade tracking
        /// </summary>
        public static int GetDatabaseVersion()
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SQLiteCommand("PRAGMA user_version", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Sets the database version
        /// </summary>
        public static void SetDatabaseVersion(int version)
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SQLiteCommand($"PRAGMA user_version = {version}", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting database version: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Backup the database to a specified location
        /// </summary>
        public static bool BackupDatabase(string backupPath)
        {
            try
            {
                if (File.Exists(DbFile))
                {
                    File.Copy(DbFile, backupPath, true);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Restore database from backup
        /// </summary>
        public static bool RestoreDatabase(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    // Create a backup of current database before restore
                    string tempBackup = DbFile + ".temp";
                    if (File.Exists(DbFile))
                    {
                        File.Copy(DbFile, tempBackup, true);
                    }

                    try
                    {
                        File.Copy(backupPath, DbFile, true);

                        // Clean up temp backup on success
                        if (File.Exists(tempBackup))
                        {
                            File.Delete(tempBackup);
                        }

                        return true;
                    }
                    catch
                    {
                        // Restore original database on failure
                        if (File.Exists(tempBackup))
                        {
                            File.Copy(tempBackup, DbFile, true);
                            File.Delete(tempBackup);
                        }
                        throw;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
