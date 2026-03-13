using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeishuFileServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FolderRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FileRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_UserId",
                table: "FolderRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_UserId",
                table: "FileRecords",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileRecords_Users_UserId",
                table: "FileRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FolderRecords_Users_UserId",
                table: "FolderRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileRecords_Users_UserId",
                table: "FileRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_FolderRecords_Users_UserId",
                table: "FolderRecords");

            migrationBuilder.DropIndex(
                name: "IX_FolderRecords_UserId",
                table: "FolderRecords");

            migrationBuilder.DropIndex(
                name: "IX_FileRecords_UserId",
                table: "FileRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FolderRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FileRecords");
        }
    }
}
