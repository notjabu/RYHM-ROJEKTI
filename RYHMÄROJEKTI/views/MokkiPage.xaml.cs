using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class MokkiPage : ContentPage
{
	public MokkiPage()
	{
		InitializeComponent();
		BindingContext = new MokkiPageViewModel();
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

	void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (BindingContext is MokkiPageViewModel vm)
		{
			var text = e.NewTextValue?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(text))
			{
				_ = vm.LataaMokitAsync();
				return;
			}

			var filtered = new System.Collections.Generic.List<MokkiItem>();
			foreach (var m in vm.Mokit)
			{
				if ((m.Mokkinimi ?? "").Contains(text, StringComparison.OrdinalIgnoreCase) ||
					(m.Toimipaikka ?? "").Contains(text, StringComparison.OrdinalIgnoreCase) ||
					(m.Postinro ?? "").Contains(text, StringComparison.OrdinalIgnoreCase))
				{
					filtered.Add(m);
				}
			}

			vm.Mokit.Clear();
			foreach (var it in filtered) vm.Mokit.Add(it);
		}
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
		public ObservableCollection<MokkiItem> Mokit { get; } = new();

		private MokkiItem _valittuMokki;
		public MokkiItem ValittuMokki
		{
			get => _valittuMokki;
			set { _valittuMokki = value; OnPropertyChanged(); }
		}

		public ICommand PoistaMokkiCommand { get; }

		public MokkiPageViewModel()
		{
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
						var resp = await ApiClient.Http.DeleteAsync($"/api/mokki/{ValittuMokki.Id.Value}");
						resp.EnsureSuccessStatusCode();
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

				var list = await ApiClient.Http.GetFromJsonAsync<List<MokkiDto>>("/api/mokki");
				if (list == null) return;

				foreach (var dto in list)
				{
					Mokit.Add(new MokkiItem
					{
						Id = dto.Id,
						AlueId = dto.AlueId,
						Postinro = dto.Postinro ?? string.Empty,
						Mokkinimi = dto.Mokkinimi ?? string.Empty,
						Katuosoite = dto.Katuosoite ?? string.Empty,
						Hinta = dto.Hinta,
						Kuvaus = dto.Kuvaus ?? string.Empty,
						Henkilomaara = dto.Henkilomaara,
						Varustelu = dto.Varustelu ?? string.Empty,
						Toimipaikka = dto.Toimipaikka ?? string.Empty
					});
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