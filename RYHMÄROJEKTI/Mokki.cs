using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RYHMÄROJEKTI.Models
{
    public class Mokki : INotifyPropertyChanged
    {
        private string _nimi;
        private string _katuosoite;
        private string _postinumero;
        private string _postitoimipaikka;
        private string _kuvaus;
        private string _kuva; // tiedostopolku tai URI

        public string Nimi { get => _nimi; set { _nimi = value; OnPropertyChanged(); } }
        public string Katuosoite { get => _katuosoite; set { _katuosoite = value; OnPropertyChanged(); } }
        public string Postinumero { get => _postinumero; set { _postinumero = value; OnPropertyChanged(); } }
        public string Postitoimipaikka { get => _postitoimipaikka; set { _postitoimipaikka = value; OnPropertyChanged(); } }
        public string Kuvaus { get => _kuvaus; set { _kuvaus = value; OnPropertyChanged(); } }
        public string Kuva { get => _kuva; set { _kuva = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}