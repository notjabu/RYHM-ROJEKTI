namespace RYHMÄROJEKTI;

using RYHMÄROJEKTI.Services;

public partial class App : Application
{
    // Luodaan staattinen yhteys, jota voi kutsua mistä vain
    public static DatabaseService Database { get; private set; }

    public App()
    {
        InitializeComponent();

        // Alustetaan tietokantapalvelu
        Database = new DatabaseService();

        MainPage = new AppShell();
    }
}