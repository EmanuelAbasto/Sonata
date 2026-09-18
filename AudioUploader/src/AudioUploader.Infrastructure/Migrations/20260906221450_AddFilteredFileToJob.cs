using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioUploader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredFileToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FilteredFileId",
                table: "jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_FilteredFileId",
                table: "jobs",
                column: "FilteredFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_audio_files_FilteredFileId",
                table: "jobs",
                column: "FilteredFileId",
                principalTable: "audio_files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_audio_files_FilteredFileId",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_jobs_FilteredFileId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "FilteredFileId",
                table: "jobs");
        }
    }
}
