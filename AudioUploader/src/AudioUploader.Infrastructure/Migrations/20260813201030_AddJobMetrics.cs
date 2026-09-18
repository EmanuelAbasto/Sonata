using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AudioUploader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_metrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    RequestStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RequestEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ProcessingStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UploadStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompressionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompressionEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TranscriptionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TranscriptionEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SummaryStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SummaryEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_metrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_metrics_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_metrics_JobId",
                table: "job_metrics",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_metrics");
        }
    }
}
