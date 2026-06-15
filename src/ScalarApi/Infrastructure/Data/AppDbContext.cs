using Microsoft.EntityFrameworkCore;
using ScalarApi.Domain.Entities;

namespace ScalarApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Forecast> Forecasts => Set<Forecast>();
}
