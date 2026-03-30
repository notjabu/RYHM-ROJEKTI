using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class MokkiPage : ContentPage
{
	private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

	public MokkiPage()
	{
		InitializeComponent();
		BindingContext = new MokkiPageViewModel(ConnectionString);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is MokkiPageViewModel vm)
		{
			await vm.LataaMokitAsync();
		}
	}

	async void LisaaMokki_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("MokkiPageEdit");
	}

	async void Muokkaa_Clicked(object sender, EventArgs e)
	{
		if (BindingContext is not MokkiPageViewModel vm || vm.ValittuMokki == null)
		{
			await DisplayAlert("Huom", "Valitse muokattava mökki ensin.", "OK");
			return;
		}

		var mokki = vm.ValittuMokki;
		if (mokki.Id.HasValue)
		{
			await Shell.Current.GoToAsync($"MokkiPageEdit?mokkiId={mokki.Id.Value}");
		}
		else
		{
			await Shell.Current.GoToAsync("MokkiPageEdit");
		}
	}

	// ── ViewModel ───────────────────────────────────────────────────────
	class MokkiPageViewModel : INotifyPropertyChanged
	{
		private readonly string _connString;

		public ObservableCollection<MokkiItem> Mokit { get; } = new();

		private MokkiItem _valittuMokki;
		public MokkiItem ValittuMokki
		{
			get => _valittuMokki;
			set { _valittuMokki = value; OnPropertyChanged(); }
		}

		public ICommand PoistaMokkiCommand { get; }

		public MokkiPageViewModel(string connectionString)
		{
			_connString = connectionString;

			PoistaMokkiCommand = new Command(async () =>
			{
				if (ValittuMokki == null)
				{
					await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin mökki.", "OK");
					return;
				}

				bool ok = await Application.Current.MainPage.DisplayAlert(
					"Vahvista", $"Poistetaanko mökki \"{ValittuMokki.Mokkinimi}\"?", "Kyllä", "Ei");
				if (!ok) return;

				try
				{
					if (ValittuMokki.Id.HasValue)
					{
						await using var conn = new MySqlConnection(_connString);
						await conn.OpenAsync();
						const string sql = "DELETE FROM mokki WHERE mokki_id = @id";
						await using var cmd = new MySqlCommand(sql, conn);
						cmd.Parameters.AddWithValue("@id", ValittuMokki.Id.Value);
						await cmd.ExecuteNonQueryAsync();
					}
				}
				catch (Exception ex)
				{
					await Application.Current.MainPage.DisplayAlert(
						"Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
				}

				Mokit.Remove(ValittuMokki);
				ValittuMokki = null;
			});
		}

		public async Task LataaMokitAsync()
		{
			try
			{
				Mokit.Clear();

				await using var conn = new MySqlConnection(_connString);
				await conn.OpenAsync();

				const string sql = @"
					SELECT m.mokki_id, m.alue_id, m.postinro, m.mokkinimi,
						   m.katuosoite, m.hinta, m.kuvaus,
						   m.henkilomaara, m.varustelu,
						   p.toimipaikka
					FROM mokki m
					LEFT JOIN posti p ON p.postinro = m.postinro
					ORDER BY m.mokkinimi";

				await using var cmd = new MySqlCommand(sql, conn);
				await using var rdr = await cmd.ExecuteReaderAsync();

				while (await rdr.ReadAsync())
				{
					var item = new MokkiItem
					{
						Id = rdr.IsDBNull(rdr.GetOrdinal("mokki_id")) ? null : rdr.GetInt32("mokki_id"),
						AlueId = rdr.IsDBNull(rdr.GetOrdinal("alue_id")) ? null : rdr.GetInt32("alue_id"),
						Postinro = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro"),
						Mokkinimi = rdr.IsDBNull(rdr.GetOrdinal("mokkinimi")) ? string.Empty : rdr.GetString("mokkinimi"),
						Katuosoite = rdr.IsDBNull(rdr.GetOrdinal("katuosoite")) ? string.Empty : rdr.GetString("katuosoite"),
						Hinta = rdr.IsDBNull(rdr.GetOrdinal("hinta")) ? 0 : rdr.GetDouble("hinta"),
						Kuvaus = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus"),
						Henkilomaara = rdr.IsDBNull(rdr.GetOrdinal("henkilomaara")) ? 0 : rdr.GetInt32("henkilomaara"),
						Varustelu = rdr.IsDBNull(rdr.GetOrdinal("varustelu")) ? string.Empty : rdr.GetString("varustelu"),
						Toimipaikka = rdr.IsDBNull(rdr.GetOrdinal("toimipaikka")) ? string.Empty : rdr.GetString("toimipaikka")
					};

					Mokit.Add(item);
				}
			}
			catch (Exception ex)
			{
				await Application.Current.MainPage.DisplayAlert(
					"Virhe", "Mökkien lataus epäonnistui: " + ex.Message, "OK");
				System.Diagnostics.Debug.WriteLine("LataaMokitAsync error: " + ex);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged([CallerMemberName] string name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	// ── Model ───────────────────────────────────────────────────────────
	class MokkiItem
	{
		public int? Id { get; set; }
		public int? AlueId { get; set; }
		public string Postinro { get; set; }
		public string Mokkinimi { get; set; }
		public string Katuosoite { get; set; }
		public double Hinta { get; set; }
		public string Kuvaus { get; set; }
		public int Henkilomaara { get; set; }
		public string Varustelu { get; set; }
		public string Toimipaikka { get; set; }     // from posti table
		public string Kuva { get; set; }            // optional thumbnail
	}
}