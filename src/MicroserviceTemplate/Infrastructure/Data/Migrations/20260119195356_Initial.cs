using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MicroserviceTemplate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero), "Install all necessary tools and dependencies for the project", new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero), "Done", "Setup Development Environment", new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero), "Go through the technical architecture document and provide feedback", new DateTimeOffset(2026, 1, 17, 0, 0, 0, TimeSpan.Zero), "InProgress", "Review Architecture Documentation", new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero), "Add JWT-based authentication to the API endpoints", new DateTimeOffset(2026, 1, 22, 0, 0, 0, TimeSpan.Zero), "Todo", "Implement Authentication", new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero), "Create comprehensive unit tests for the service layer", new DateTimeOffset(2026, 1, 25, 0, 0, 0, TimeSpan.Zero), "Todo", "Write Unit Tests", new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero), "Deploy the application to staging environment for QA testing", new DateTimeOffset(2026, 1, 29, 0, 0, 0, TimeSpan.Zero), "Todo", "Deploy to Staging", new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.Zero) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedAt",
                table: "Tasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueDate",
                table: "Tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
