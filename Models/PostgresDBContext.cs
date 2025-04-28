using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopScoutWebApplication.Models.DBContext
{
    /// <summary>
    /// Версия товара для БД
    /// </summary>
    public class ProductDB : Product
    {
        /// <summary>
        /// Первичный ключ
        /// </summary>
        [Key]
        public int Id { get; set; }
        [Required]
        public Request Request { get; set; }
        [ForeignKey(nameof(Request))]
        public int RequestId { get; set; }
        private ProductDB()
        {
        }
        /// <summary>
        /// Конструктор, копирующий поля товара
        /// </summary>
        /// <param name="product">товар</param>
        public ProductDB(Product product, Request request)
        {
            foreach (var prop in product.GetType().GetProperties())
            {
                prop.SetValue(this, prop.GetValue(product));
            }

            Request = request;
        }
    }
    public class Request
    {
        /// <summary>
        /// Первичный ключ
        /// </summary>
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Created { get; set; }
        public string SearchText { get; set; } = "";
        public MarketName Market { get; set; }
        public Sort Sort { get; set; }
        public List<ProductDB> Products { get; set; } = new List<ProductDB>();
    }
    /// <summary>
    /// Контекст БД
    /// </summary>
    public class PostgresDBContext : DbContext
    {
        private string connectionString;
        public DbSet<ProductDB> Products { get; set; } = null!;
        public DbSet<Request> Requests { get; set; } = null!;

        public PostgresDBContext(string connectionString)
        {
            this.connectionString = connectionString;
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}
