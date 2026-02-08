using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentEcosystem.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppResearchRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    AnalyzedResult = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    Sources = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppResearchRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppResearchRecords_CompletedAt",
                table: "AppResearchRecords",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppResearchRecords_Query",
                table: "AppResearchRecords",
                column: "Query");

            migrationBuilder.CreateIndex(
                name: "IX_AppResearchRecords_Status",
                table: "AppResearchRecords",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppResearchRecords");
        }
    }
}
