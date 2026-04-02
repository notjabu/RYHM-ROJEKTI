using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class AsiakasPage : ContentPage
{
public AsiakasPage()
	{
		InitializeComponent();
		BindingContext = new AsiakasPageViewModel();
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

	async void LisaaVaraus_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("VarausPageEdit");
	}

	async void Muokkaa_Clicked(object sender, EventArgs e)
	{
		if (BindingContext is not AsiakasPageViewModel vm)
			return;

		if (vm.LastSelection == SelectionType.Varaus && vm.ValittuVaraus != null)
		{
			var varaus = vm.ValittuVaraus;
			if (varaus.VarausId.HasValue)
				await Shell.Current.GoToAsync($"VarausPageEdit?varausId={varaus.VarausId.Value}");
		}
		else if (vm.LastSelection == SelectionType.Asiakas && vm.ValittuAsiakas != null)
		{
			var asiakas = vm.ValittuAsiakas;
			if (asiakas.Id.HasValue)
				await Shell.Current.GoToAsync($"AsiakasPageEdit?asiakasId={asiakas.Id.Value}");
			else
				await Shell.Current.GoToAsync("AsiakasPageEdit");
		}
		else
		{
			await DisplayAlert("Huom", "Valitse muokattava asiakas tai varaus ensin.", "OK");
		}
	}

	// ── ViewModel ───────────────────────────────────────────────────────
	enum SelectionType { None, Asiakas, Varaus }

	class AsiakasPageViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<AsiakasItem> Asiakkaat { get; } = new();
		public ObservableCollection<VarausItem> Varaukset { get; } = new();

		public SelectionType LastSelection { get; private set; } = SelectionType.None;

		private AsiakasItem _valittuAsiakas;
		public AsiakasItem ValittuAsiakas
		{
			get => _valittuAsiakas;
			set
			{
				_valittuAsiakas = value;
				if (value != null)
					LastSelection = SelectionType.Asiakas;
				OnPropertyChanged();
				PaivitaVarausHighlights();
			}
		}

		private VarausItem _valittuVaraus;
		public VarausItem ValittuVaraus
		{
			get => _valittuVaraus;
			set
			{
				if (_valittuVaraus != null)
					_valittuVaraus.IsSelected = false;
				_valittuVaraus = value;
				if (_valittuVaraus != null)
				{
					_valittuVaraus.IsSelected = true;
					LastSelection = SelectionType.Varaus;
				}
				OnPropertyChanged();
			}
		}

		public ICommand PoistaCommand { get; }

		public AsiakasPageViewModel()
		{
			PoistaCommand = new Command(async () =>
			{
				if (LastSelection == SelectionType.Varaus && ValittuVaraus != null)
				{
					var varaus = ValittuVaraus;
					bool ok = await Application.Current.MainPage.DisplayAlert(
						"Vahvista", $"Poistetaanko varaus ({varaus.AsiakasNimi}, {varaus.MokkiNimi})?", "Kyllä", "Ei");
					if (!ok) return;

					try
					{
						if (varaus.VarausId.HasValue)
						{
							var resp = await ApiClient.Http.DeleteAsync($"/api/varaus/{varaus.VarausId.Value}");
							resp.EnsureSuccessStatusCode();
						}
					}
					catch (Exception ex)
					{
						await Application.Current.MainPage.DisplayAlert(
							"Virhe", "Varauksen poisto epäonnistui: " + ex.Message, "OK");
						return;
					}

					Varaukset.Remove(varaus);
					ValittuVaraus = null;
				}
				else if (LastSelection == SelectionType.Asiakas && ValittuAsiakas != null)
				{
					var asiakas = ValittuAsiakas;
					bool ok = await Application.Current.MainPage.DisplayAlert(
						"Vahvista", $"Poistetaanko asiakas \"{asiakas.KokoNimi}\"?", "Kyllä", "Ei");
					if (!ok) return;

					try
					{
						if (asiakas.Id.HasValue)
						{
							var resp = await ApiClient.Http.DeleteAsync($"/api/asiakas/{asiakas.Id.Value}");
							resp.EnsureSuccessStatusCode();
						}
					}
					catch (Exception ex)
					{
						await Application.Current.MainPage.DisplayAlert(
							"Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
						return;
					}

					Asiakkaat.Remove(asiakas);
					ValittuAsiakas = null;
				}
				else
				{
					await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin asiakas tai varaus.", "OK");
				}
			});
		}

		public async Task LataaAsiakkaatAsync()
		{
			try
			{
				Asiakkaat.Clear();

				var list = await ApiClient.Http.GetFromJsonAsync<List<AsiakasDto>>("/api/asiakas");
				if (list == null) return;

				foreach (var dto in list)
				{
					Asiakkaat.Add(new AsiakasItem
					{
						Id = dto.Id,
						Etunimi = dto.Etunimi ?? string.Empty,
						Sukunimi = dto.Sukunimi ?? string.Empty,
						Lahiosoite = dto.Lahiosoite ?? string.Empty,
						Postinro = dto.Postinro ?? string.Empty,
						Email = dto.Email ?? string.Empty,
						Puhelinnro = dto.Puhelinnro ?? string.Empty,
						Toimipaikka = dto.Toimipaikka ?? string.Empty
					});
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

				var list = await ApiClient.Http.GetFromJsonAsync<List<VarausDto>>("/api/varaus");
				if (list == null) return;

				foreach (var dto in list)
				{
					var item = new VarausItem
					{
						VarausId = dto.VarausId,
						AsiakasId = dto.AsiakasId,
						AsiakasNimi = $"{dto.Etunimi} {dto.Sukunimi}".Trim(),
						MokkiId = dto.MokkiId,
						MokkiNimi = dto.MokkiNimi ?? string.Empty,
						VarattuPvm = dto.VarattuPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
						VahvistusPvm = dto.VahvistusPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
						VarattuAlkuPvm = dto.VarattuAlkuPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
						VarattuLoppuPvm = dto.VarattuLoppuPvm?.ToString("dd.MM.yyyy") ?? string.Empty
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

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected == value) return;
				_isSelected = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighlightColor)));
			}
		}

		public Color HighlightColor => IsSelected ? Color.FromArgb("#D0D0D0") : IsHighlighted ? Color.FromArgb("#D6BCFA") : Colors.White;

		public event PropertyChangedEventHandler PropertyChanged;
	}
}