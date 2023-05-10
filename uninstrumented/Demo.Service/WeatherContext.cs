using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System;
namespace Demo.Service;

public class WeatherContext : DbContext
{
    public WeatherContext(DbContextOptions<WeatherContext> context) : base(context) { }

    public DbSet<WeatherServiceRequest> WeatherServiceRequests { get; set; } = null!;
}

public class WeatherServiceRequest
{
    
    [Key]
    public Guid Id { get; set; }
    public string Note { get; set; } = string.Empty;
}
