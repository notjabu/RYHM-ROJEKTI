using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RYHMÄROJEKTI.Modeelit;

public class Alue : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int AlueId { get; set; }

    private string _nimi;
    public string Nimi
    {
        get => _nimi;
        set
        {
            _nimi = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}