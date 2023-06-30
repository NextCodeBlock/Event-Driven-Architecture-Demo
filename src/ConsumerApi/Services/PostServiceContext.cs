using Architecture.EventDriven.ConsumerApi.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Architecture.EventDriven.ConsumerApi.Services;

public class PostServiceContext : DbContext
{
    public PostServiceContext(DbContextOptions<PostServiceContext> options) : base(options)
    {
    }

    public DbSet<Post> Post { get; set; }
    public DbSet<User> User { get; set; }
}