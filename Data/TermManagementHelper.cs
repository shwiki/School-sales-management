using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gusheshe.Data
{
    public static class TermManagementHelper
    {
        /// <summary>
        /// Creates a new academic term
        /// </summary>
        public static bool CreateTerm(string termName, DateTime startDate, DateTime endDate, bool isActive = false)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    // If this term should be active, deactivate all other terms
                    if (isActive)
                    {
                        using (var deactivateCmd = new SQLiteCommand("UPDATE AcademicTerms SET IsActive = 0", conn, transaction))
                        {
                            deactivateCmd.ExecuteNonQuery();
                        }
                    }

                    // Create the new term
                    using (var cmd = new SQLiteCommand(@"
                        INSERT INTO AcademicTerms (TermName, StartDate, EndDate, IsActive, CreatedDate)
                        VALUES (@termName, @startDate, @endDate, @isActive, @createdDate)", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@termName", termName);
                        cmd.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating term: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Sets up fee structure for all students for a specific term
        /// </summary>
        public static bool SetupTermFeesForAllStudents(int termId, double defaultFeeAmount)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    // Get all active students
                    using (var studentsCmd = new SQLiteCommand("SELECT StudentId FROM Students", conn, transaction))
                    using (var reader = studentsCmd.ExecuteReader())
                    {
                        var studentIds = new List<int>();
                        while (reader.Read())
                        {
                            studentIds.Add(Convert.ToInt32(reader["StudentId"]));
                        }

                        reader.Close();

                        // Create term fee records for each student
                        foreach (int studentId in studentIds)
                        {
                            using (var feeCmd = new SQLiteCommand(@"
                                INSERT OR IGNORE INTO StudentTermFees (StudentId, TermId, ExpectedFees, CreatedDate)
                                VALUES (@studentId, @termId, @expectedFees, @createdDate)", conn, transaction))
                            {
                                feeCmd.Parameters.AddWithValue("@studentId", studentId);
                                feeCmd.Parameters.AddWithValue("@termId", termId);
                                feeCmd.Parameters.AddWithValue("@expectedFees", defaultFeeAmount);
                                feeCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                feeCmd.ExecuteNonQuery();
                            }

                            // Create corresponding TermFeeAccounts record
                            using (var accountCmd = new SQLiteCommand(@"
                                INSERT OR IGNORE INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, CreatedDate, UpdatedDate)
                                VALUES (@studentId, @termId, @expectedFees, 0, @expectedFees, @createdDate, @updatedDate)", conn, transaction))
                            {
                                accountCmd.Parameters.AddWithValue("@studentId", studentId);
                                accountCmd.Parameters.AddWithValue("@termId", termId);
                                accountCmd.Parameters.AddWithValue("@expectedFees", defaultFeeAmount);
                                accountCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                accountCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                accountCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up term fees: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Sets up fee structure for a specific student for a specific term
        /// </summary>
        public static bool SetupStudentTermFee(int studentId, int termId, double feeAmount)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    // Insert/Update StudentTermFees
                    using (var feeCmd = new SQLiteCommand(@"
                        INSERT OR REPLACE INTO StudentTermFees (StudentId, TermId, ExpectedFees, CreatedDate)
                        VALUES (@studentId, @termId, @expectedFees, @createdDate)", conn, transaction))
                    {
                        feeCmd.Parameters.AddWithValue("@studentId", studentId);
                        feeCmd.Parameters.AddWithValue("@termId", termId);
                        feeCmd.Parameters.AddWithValue("@expectedFees", feeAmount);
                        feeCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        feeCmd.ExecuteNonQuery();
                    }

                    // Insert/Update TermFeeAccounts
                    using (var checkCmd = new SQLiteCommand("SELECT TotalPaid FROM TermFeeAccounts WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@studentId", studentId);
                        checkCmd.Parameters.AddWithValue("@termId", termId);
                        var existingPaid = checkCmd.ExecuteScalar();

                        double totalPaid = existingPaid != null ? Convert.ToDouble(existingPaid) : 0;

                        using (var accountCmd = new SQLiteCommand(@"
                            INSERT OR REPLACE INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, CreatedDate, UpdatedDate)
                            VALUES (@studentId, @termId, @expectedFees, @totalPaid, @balance, @createdDate, @updatedDate)", conn, transaction))
                        {
                            accountCmd.Parameters.AddWithValue("@studentId", studentId);
                            accountCmd.Parameters.AddWithValue("@termId", termId);
                            accountCmd.Parameters.AddWithValue("@expectedFees", feeAmount);
                            accountCmd.Parameters.AddWithValue("@totalPaid", totalPaid);
                            accountCmd.Parameters.AddWithValue("@balance", feeAmount - totalPaid);
                            accountCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            accountCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            accountCmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up student term fee: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets all terms with their basic information
        /// </summary>
        public static List<TermInfo> GetAllTerms()
        {
            var terms = new List<TermInfo>();
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand("SELECT TermId, TermName, StartDate, EndDate, IsActive FROM AcademicTerms ORDER BY StartDate", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        terms.Add(new TermInfo
                        {
                            TermId = Convert.ToInt32(reader["TermId"]),
                            TermName = reader["TermName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting terms: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return terms;
        }

        /// <summary>
        /// Sets a term as the active term (deactivates all others)
        /// </summary>
        public static bool SetActiveTerm(int termId)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    // Deactivate all terms
                    using (var deactivateCmd = new SQLiteCommand("UPDATE AcademicTerms SET IsActive = 0", conn, transaction))
                    {
                        deactivateCmd.ExecuteNonQuery();
                    }

                    // Activate the specified term
                    using (var activateCmd = new SQLiteCommand("UPDATE AcademicTerms SET IsActive = 1 WHERE TermId = @termId", conn, transaction))
                    {
                        activateCmd.Parameters.AddWithValue("@termId", termId);
                        int rowsAffected = activateCmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting active term: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets students who owe money across all terms
        /// </summary>
        public static List<StudentDebtInfo> GetStudentsWithOutstandingFees()
        {
            var debtors = new List<StudentDebtInfo>();
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        s.StudentId,
                        s.FirstName || ' ' || s.LastName as StudentName,
                        s.Class,
                        SUM(COALESCE(tfa.Balance, 0)) as TotalBalance,
                        COUNT(tfa.TermId) as TermsWithFees,
                        MAX(tfa.LastPaymentDate) as LastPaymentDate
                    FROM Students s
                    LEFT JOIN TermFeeAccounts tfa ON s.StudentId = tfa.StudentId
                    WHERE COALESCE(tfa.Balance, 0) > 0
                    GROUP BY s.StudentId, s.FirstName, s.LastName, s.Class
                    HAVING SUM(COALESCE(tfa.Balance, 0)) > 0
                    ORDER BY TotalBalance DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        debtors.Add(new StudentDebtInfo
                        {
                            StudentId = Convert.ToInt32(reader["StudentId"]),
                            StudentName = reader["StudentName"].ToString(),
                            Class = reader["Class"].ToString(),
                            TotalBalance = Convert.ToDouble(reader["TotalBalance"]),
                            TermsWithFees = Convert.ToInt32(reader["TermsWithFees"]),
                            LastPaymentDate = reader["LastPaymentDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["LastPaymentDate"])
                                : (DateTime?)null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting students with outstanding fees: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return debtors;
        }

        /// <summary>
        /// Handles overpayment by distributing excess to future terms
        /// </summary>
        public static bool HandleOverpayment(int studentId, double overpaymentAmount)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    // Get future terms that this student doesn't have accounts for yet
                    using (var futureTermsCmd = new SQLiteCommand(@"
                        SELECT at.TermId, at.TermName 
                        FROM AcademicTerms at
                        LEFT JOIN TermFeeAccounts tfa ON at.TermId = tfa.TermId AND tfa.StudentId = @studentId
                        WHERE tfa.AccountId IS NULL
                        AND at.StartDate > DATE('now')
                        ORDER BY at.StartDate
                        LIMIT 1", conn, transaction))
                    {
                        futureTermsCmd.Parameters.AddWithValue("@studentId", studentId);
                        using (var reader = futureTermsCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int futureTermId = Convert.ToInt32(reader["TermId"]);
                                reader.Close();

                                // Get the student's standard fee amount
                                double standardFee = 0;
                                using (var feeCmd = new SQLiteCommand("SELECT ExpectedFees FROM Students WHERE StudentId = @studentId", conn, transaction))
                                {
                                    feeCmd.Parameters.AddWithValue("@studentId", studentId);
                                    var feeResult = feeCmd.ExecuteScalar();
                                    standardFee = feeResult != null ? Convert.ToDouble(feeResult) : 0;
                                }

                                // Create advance payment record for future term
                                using (var advanceCmd = new SQLiteCommand(@"
                                    INSERT INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, CreatedDate, UpdatedDate)
                                    VALUES (@studentId, @termId, @expectedFees, @totalPaid, @balance, @createdDate, @updatedDate)", conn, transaction))
                                {
                                    advanceCmd.Parameters.AddWithValue("@studentId", studentId);
                                    advanceCmd.Parameters.AddWithValue("@termId", futureTermId);
                                    advanceCmd.Parameters.AddWithValue("@expectedFees", standardFee);
                                    advanceCmd.Parameters.AddWithValue("@totalPaid", overpaymentAmount);
                                    advanceCmd.Parameters.AddWithValue("@balance", standardFee - overpaymentAmount);
                                    advanceCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    advanceCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    advanceCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling overpayment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets comprehensive payment history for a student across all terms
        /// </summary>
        public static List<StudentPaymentHistory> GetStudentPaymentHistory(int studentId)
        {
            var history = new List<StudentPaymentHistory>();
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        fp.PaymentId,
                        fp.AmountPaid,
                        fp.DatePaid,
                        fp.Description,
                        COALESCE(at.TermName, 'Legacy Payment') as TermName,
                        s.FirstName || ' ' || s.LastName as StudentName
                    FROM FeePayments fp
                    JOIN Students s ON fp.StudentId = s.StudentId
                    LEFT JOIN AcademicTerms at ON fp.TermId = at.TermId
                    WHERE fp.StudentId = @studentId
                    ORDER BY fp.DatePaid DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new StudentPaymentHistory
                            {
                                PaymentId = Convert.ToInt32(reader["PaymentId"]),
                                AmountPaid = Convert.ToDouble(reader["AmountPaid"]),
                                DatePaid = Convert.ToDateTime(reader["DatePaid"]),
                                Description = reader["Description"].ToString(),
                                TermName = reader["TermName"].ToString(),
                                StudentName = reader["StudentName"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting student payment history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return history;
        }

        /// <summary>
        /// Gets term-by-term fee breakdown for a specific student
        /// </summary>
        public static List<StudentTermBreakdown> GetStudentTermBreakdown(int studentId)
        {
            var breakdown = new List<StudentTermBreakdown>();
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        at.TermId,
                        at.TermName,
                        at.StartDate,
                        at.EndDate,
                        at.IsActive,
                        COALESCE(tfa.ExpectedFees, 0) as ExpectedFees,
                        COALESCE(tfa.TotalPaid, 0) as TotalPaid,
                        COALESCE(tfa.Balance, tfa.ExpectedFees, 0) as Balance,
                        tfa.LastPaymentDate,
                        COUNT(fp.PaymentId) as PaymentCount
                    FROM AcademicTerms at
                    LEFT JOIN TermFeeAccounts tfa ON at.TermId = tfa.TermId AND tfa.StudentId = @studentId
                    LEFT JOIN FeePayments fp ON fp.TermId = at.TermId AND fp.StudentId = @studentId
                    GROUP BY at.TermId, at.TermName, at.StartDate, at.EndDate, at.IsActive, 
                             tfa.ExpectedFees, tfa.TotalPaid, tfa.Balance, tfa.LastPaymentDate
                    ORDER BY at.StartDate", conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            breakdown.Add(new StudentTermBreakdown
                            {
                                TermId = Convert.ToInt32(reader["TermId"]),
                                TermName = reader["TermName"].ToString(),
                                StartDate = Convert.ToDateTime(reader["StartDate"]),
                                EndDate = Convert.ToDateTime(reader["EndDate"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                ExpectedFees = Convert.ToDouble(reader["ExpectedFees"]),
                                TotalPaid = Convert.ToDouble(reader["TotalPaid"]),
                                Balance = Convert.ToDouble(reader["Balance"]),
                                LastPaymentDate = reader["LastPaymentDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["LastPaymentDate"])
                                    : (DateTime?)null,
                                PaymentCount = Convert.ToInt32(reader["PaymentCount"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting student term breakdown: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return breakdown;
        }
    }

    // Supporting Classes
    public class TermInfo
    {
        public int TermId { get; set; }
        public string TermName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentDebtInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Class { get; set; }
        public double TotalBalance { get; set; }
        public int TermsWithFees { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    public class StudentPaymentHistory
    {
        public int PaymentId { get; set; }
        public double AmountPaid { get; set; }
        public DateTime DatePaid { get; set; }
        public string Description { get; set; }
        public string TermName { get; set; }
        public string StudentName { get; set; }
    }

    public class StudentTermBreakdown
    {
        public int TermId { get; set; }
        public string TermName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public double ExpectedFees { get; set; }
        public double TotalPaid { get; set; }
        public double Balance { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int PaymentCount { get; set; }
    }

    public class PaymentDistribution
    {
        public int TermId { get; set; }
        public string TermName { get; set; }
        public double Amount { get; set; }
    }
}
