using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerPayFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyRate",
                table: "WorkerRoles",
                newName: "PayRate");

            migrationBuilder.AddColumn<int>(
                name: "PayFrequency",
                table: "WorkerRoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayFrequency",
                table: "WorkerRoles");

            migrationBuilder.RenameColumn(
                name: "PayRate",
                table: "WorkerRoles",
                newName: "DailyRate");
        }
    }
}
