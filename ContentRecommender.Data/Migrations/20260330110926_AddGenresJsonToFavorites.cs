using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentRecommender.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenresJsonToFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GenresJson",
                table: "Favorites",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenresJson",
                table: "Favorites");
        }
    }
}
