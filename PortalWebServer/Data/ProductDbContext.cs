using Microsoft.EntityFrameworkCore;
using PortalWebServer.Entity;
using System.Collections.Generic;

namespace PortalWebServer.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}
