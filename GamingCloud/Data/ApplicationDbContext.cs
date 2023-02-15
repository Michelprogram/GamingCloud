using GamingCloud.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GamingCloud.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<CustomVM> DbSet {
        get;
        set;
    }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}