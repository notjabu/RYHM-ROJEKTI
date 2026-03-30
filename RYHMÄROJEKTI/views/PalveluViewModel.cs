using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace RYHMÄROJEKTI.views
{
    public class PalveluViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<object> Palvelut { get; } = new();

        private object? _valittuPalvelu;
        public object? ValittuPalvelu
        {
            get => _valittuPalvelu;
            set
            {
                if (_valittuPalvelu == value) return;
                _valittuPalvelu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValittuPalvelu)); // update boolean
            }
        }

        // New boolean property used by XAML for IsVisible
        public bool HasValittuPalvelu => ValittuPalvelu != null;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}