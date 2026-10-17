using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ordering.Infrastructure.Persistence;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <summary>
    /// Defines the RemoveRawPaymentData type.
    /// </summary>
    [DbContext(typeof(OrderContext))]
    [Migration("20261017160000_RemoveRawPaymentData")]
    public partial class RemoveRawPaymentData : Migration
    {
        /// <summary>
        /// Describes the Up operation.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CardName", table: "Orders");
            migrationBuilder.DropColumn(name: "CardNumber", table: "Orders");
            migrationBuilder.DropColumn(name: "Expiration", table: "Orders");
            migrationBuilder.DropColumn(name: "CVV", table: "Orders");
            migrationBuilder.AddColumn<string>(name: "PaymentIntentId", table: "Orders", type: "nvarchar(max)", nullable: true);
        }

        /// <summary>
        /// Describes the Down operation.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PaymentIntentId", table: "Orders");
            migrationBuilder.AddColumn<string>(name: "CardName", table: "Orders", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CardNumber", table: "Orders", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Expiration", table: "Orders", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CVV", table: "Orders", type: "nvarchar(max)", nullable: true);
        }
    }
}
