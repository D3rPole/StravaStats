using Microsoft.EntityFrameworkCore;
using StravaStats.BusinessObjects;
using System.Diagnostics;

namespace StravaStats.Services;

public class AppDbContext : DbContext
{
    // Quick tip: Make sure to include your model type <Product> here so EF Core knows what it's mapping!

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

        string myAppFolder = AppData.DataDirectory;

        if (!Directory.Exists(myAppFolder))
        {
            Directory.CreateDirectory(myAppFolder);
        }

        string dbPath = Path.Combine(myAppFolder, "app.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}
