using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioUploader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchFieldsToJobMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "job_metrics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchSize",
                table: "job_metrics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchType",
                table: "job_metrics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "job_metrics");

            migrationBuilder.DropColumn(
                name: "BatchSize",
                table: "job_metrics");

            migrationBuilder.DropColumn(
                name: "BatchType",
                table: "job_metrics");
        }
    }
}
