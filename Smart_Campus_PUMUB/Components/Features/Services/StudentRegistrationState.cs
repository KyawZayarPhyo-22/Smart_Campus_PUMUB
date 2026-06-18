using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services
{
    /// <summary>
    /// State service for sharing student registration data between pages
    /// Without calling API repeatedly
    /// </summary>
    public class StudentRegistrationState
    {
        // Personal Information
        public string StudentName { get; set; } = string.Empty;
        public string StudentNameEn { get; set; } = string.Empty;
        public string NrcNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Academic Information
        public string RollNo { get; set; } = string.Empty;
        public string PreviousRollNo { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string AcademicYearRange { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        
        // Address Information
        public string PermanentAddress { get; set; } = string.Empty;
        public string CurrentAddress { get; set; } = string.Empty;
        
        // Guardian Information
        public string FatherName { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        
        // Registration ID
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        
        // Timestamps
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        
        // Methods to set and clear data
        public void SetPersonalInfo(
            string studentName, 
            string studentNameEn, 
            string nrcNumber,
            DateTime dob,
            string gender,
            string phone,
            string email)
        {
            StudentName = studentName;
            StudentNameEn = studentNameEn;
            NrcNumber = nrcNumber;
            DateOfBirth = dob;
            Gender = gender;
            PhoneNumber = phone;
            Email = email;
        }
        
        public void SetAcademicInfo(
            string rollNo,
            string previousRollNo,
            string academicYear,
            string academicYearRange,
            string major,
            string semester)
        {
            RollNo = rollNo;
            PreviousRollNo = previousRollNo;
            AcademicYear = academicYear;
            AcademicYearRange = academicYearRange;
            Major = major;
            Semester = semester;
        }
        
        public void SetAddressInfo(
            string permanentAddress,
            string currentAddress)
        {
            PermanentAddress = permanentAddress;
            CurrentAddress = currentAddress;
        }
        
        public void SetGuardianInfo(
            string fatherName,
            string motherName,
            string guardianName,
            string guardianPhone)
        {
            FatherName = fatherName;
            MotherName = motherName;
            GuardianName = guardianName;
            GuardianPhone = guardianPhone;
        }
        
        public void SetRegistrationIds(int registrationId, int userId)
        {
            RegistrationId = registrationId;
            UserId = userId;
        }
        
        public void SetFromRegistrationModel(StudentRegistrationCreateRequestModel model)
        {
            // Personal Info
            StudentName = model.student_name_mm ?? string.Empty;
            StudentNameEn = model.student_name_en ?? string.Empty;
            NrcNumber = model.student_nrc_no ?? string.Empty;
            DateOfBirth = model.dob;
            Gender = model.gender_relation ?? string.Empty;
            PhoneNumber = model.app_student_phone ?? string.Empty;
            Email = model.email ?? string.Empty;
            
            // Academic Info
            RollNo = model.roll_no ?? string.Empty;
            PreviousRollNo = model.previous_year_roll_no ?? string.Empty;
            AcademicYear = model.academic_year_level ?? string.Empty;
            AcademicYearRange = model.academic_year_range ?? string.Empty;
            Major = model.major ?? string.Empty;
            Semester = ""; // No semester property in model
            
            // Address Info
            PermanentAddress = model.permanent_address_mm ?? string.Empty;
            CurrentAddress = model.current_address ?? string.Empty;
            
            // Guardian Info
            FatherName = model.father_name ?? string.Empty;
            MotherName = model.mother_name ?? string.Empty;
            GuardianName = model.app_guardian_name ?? string.Empty;
            GuardianPhone = model.app_guardian_phone ?? string.Empty;
            
            // IDs
            UserId = model.UserId ?? 0;
        }
        
        public void Clear()
        {
            StudentName = string.Empty;
            StudentNameEn = string.Empty;
            NrcNumber = string.Empty;
            DateOfBirth = default;
            Gender = string.Empty;
            PhoneNumber = string.Empty;
            Email = string.Empty;
            RollNo = string.Empty;
            PreviousRollNo = string.Empty;
            AcademicYear = string.Empty;
            AcademicYearRange = string.Empty;
            Major = string.Empty;
            Semester = string.Empty;
            PermanentAddress = string.Empty;
            CurrentAddress = string.Empty;
            FatherName = string.Empty;
            MotherName = string.Empty;
            GuardianName = string.Empty;
            GuardianPhone = string.Empty;
            RegistrationId = 0;
            UserId = 0;
            RegistrationDate = DateTime.Now;
        }
        
        public bool HasData => !string.IsNullOrEmpty(StudentName) && RegistrationId > 0;
        
        public string GetSummary()
        {
            return $"{StudentName} | {RollNo} | {AcademicYear} | {Major}";
        }
    }
}