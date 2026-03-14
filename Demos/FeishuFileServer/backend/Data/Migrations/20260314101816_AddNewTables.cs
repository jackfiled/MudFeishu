using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeishuFileServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VersionRecords_IsCurrentVersion",
                table: "VersionRecords");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_FolderRecords_IsDeleted",
                table: "FolderRecords");

            migrationBuilder.DropIndex(
                name: "IX_FileRecords_IsDeleted",
                table: "FileRecords");

            migrationBuilder.AddColumn<string>(
                name: "FeishuVersionToken",
                table: "VersionRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedTime",
                table: "FolderRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedTime",
                table: "FileRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChunkUploadRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UploadId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileMD5 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ChunkSize = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalChunks = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedChunks = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedChunkNumbers = table.Column<string>(type: "TEXT", nullable: true),
                    FolderToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkUploadRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChunkUploadRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilePermissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    PermissionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CanRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanWrite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanShare = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManage = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilePermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OperationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpireTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevokedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShareCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxAccessCount = table.Column<int>(type: "INTEGER", nullable: true),
                    AccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareRecords_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChunkUploadRecords_IsCompleted",
                table: "ChunkUploadRecords",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkUploadRecords_UploadId",
                table: "ChunkUploadRecords",
                column: "UploadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChunkUploadRecords_UserId",
                table: "ChunkUploadRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_ResourceToken_UserId",
                table: "FilePermissions",
                columns: new[] { "ResourceToken", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_ResourceType",
                table: "FilePermissions",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_UserId",
                table: "FilePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_OperationTime",
                table: "OperationLogs",
                column: "OperationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_OperationType",
                table: "OperationLogs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_ResourceToken",
                table: "OperationLogs",
                column: "ResourceToken");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_UserId",
                table: "OperationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpireTime",
                table: "RefreshTokens",
                column: "ExpireTime");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareRecords_CreatorId",
                table: "ShareRecords",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareRecords_IsActive",
                table: "ShareRecords",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ShareRecords_ResourceToken",
                table: "ShareRecords",
                column: "ResourceToken");

            migrationBuilder.CreateIndex(
                name: "IX_ShareRecords_ShareCode",
                table: "ShareRecords",
                column: "ShareCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChunkUploadRecords");

            migrationBuilder.DropTable(
                name: "FilePermissions");

            migrationBuilder.DropTable(
                name: "OperationLogs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ShareRecords");

            migrationBuilder.DropColumn(
                name: "FeishuVersionToken",
                table: "VersionRecords");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "FolderRecords");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "FileRecords");

            migrationBuilder.CreateIndex(
                name: "IX_VersionRecords_IsCurrentVersion",
                table: "VersionRecords",
                column: "IsCurrentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_IsDeleted",
                table: "FolderRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_IsDeleted",
                table: "FileRecords",
                column: "IsDeleted");
        }
    }
}
