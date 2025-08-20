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

                // <-- old-style using block
                using (SQLiteConnection conn = new SQLiteConnection(ConnString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = @"
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
                            CREATE TABLE IF NOT EXISTS FeePayments (
                              PaymentId  INTEGER PRIMARY KEY AUTOINCREMENT,
                              StudentId  INTEGER,
                              AmountPaid REAL,
                              DatePaid   TEXT,
                              IsSynced   INTEGER DEFAULT 0,
                              FOREIGN KEY (StudentId) REFERENCES Students(StudentId)    
                            
                            );
                            CREATE TABLE IF NOT EXISTS FeeAccounts (
                                StudentId        INTEGER PRIMARY KEY,
                                TotalPaid        REAL    NOT NULL DEFAULT 0,
                                LastPaymentDate  TEXT,
                                Arrears          REAL    NOT NULL DEFAULT 0,
                                FOREIGN KEY (StudentId) REFERENCES Students(StudentId)
                            );
                            CREATE TABLE IF NOT EXISTS UniformItems (
                              ItemId   INTEGER PRIMARY KEY AUTOINCREMENT,
                              ItemName TEXT,
                              Price    REAL,
                              Quantity INTEGER
                            );
                            CREATE TABLE IF NOT EXISTS UniformSales (
                              SaleId      INTEGER PRIMARY KEY AUTOINCREMENT,
                              ItemName    TEXT,
                              Quantity    INTEGER,
                              UnitPrice   REAL,
                              Total       REAL,
                              FirstName  TEXT,
                              LastName TEXT,
                              Class     TEXT, 
                              DateSold    TEXT
                            );
                            CREATE TABLE IF NOT EXISTS Payments (
                            PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                            StudentId INTEGER,
                            Amount REAL NOT NULL,
                            PaymentType TEXT NOT NULL,
                            PaymentDate TEXT NOT NULL,
                            Description TEXT,
                            FOREIGN KEY (StudentId) REFERENCES Students (StudentId)
                            );";
                        cmd.ExecuteNonQuery();
                    }
                    }
                }
     
        }

        public static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(ConnString);
            conn.Open();
            return conn;
        }
    }
}
