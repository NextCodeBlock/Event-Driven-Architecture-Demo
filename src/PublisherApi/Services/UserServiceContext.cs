using Architecture.EventDriven.PublisherApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace Architecture.EventDriven.PublisherApi.Services;

public class UserServiceContext : DbContext
{
    public UserServiceContext(DbContextOptions<UserServiceContext> options) : base(options)
    {
    }

    public DbSet<User> User { get; set; }
}