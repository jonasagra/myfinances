using Microsoft.EntityFrameworkCore.Design;

namespace MyFinances.Data;

public class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		return new AppDbContext(AppDbContext.Options("Data Source=design-time.db"));
	}
}
