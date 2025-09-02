using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_proj.Migrations
{
    /// <inheritdoc />
    public partial class FixUnitOrderIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    DoctorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.DoctorId);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaterialCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialId);
                    table.ForeignKey(
                        name: "FK_Materials_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ExamName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MainDegree = table.Column<int>(type: "int", nullable: false),
                    TotalProblems = table.Column<int>(type: "int", nullable: false),
                    Shuffle = table.Column<bool>(type: "bit", nullable: false),
                    ExamDuration = table.Column<int>(type: "int", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UniversityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CollegeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exams_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "MaterialId");
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    TopicId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.TopicId);
                    table.ForeignKey(
                        name: "FK_Topics_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "MaterialId");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommonQuestionHeader = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalProblems = table.Column<int>(type: "int", nullable: false),
                    MainDegree = table.Column<int>(type: "int", nullable: false),
                    HasCommonHeader = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_Groups_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "TopicId");
                });

            migrationBuilder.CreateTable(
                name: "ExamUnits",
                columns: table => new
                {
                    UnitOrder = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    MainDegree = table.Column<int>(type: "int", nullable: false),
                    TotalProblems = table.Column<int>(type: "int", nullable: false),
                    Shuffle = table.Column<bool>(type: "bit", nullable: false),
                    AllProblems = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamUnits", x => new { x.ExamId, x.GroupId, x.UnitOrder });
                    table.ForeignKey(
                        name: "FK_ExamUnits_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_ExamUnits_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    ProblemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ProblemName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProblemHeader = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    ProblemImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChoicesNumber = table.Column<int>(type: "int", nullable: false),
                    RightAnswer = table.Column<int>(type: "int", nullable: false),
                    Shuffle = table.Column<bool>(type: "bit", nullable: false),
                    MainDegree = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.ProblemId);
                    table.ForeignKey(
                        name: "FK_Problems_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                });

            migrationBuilder.CreateTable(
                name: "ProblemChoices",
                columns: table => new
                {
                    ChoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Choices = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChoiceImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitOrder = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemChoices", x => x.ChoiceId);
                    table.ForeignKey(
                        name: "FK_ProblemChoices_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "ProblemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_MaterialId",
                table: "Exams",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamUnits_GroupId",
                table: "ExamUnits",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_TopicId",
                table: "Groups",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_DoctorId",
                table: "Materials",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemChoices_ProblemId",
                table: "ProblemChoices",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_GroupId",
                table: "Problems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_MaterialId",
                table: "Topics",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamUnits");

            migrationBuilder.DropTable(
                name: "ProblemChoices");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Doctors");
        }
    }
}
