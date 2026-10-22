using Microsoft.EntityFrameworkCore;
using CuaHangGiaDung.Models;

namespace CuaHangGiaDung.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ShopSetting> Settings => Set<ShopSetting>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductBrand> ProductBrands => Set<ProductBrand>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
