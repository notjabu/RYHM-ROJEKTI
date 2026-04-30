namespace RYHMÄROJEKTI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new LoadingPage();
        InitializeAppAsync();
    }

    private async void InitializeAppAsync()
    {
        try
        {
            var loadingPage = MainPage as LoadingPage;

            // Attempt to connect to the API with retries
            int maxRetries = 10;
            int retryDelayMs = 1000;
            bool connected = false;

            for (int i = 0; i < maxRetries && !connected; i++)
            {
                try
                {
                    loadingPage?.UpdateStatus($"Yhdistetään palvelimeen... ({i + 1}/{maxRetries})");

                    // Simple health check - try to get any API data
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var response = await ApiClient.Http.GetAsync("/api/health", cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        connected = true;
                        loadingPage?.UpdateStatus("Tietokanta valmis!");
                        await Task.Delay(500);
                    }
                }
                catch
                {
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                }
            }

            if (!connected)
            {
                loadingPage?.UpdateStatus("Yhteysvirhe. Koetetaan uudelleen...");
                await Task.Delay(1000);
            }

            // Navigate to AppShell
            MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App initialization error: {ex}");
            MainPage = new AppShell();
        }
    }
}