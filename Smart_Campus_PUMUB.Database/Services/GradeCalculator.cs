using System;
using System.Collections.Generic;
using System.Linq;

namespace Smart_Campus_PUMUB.Database.Services
{
    public class GradeTierInfo
    {
        public string LetterGrade { get; set; } = string.Empty;
        public decimal GradePoint { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal MinMark { get; set; }
        public decimal MaxMark { get; set; }
        public string DescriptionMm { get; set; } = string.Empty;
    }

    public static class GradeCalculator
    {
        public static readonly List<GradeTierInfo> DefaultGradeTiers = new()
        {
            new GradeTierInfo { LetterGrade = "A+", GradePoint = 4.00m, Status = "Excellent",    MinMark = 90.0m, MaxMark = 100.0m,  DescriptionMm = "ထူးချွန်" },
            new GradeTierInfo { LetterGrade = "A",  GradePoint = 4.00m, Status = "Very Good",    MinMark = 80.0m, MaxMark = 89.99m,  DescriptionMm = "အလွန်ကောင်း" },
            new GradeTierInfo { LetterGrade = "A-", GradePoint = 3.67m, Status = "Very Good",    MinMark = 75.0m, MaxMark = 79.99m,  DescriptionMm = "အလွန်ကောင်း" },
            new GradeTierInfo { LetterGrade = "B+", GradePoint = 3.33m, Status = "Good",         MinMark = 70.0m, MaxMark = 74.99m,  DescriptionMm = "ကောင်း" },
            new GradeTierInfo { LetterGrade = "B",  GradePoint = 3.00m, Status = "Good",         MinMark = 65.0m, MaxMark = 69.99m,  DescriptionMm = "ကောင်း" },
            new GradeTierInfo { LetterGrade = "B-", GradePoint = 2.67m, Status = "Good",         MinMark = 60.0m, MaxMark = 64.99m,  DescriptionMm = "ကောင်း" },
            new GradeTierInfo { LetterGrade = "C+", GradePoint = 2.33m, Status = "Satisfactory", MinMark = 55.0m, MaxMark = 59.99m,  DescriptionMm = "အသင့်အတင့်" },
            new GradeTierInfo { LetterGrade = "C",  GradePoint = 2.00m, Status = "Satisfactory", MinMark = 50.0m, MaxMark = 54.99m,  DescriptionMm = "အသင့်အတင့်" },
            new GradeTierInfo { LetterGrade = "D",  GradePoint = 1.00m, Status = "Marginal",     MinMark = 40.0m, MaxMark = 49.99m,  DescriptionMm = "အနားသတ်/အားနည်း" },
            new GradeTierInfo { LetterGrade = "F",  GradePoint = 0.00m, Status = "Poor",         MinMark = 0.0m,  MaxMark = 39.99m,  DescriptionMm = "ကျရှုံး" }
        };

        /// <summary>
        /// Maps numerical marks (0-100) to the corresponding Grade tier.
        /// </summary>
        public static GradeTierInfo GetGradeInfoFromMarks(decimal marks)
        {
            if (marks < 0) marks = 0;
            if (marks > 100) marks = 100;

            foreach (var tier in DefaultGradeTiers)
            {
                if (marks >= tier.MinMark)
                {
                    return tier;
                }
            }

            return DefaultGradeTiers.Last();
        }

        /// <summary>
        /// Gets the Grade Point (Score) for a given Letter Grade (e.g. "B+" -> 3.33).
        /// </summary>
        public static decimal GetGradePoint(string? gradeLetter)
        {
            if (string.IsNullOrWhiteSpace(gradeLetter)) return 0.0m;
            var clean = gradeLetter.Trim().ToUpperInvariant();
            var match = DefaultGradeTiers.FirstOrDefault(t => t.LetterGrade.Equals(clean, StringComparison.OrdinalIgnoreCase));
            return match?.GradePoint ?? 0.0m;
        }

        /// <summary>
        /// Gets the Status for a given Letter Grade (e.g. "B+" -> "Good").
        /// </summary>
        public static string GetGradeStatus(string? gradeLetter)
        {
            if (string.IsNullOrWhiteSpace(gradeLetter)) return "-";
            var clean = gradeLetter.Trim().ToUpperInvariant();
            var match = DefaultGradeTiers.FirstOrDefault(t => t.LetterGrade.Equals(clean, StringComparison.OrdinalIgnoreCase));
            return match?.Status ?? "-";
        }

        /// <summary>
        /// Grade Point Earned for a subject = Grade Point (Score) * Credit Unit
        /// </summary>
        public static decimal CalculateGradePointsEarned(decimal gradePoint, int creditUnit)
        {
            return Math.Round(gradePoint * creditUnit, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates Semester GPA = Total Grade Points Earned in Semester / Total Credit Units Earned in Semester
        /// </summary>
        public static decimal CalculateSemesterGPA(decimal totalGradePointsEarned, int totalCredits)
        {
            if (totalCredits <= 0) return 0.0m;
            return Math.Round(totalGradePointsEarned / totalCredits, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates Cumulative GPA (CGPA) by averaging Semester GPAs:
        /// CGPA = (Semester 1 GPA + Semester 2 GPA + ... + Semester N GPA) / N
        /// </summary>
        public static decimal CalculateCumulativeGPAFromSemesterGpas(IEnumerable<decimal> semesterGpas)
        {
            if (semesterGpas == null) return 0.0m;
            var list = semesterGpas.Where(g => g > 0).ToList();
            if (!list.Any()) return 0.0m;
            return Math.Round(list.Average(), 2, MidpointRounding.AwayFromZero);
        }

        public static decimal CalculateCumulativeGPA(IEnumerable<decimal> semesterGpas)
        {
            return CalculateCumulativeGPAFromSemesterGpas(semesterGpas);
        }

        public static decimal CalculateCumulativeGPA(decimal sumAllGradePointsEarned, int sumAllCredits)
        {
            if (sumAllCredits <= 0) return 0.0m;
            return Math.Round(sumAllGradePointsEarned / sumAllCredits, 2, MidpointRounding.AwayFromZero);
        }
    }
}
