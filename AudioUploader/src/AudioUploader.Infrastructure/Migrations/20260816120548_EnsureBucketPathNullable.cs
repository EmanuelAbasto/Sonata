using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioUploader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureBucketPathNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BucketPath",
                table: "audio_files",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BucketPath",
                table: "audio_files",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}