using StravaStats.BusinessObjects;
using System.Text.Json;

namespace StravaStats.Services
{
    public class AccountService(ILogger<AccountService> logger, ActivityService activityService, StravaService stravaService) : IHostedService
    {
        public List<Account> Accounts { get; set; } = [];
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<Task> tasks = [];
            foreach (var accountDir in Directory.EnumerateDirectories(AppData.AccountsDirectory))
            {
                var task = Task.Run(async () => {
                    string accountFilePath = Path.Combine(accountDir, "Account.json");
                    string accountActivitiesPath = Path.Combine(accountDir, "Activities");

                    var fileStream = File.OpenRead(accountFilePath);
                    Account? account = JsonSerializer.Deserialize<Account>(fileStream);
                    await fileStream.DisposeAsync();
                    if (account is null)
                    {
                        logger.LogError("Couldn't Parse Accountfile");
                        return;
                    }
                    account.AccountDirectory = accountDir;
                    await stravaService.DownloadActivities(account, accountActivitiesPath);
                    account.Activities = await activityService.GetActivities(accountActivitiesPath, account.Name);
                    Accounts.Add(account);
                    await account.BuildGraphs();
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

        }
    }
}
