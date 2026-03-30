using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class AsiakasPage : ContentPage
{
	private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

	public AsiakasPage()
	{
		InitializeComponent();
		BindingContext = new AsiakasPageViewModel(ConnectionString);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is AsiakasPageViewModel vm)
		{
			await vm.LataaAsiakkaatAsync();
			await vm.LataaVarauksetAsync();
		}
	}

	async void LisaaAsiakas_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AsiakasPageEdit");
	}

	async void Muokkaa_Clicked(object sender, EventArgs e)
	{
		if (BindingContext is not AsiakasPageViewModel vm || vm.ValittuAsiakas == null)
		{
			await DisplayAlert("Huom", "Valitse muokattava asiakas ensin.", "OK");
			return;
		}

		var asiakas = vm.ValittuAsiakas;
		if (asiakas.Id.HasValue)
		{
			await Shell.Current.GoToAsync($"AsiakasPageEdit?asiakasId={asiakas.Id.Value}");
		}
		else
		{
			await Shell.Current.GoToAsync("AsiakasPageEdit");
		}
	}

	// ── ViewModel ───────────────────────────────────────────────────────
	class AsiakasPageViewModel : INotifyPropertyChanged
	{
		private readonly string _connString;

		public ObservableCollection<AsiakasItem> Asiakkaat { get; } = new();
		public ObservableCollection<VarausItem> Varaukset { get; } = new();

		private AsiakasItem _valittuAsiakas;
		public AsiakasItem ValittuAsiakas
		{
			get => _valittuAsiakas;
			set
			{
				_valittuAsiakas = value;
				OnPropertyChanged();
				PaivitaVarausHighlights();
			}
		}

		public ICommand PoistaAsiakasCommand { get; }

		public AsiakasPageViewModel(string connectionString)
		{
			_connString = connectionString;

			PoistaAsiakasCommand = new Command(async () =>
			{
				if (ValittuAsiakas == null)
				{
					await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin asiakas.", "OK");
					return;
				}

				bool ok = await Application.Current.MainPage.DisplayAlert(
					"Vahvista", $"Poistetaanko asiakas \"{ValittuAsiakas.KokoNimi}\"?", "Kyllä", "Ei");
				if (!ok) return;

				try
				{
					if (ValittuAsiakas.Id.HasValue)
					{
						await using var conn = new MySqlConnection(_connString);
						await conn.OpenAsync();
						const string sql = "DELETE FROM asiakas WHERE asiakas_id = @id";
						await using var cmd = new MySqlCommand(sql, conn);
						cmd.Parameters.AddWithValue("@id", ValittuAsiakas.Id.Value);
						await cmd.ExecuteNonQueryAsync();
					}
				}
				catch (Exception ex)
				{
					await Application.Current.MainPage.DisplayAlert(
						"Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
				}

				Asiakkaat.Remove(ValittuAsiakas);
				ValittuAsiakas = null;
			});
		}

		public async Task LataaAsiakkaatAsync()
		{
			try
			{
				Asiakkaat.Clear();

				await using var conn = new MySqlConnection(_connString);
				await conn.OpenAsync();

				const string sql = @"
					SELECT a.asiakas_id, a.etunimi, a.sukunimi, a.lahiosoite,
						   a.postinro, a.email, a.puhelinnro,
						   p.toimipaikka
					FROM asiakas a
					LEFT JOIN posti p ON p.postinro = a.postinro
					ORDER BY a.sukunimi, a.etunimi";

				await using var cmd = new MySqlCommand(sql, conn);
				await using var rdr = await cmd.ExecuteReaderAsync();

				while (await rdr.ReadAsync())
				{
					var item = new AsiakasItem
					{
						Id = rdr.IsDBNull(rdr.GetOrdinal("asiakas_id")) ? null : rdr.GetInt32("asiakas_id"),
						Etunimi = rdr.IsDBNull(rdr.GetOrdinal("etunimi")) ? string.Empty : rdr.GetString("etunimi"),
						Sukunimi = rdr.IsDBNull(rdr.GetOrdinal("sukunimi")) ? string.Empty : rdr.GetString("sukunimi"),
						Lahiosoite = rdr.IsDBNull(rdr.GetOrdinal("lahiosoite")) ? string.Empty : rdr.GetString("lahiosoite"),
						Postinro = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro"),
						Email = rdr.IsDBNull(rdr.GetOrdinal("email")) ? string.Empty : rdr.GetString("email"),
						Puhelinnro = rdr.IsDBNull(rdr.GetOrdinal("puhelinnro")) ? string.Empty : rdr.GetString("puhelinnro"),
						Toimipaikka = rdr.IsDBNull(rdr.GetOrdinal("toimipaikka")) ? string.Empty : rdr.GetString("toimipaikka")
					};

					Asiakkaat.Add(item);
				}
			}
			catch (Exception ex)
			{
				await Application.Current.MainPage.DisplayAlert(
					"Virhe", "Asiakkaiden lataus epäonnistui: " + ex.Message, "OK");
				System.Diagnostics.Debug.WriteLine("LataaAsiakkaatAsync error: " + ex);
			}
		}

		private void PaivitaVarausHighlights()
		{
			var selectedId = ValittuAsiakas?.Id;
			foreach (var v in Varaukset)
				v.IsHighlighted = selectedId.HasValue && v.AsiakasId == selectedId.Value;
		}

		public async Task LataaVarauksetAsync()
		{
			try
			{
				Varaukset.Clear();

				await using var conn = new MySqlConnection(_connString);
				await conn.OpenAsync();

				const string sql = @"
					SELECT v.varaus_id, v.asiakas_id, v.mokki_id,
						   v.varattu_pvm, v.vahvistus_pvm,
						   v.varattu_alkupvm, v.varattu_loppupvm,
						   a.etunimi, a.sukunimi,
						   m.mokkinimi
					FROM varaus v
					LEFT JOIN asiakas a ON a.asiakas_id = v.asiakas_id
					LEFT JOIN mokki m ON m.mokki_id = v.mokki_id
					ORDER BY v.varattu_alkupvm DESC";

				await using var cmd = new MySqlCommand(sql, conn);
				await using var rdr = await cmd.ExecuteReaderAsync();

				while (await rdr.ReadAsync())
				{
					var etunimi = rdr.IsDBNull(rdr.GetOrdinal("etunimi")) ? string.Empty : rdr.GetString("etunimi");
					var sukunimi = rdr.IsDBNull(rdr.GetOrdinal("sukunimi")) ? string.Empty : rdr.GetString("sukunimi");

					var item = new VarausItem
					{
						VarausId = rdr.IsDBNull(rdr.GetOrdinal("varaus_id")) ? null : rdr.GetInt32("varaus_id"),
						AsiakasId = rdr.IsDBNull(rdr.GetOrdinal("asiakas_id")) ? null : rdr.GetInt32("asiakas_id"),
						AsiakasNimi = $"{etunimi} {sukunimi}".Trim(),
						MokkiId = rdr.IsDBNull(rdr.GetOrdinal("mokki_id")) ? null : rdr.GetInt32("mokki_id"),
						MokkiNimi = rdr.IsDBNull(rdr.GetOrdinal("mokkinimi")) ? string.Empty : rdr.GetString("mokkinimi"),
						VarattuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_pvm")) ? string.Empty : rdr.GetDateTime("varattu_pvm").ToString("dd.MM.yyyy"),
						VahvistusPvm = rdr.IsDBNull(rdr.GetOrdinal("vahvistus_pvm")) ? string.Empty : rdr.GetDateTime("vahvistus_pvm").ToString("dd.MM.yyyy"),
						VarattuAlkuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_alkupvm")) ? string.Empty : rdr.GetDateTime("varattu_alkupvm").ToString("dd.MM.yyyy"),
						VarattuLoppuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_loppupvm")) ? string.Empty : rdr.GetDateTime("varattu_loppupvm").ToString("dd.MM.yyyy")
					};

					Varaukset.Add(item);
				}

				PaivitaVarausHighlights();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("LataaVarauksetAsync error: " + ex);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged([CallerMemberName] string name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	// ── Model ───────────────────────────────────────────────────────────
	class AsiakasItem
	{
		public int? Id { get; set; }
		public string Etunimi { get; set; } = string.Empty;
		public string Sukunimi { get; set; } = string.Empty;
		public string Lahiosoite { get; set; } = string.Empty;
		public string Postinro { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Puhelinnro { get; set; } = string.Empty;
		public string Toimipaikka { get; set; } = string.Empty;
		public string KokoNimi => $"{Etunimi} {Sukunimi}".Trim();
	}

	class VarausItem : INotifyPropertyChanged
	{
		public int? VarausId { get; set; }
		public int? AsiakasId { get; set; }
		public string AsiakasNimi { get; set; } = string.Empty;
		public int? MokkiId { get; set; }
		public string MokkiNimi { get; set; } = string.Empty;
		public string VarattuPvm { get; set; } = string.Empty;
		public string VahvistusPvm { get; set; } = string.Empty;
		public string VarattuAlkuPvm { get; set; } = string.Empty;
		public string VarattuLoppuPvm { get; set; } = string.Empty;
		public string AikavaliText => $"{VarattuAlkuPvm} – {VarattuLoppuPvm}";
		public string VarattuPvmText => $"Varattu: {VarattuPvm}";
		public string VahvistusPvmText => !string.IsNullOrEmpty(VahvistusPvm) ? $"Vahvistettu: {VahvistusPvm}" : "Ei vahvistettu";

		private bool _isHighlighted;
		public bool IsHighlighted
		{
			get => _isHighlighted;
			set
			{
				if (_isHighlighted == value) return;
				_isHighlighted = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighlightColor)));
			}
		}

		public Color HighlightColor => IsHighlighted ? Color.FromArgb("#D6BCFA") : Colors.White;

		public event PropertyChangedEventHandler PropertyChanged;
	}
}