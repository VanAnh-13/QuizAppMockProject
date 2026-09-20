using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizapp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NotificationStatus = table.Column<int>(type: "int", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "int", nullable: false),
                    NotificationErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmationErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                    table.CheckConstraint("CK_ContactMessages_ConfirmationStatus", "[ConfirmationStatus] IN (0, 1, 2)");
                    table.CheckConstraint("CK_ContactMessages_NotificationStatus", "[NotificationStatus] IN (0, 1, 2)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_ReceivedAt",
                table: "ContactMessages",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");
        }
    }
}
