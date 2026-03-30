using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RYHMÄROJEKTI.Modeelit;

public class Mokki : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    private string _nimi;
    public string Nimi
    {
        get => _nimi;
        set { _nimi = value; OnPropertyChanged(); }
    }

    private string _alue;
    public string Alue
    {
        get => _alue;
        set { _alue = value; OnPropertyChanged(); }
    }

    private string _postitoimipaikka;
    public string Postitoimipaikka
    {
        get => _postitoimipaikka;
        set { _postitoimipaikka = value; OnPropertyChanged(); }
    }

    private string _katuosoite;
    public string Katuosoite
    {
        get => _katuosoite;
        set { _katuosoite = value; OnPropertyChanged(); }
    }

    private string _kuvaus;
    public string Kuvaus
    {
        get => _kuvaus;
        set { _kuvaus = value; OnPropertyChanged(); }
    }

    private double _hintaPerPaiva;
    public double HintaPerPaiva
    {
        get => _hintaPerPaiva;
        set { _hintaPerPaiva = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}