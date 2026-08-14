using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Campus_PUMUB.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationIdToResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    Activity_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activity_Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Image = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Activity__393F5A452A0468C9", x => x.Activity_Id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Category_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Category__6DB38D6EDD2875F1", x => x.Category_Id);
                });

            migrationBuilder.CreateTable(
                name: "Faculty",
                columns: table => new
                {
                    Faculty_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Faculty_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Faculty__4EFCEAAADECD7578", x => x.Faculty_Id);
                });

            migrationBuilder.CreateTable(
                name: "Grade",
                columns: table => new
                {
                    Grade_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grade", x => x.Grade_Id);
                });

            migrationBuilder.CreateTable(
                name: "NewStudentAcc",
                columns: table => new
                {
                    NewStudentAccId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegisterAccId = table.Column<int>(type: "int", nullable: true),
                    Full_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewStudentAcc", x => x.NewStudentAccId);
                });

            migrationBuilder.CreateTable(
                name: "Payment_Fees",
                columns: table => new
                {
                    Fees_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Class_Year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fee_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Montly_Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Active"),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payment___24E61F7BD6034E00", x => x.Fees_Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Permission_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Permission_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Permission_Id);
                });

            migrationBuilder.CreateTable(
                name: "Position",
                columns: table => new
                {
                    Position_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Position_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Position__3C3EAE062191C9DE", x => x.Position_Id);
                });

            migrationBuilder.CreateTable(
                name: "RegisterAcc",
                columns: table => new
                {
                    RegisterAccId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Full_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Form_No = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Exam_Seat_No = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ReviewedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterAcc", x => x.RegisterAccId);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Role_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Role__D80AB4BB2E4B3432", x => x.Role_Id);
                });

            migrationBuilder.CreateTable(
                name: "Rules_Regulations",
                columns: table => new
                {
                    Rule_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Penalty = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rules_Re__70B7089E77C8C2C5", x => x.Rule_Id);
                });

            migrationBuilder.CreateTable(
                name: "Semester",
                columns: table => new
                {
                    Semester_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Semester_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Semester__12459A74A033FE51", x => x.Semester_Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentPersonalInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NewStudentAccId = table.Column<int>(type: "int", nullable: true),
                    AdmissionSerialNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    academic_year_range = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    academic_year_level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    major = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    roll_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    university_reg_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    admission_year = table.Column<int>(type: "int", nullable: true),
                    student_name_mm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    student_name_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mother_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    father_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gender_relation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ethnicity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    religion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    birth_place_region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    student_nrc_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nationality_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    blood_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    covid_vaccine_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    current_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    permanent_address_mm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    permanent_address_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    matric_roll_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    matric_passed_year = table.Column<int>(type: "int", nullable: true),
                    exam_center = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    father_occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mother_occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    past_exam_major = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    past_exam_roll_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    past_exam_year = table.Column<int>(type: "int", nullable: true),
                    past_exam_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    previous_year_roll_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    guardian_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    guardian_relationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    guardian_occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    guardian_address_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_guardian_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_guardian_nrc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_guardian_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_guardian_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_student_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_student_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stipend_requested = table.Column<bool>(type: "bit", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nrc_state = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nrc_township = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nrc_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nrc_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPersonalInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Book",
                columns: table => new
                {
                    Book_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category_Id = table.Column<int>(type: "int", nullable: false),
                    Book_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Image = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Book__C223F3B4A9366735", x => x.Book_Id);
                    table.ForeignKey(
                        name: "FK__Book__Category_I__5DCAEF64",
                        column: x => x.Category_Id,
                        principalTable: "Category",
                        principalColumn: "Category_Id");
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Department_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Faculty_Id = table.Column<int>(type: "int", nullable: false),
                    Department_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Departme__151675F14D7DCED4", x => x.Department_Id);
                    table.ForeignKey(
                        name: "FK__Departmen__Facul__59063A47",
                        column: x => x.Faculty_Id,
                        principalTable: "Faculty",
                        principalColumn: "Faculty_Id");
                });

            migrationBuilder.CreateTable(
                name: "Major",
                columns: table => new
                {
                    Major_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Major_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Faculty_Id = table.Column<int>(type: "int", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Major", x => x.Major_Id);
                    table.ForeignKey(
                        name: "FK_Major_Faculty",
                        column: x => x.Faculty_Id,
                        principalTable: "Faculty",
                        principalColumn: "Faculty_Id");
                });

            migrationBuilder.CreateTable(
                name: "Role_Permissions",
                columns: table => new
                {
                    Role_Permission_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role_Id = table.Column<int>(type: "int", nullable: false),
                    Permission_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role_Permissions", x => x.Role_Permission_Id);
                    table.ForeignKey(
                        name: "FK_Role_Permissions_Permissions_Permission_Id",
                        column: x => x.Permission_Id,
                        principalTable: "Permissions",
                        principalColumn: "Permission_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Role_Permissions_Role_Role_Id",
                        column: x => x.Role_Id,
                        principalTable: "Role",
                        principalColumn: "Role_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleHierarchy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Parent_Role_Id = table.Column<int>(type: "int", nullable: false),
                    Child_Role_Id = table.Column<int>(type: "int", nullable: false),
                    CanAccessAllFaculties = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleHierarchy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleHierarchy_Role_Child_Role_Id",
                        column: x => x.Child_Role_Id,
                        principalTable: "Role",
                        principalColumn: "Role_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleHierarchy_Role_Parent_Role_Id",
                        column: x => x.Parent_Role_Id,
                        principalTable: "Role",
                        principalColumn: "Role_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    User_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role_id = table.Column<int>(type: "int", nullable: false),
                    Full_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Role_No = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Password = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: true),
                    Faculty_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User__206D91702B22E388", x => x.User_Id);
                    table.ForeignKey(
                        name: "FK_User_Faculty",
                        column: x => x.Faculty_Id,
                        principalTable: "Faculty",
                        principalColumn: "Faculty_Id");
                    table.ForeignKey(
                        name: "FK__User__Role_id__68487DD7",
                        column: x => x.Role_id,
                        principalTable: "Role",
                        principalColumn: "Role_Id");
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    Subject_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Semester_Id = table.Column<int>(type: "int", nullable: false),
                    Faculty_Id = table.Column<int>(type: "int", nullable: true),
                    Subject_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subject_Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subject__D98F54B64787BB85", x => x.Subject_Id);
                    table.ForeignKey(
                        name: "FK_Subject_Faculty",
                        column: x => x.Faculty_Id,
                        principalTable: "Faculty",
                        principalColumn: "Faculty_Id");
                    table.ForeignKey(
                        name: "FK__Subject__Semeste__628FA481",
                        column: x => x.Semester_Id,
                        principalTable: "Semester",
                        principalColumn: "Semester_Id");
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    Student_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_Id = table.Column<int>(type: "int", nullable: false),
                    Current_Class_Year = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Current_Major = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Current_Roll_No = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Active"),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Sem1_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem2_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem3_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem4_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem5_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem6_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem7_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem8_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sem9_Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Student_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Enrollment_No = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Faculty_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__A2F4E98C1185513A", x => x.Student_Id);
                    table.ForeignKey(
                        name: "FK_Student_Faculty_Faculty_Id",
                        column: x => x.Faculty_Id,
                        principalTable: "Faculty",
                        principalColumn: "Faculty_Id");
                    table.ForeignKey(
                        name: "FK__Student__User_Id__7C4F7684",
                        column: x => x.User_Id,
                        principalTable: "User",
                        principalColumn: "User_Id");
                });

            migrationBuilder.CreateTable(
                name: "Student_Registrations",
                columns: table => new
                {
                    registration_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    NewStudentAccId = table.Column<int>(type: "int", nullable: true),
                    admission_serial_no = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    academic_year_range = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    academic_year_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    major = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    roll_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    university_reg_no = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    admission_year = table.Column<int>(type: "int", nullable: true),
                    application_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    student_name_mm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    student_name_en = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    mother_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    father_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    gender_relation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ethnicity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    religion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pob = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    birth_place_region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    student_nrc_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nationality_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: false),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    blood_type = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    covid_vaccine_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    current_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    permanent_address_mm = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    permanent_address_en = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    matric_roll_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    matric_passed_year = table.Column<int>(type: "int", nullable: false),
                    exam_center = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    father_occupation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    mother_occupation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    past_exam_major = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    past_exam_roll_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    past_exam_year = table.Column<int>(type: "int", nullable: true),
                    past_exam_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    previous_year_roll_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    guardian_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    guardian_relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    guardian_occupation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    guardian_address_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_guardian_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    app_guardian_nrc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    app_guardian_phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    app_guardian_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    app_student_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    app_student_phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    stipend_requested = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Pending"),
                    created_datetime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    modified_datetime = table.Column<DateTime>(type: "datetime", nullable: true),
                    modified_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_delete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    student_image = table.Column<string>(type: "nvarchar(MAX)", nullable: true),
                    signature_image = table.Column<string>(type: "nvarchar(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student___22A298F6A8464C8B", x => x.registration_id);
                    table.ForeignKey(
                        name: "FK__Student_R__user___76969D2E",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "User_Id");
                });

            migrationBuilder.CreateTable(
                name: "Tutor",
                columns: table => new
                {
                    Tutor_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_Id = table.Column<int>(type: "int", nullable: false),
                    Position_id = table.Column<int>(type: "int", nullable: false),
                    Department_Id = table.Column<int>(type: "int", nullable: false),
                    Tutor_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Profile = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    About = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tutor__95664E0D409B1130", x => x.Tutor_Id);
                    table.ForeignKey(
                        name: "FK__Tutor__Departmen__6EF57B66",
                        column: x => x.Department_Id,
                        principalTable: "Department",
                        principalColumn: "Department_Id");
                    table.ForeignKey(
                        name: "FK__Tutor__Position___6E01572D",
                        column: x => x.Position_id,
                        principalTable: "Position",
                        principalColumn: "Position_Id");
                    table.ForeignKey(
                        name: "FK__Tutor__User_Id__6D0D32F4",
                        column: x => x.User_Id,
                        principalTable: "User",
                        principalColumn: "User_Id");
                });

            migrationBuilder.CreateTable(
                name: "Subject_Prerequisite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject_Id = table.Column<int>(type: "int", nullable: false),
                    Prerequisite_Subject_Id = table.Column<int>(type: "int", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject_Prerequisite", x => x.Id);
                    table.CheckConstraint("CK_No_Self_Prereq", "[Subject_Id] <> [Prerequisite_Subject_Id]");
                    table.ForeignKey(
                        name: "FK_Subject_Prerequisite_Subject_Prerequisite_Subject_Id",
                        column: x => x.Prerequisite_Subject_Id,
                        principalTable: "Subject",
                        principalColumn: "Subject_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subject_Prerequisite_Subject_Subject_Id",
                        column: x => x.Subject_Id,
                        principalTable: "Subject",
                        principalColumn: "Subject_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Student_Subject_Enrollment",
                columns: table => new
                {
                    Enrollment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_Id = table.Column<int>(type: "int", nullable: false),
                    Subject_Id = table.Column<int>(type: "int", nullable: false),
                    Semester_Id = table.Column<int>(type: "int", nullable: false),
                    Enrollment_Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Subject_Enrollment", x => x.Enrollment_Id);
                    table.ForeignKey(
                        name: "FK_Student_Subject_Enrollment_Semester_Semester_Id",
                        column: x => x.Semester_Id,
                        principalTable: "Semester",
                        principalColumn: "Semester_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Student_Subject_Enrollment_Student_Student_Id",
                        column: x => x.Student_Id,
                        principalTable: "Student",
                        principalColumn: "Student_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Student_Subject_Enrollment_Subject_Subject_Id",
                        column: x => x.Subject_Id,
                        principalTable: "Subject",
                        principalColumn: "Subject_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Registration_Payment",
                columns: table => new
                {
                    Payment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Registration_Id = table.Column<int>(type: "int", nullable: false),
                    Amount_Paid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Payment_Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Receipt_Image = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Payment_Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Pending"),
                    VerifyBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Registra__DA6C7FC1C239E7F3", x => x.Payment_Id);
                    table.ForeignKey(
                        name: "FK__Registrat__Regis__02084FDA",
                        column: x => x.Registration_Id,
                        principalTable: "Student_Registrations",
                        principalColumn: "registration_id");
                    table.ForeignKey(
                        name: "FK__Registrat__Verif__02FC7413",
                        column: x => x.VerifyBy,
                        principalTable: "User",
                        principalColumn: "User_Id");
                });

            migrationBuilder.CreateTable(
                name: "Student_Subject_Result",
                columns: table => new
                {
                    Result_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enrollment_Id = table.Column<int>(type: "int", nullable: true),
                    Student_Id = table.Column<int>(type: "int", nullable: true),
                    Subject_Id = table.Column<int>(type: "int", nullable: true),
                    Semester_Id = table.Column<int>(type: "int", nullable: true),
                    Registration_Id = table.Column<int>(type: "int", nullable: true),
                    Marks_Obtained = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Max_Marks = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Is_Pass = table.Column<bool>(type: "bit", nullable: false),
                    Result_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Subject_Result", x => x.Result_Id);
                    table.ForeignKey(
                        name: "FK_Result_Enrollment",
                        column: x => x.Enrollment_Id,
                        principalTable: "Student_Subject_Enrollment",
                        principalColumn: "Enrollment_Id");
                    table.ForeignKey(
                        name: "FK_Student_Subject_Result_Semester_Semester_Id",
                        column: x => x.Semester_Id,
                        principalTable: "Semester",
                        principalColumn: "Semester_Id");
                    table.ForeignKey(
                        name: "FK_Student_Subject_Result_Student_Registrations_Registration_Id",
                        column: x => x.Registration_Id,
                        principalTable: "Student_Registrations",
                        principalColumn: "registration_id");
                    table.ForeignKey(
                        name: "FK_Student_Subject_Result_Student_Student_Id",
                        column: x => x.Student_Id,
                        principalTable: "Student",
                        principalColumn: "Student_Id");
                    table.ForeignKey(
                        name: "FK_Student_Subject_Result_Subject_Subject_Id",
                        column: x => x.Subject_Id,
                        principalTable: "Subject",
                        principalColumn: "Subject_Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Book_Category_Id",
                table: "Book",
                column: "Category_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Faculty_Id",
                table: "Department",
                column: "Faculty_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Major_Faculty_Id",
                table: "Major",
                column: "Faculty_Id");

            migrationBuilder.CreateIndex(
                name: "UQ_NewStudentAcc_Username",
                table: "NewStudentAcc",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registration_Payment_Registration_Id",
                table: "Registration_Payment",
                column: "Registration_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_Payment_VerifyBy",
                table: "Registration_Payment",
                column: "VerifyBy");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permissions_Permission_Id",
                table: "Role_Permissions",
                column: "Permission_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permissions_Role_Id",
                table: "Role_Permissions",
                column: "Role_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoleHierarchy_Child_Role_Id",
                table: "RoleHierarchy",
                column: "Child_Role_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoleHierarchy_Parent_Role_Id",
                table: "RoleHierarchy",
                column: "Parent_Role_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Faculty_Id",
                table: "Student",
                column: "Faculty_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_User_Id",
                table: "Student",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Registrations_user_id",
                table: "Student_Registrations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Enrollment_Semester_Id",
                table: "Student_Subject_Enrollment",
                column: "Semester_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Enrollment_Subject_Id",
                table: "Student_Subject_Enrollment",
                column: "Subject_Id");

            migrationBuilder.CreateIndex(
                name: "UQ_Student_Subject_Semester",
                table: "Student_Subject_Enrollment",
                columns: new[] { "Student_Id", "Subject_Id", "Semester_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Result_Enrollment",
                table: "Student_Subject_Result",
                column: "Enrollment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Result_Enrollment_Id",
                table: "Student_Subject_Result",
                column: "Enrollment_Id",
                unique: true,
                filter: "[Enrollment_Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Result_Registration_Id",
                table: "Student_Subject_Result",
                column: "Registration_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Result_Semester_Id",
                table: "Student_Subject_Result",
                column: "Semester_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Result_Student_Id",
                table: "Student_Subject_Result",
                column: "Student_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Subject_Result_Subject_Id",
                table: "Student_Subject_Result",
                column: "Subject_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Subject_Faculty_Id",
                table: "Subject",
                column: "Faculty_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Subject_Semester_Id",
                table: "Subject",
                column: "Semester_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Subject_Prerequisite_Prerequisite_Subject_Id",
                table: "Subject_Prerequisite",
                column: "Prerequisite_Subject_Id");

            migrationBuilder.CreateIndex(
                name: "UQ_Subject_Prereq",
                table: "Subject_Prerequisite",
                columns: new[] { "Subject_Id", "Prerequisite_Subject_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Department_Id",
                table: "Tutor",
                column: "Department_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Position_id",
                table: "Tutor",
                column: "Position_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_User_Id",
                table: "Tutor",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Faculty_Id",
                table: "User",
                column: "Faculty_Id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Role_id",
                table: "User",
                column: "Role_id");

            migrationBuilder.CreateIndex(
                name: "UQ__User__C9F28456D3FE7075",
                table: "User",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropTable(
                name: "Book");

            migrationBuilder.DropTable(
                name: "Grade");

            migrationBuilder.DropTable(
                name: "Major");

            migrationBuilder.DropTable(
                name: "NewStudentAcc");

            migrationBuilder.DropTable(
                name: "Payment_Fees");

            migrationBuilder.DropTable(
                name: "RegisterAcc");

            migrationBuilder.DropTable(
                name: "Registration_Payment");

            migrationBuilder.DropTable(
                name: "Role_Permissions");

            migrationBuilder.DropTable(
                name: "RoleHierarchy");

            migrationBuilder.DropTable(
                name: "Rules_Regulations");

            migrationBuilder.DropTable(
                name: "Student_Subject_Result");

            migrationBuilder.DropTable(
                name: "StudentPersonalInfo");

            migrationBuilder.DropTable(
                name: "Subject_Prerequisite");

            migrationBuilder.DropTable(
                name: "Tutor");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Student_Subject_Enrollment");

            migrationBuilder.DropTable(
                name: "Student_Registrations");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "Position");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Semester");

            migrationBuilder.DropTable(
                name: "Faculty");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
