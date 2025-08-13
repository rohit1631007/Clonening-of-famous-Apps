using Microsoft.EntityFrameworkCore;
using Payment_method.Models;

namespace Payment_method.Data;

public class Payment_methodDb : DbContext
{
    public Payment_methodDb(DbContextOptions<Payment_methodDb> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
}
