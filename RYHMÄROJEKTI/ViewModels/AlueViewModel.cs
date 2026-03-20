using System.Windows.Input;
using Microsoft.Maui.Controls;

public class AlueViewModel
{
    public object ValittuAlue { get; set; }

    public ICommand MuokkaaAluettaCommand { get; }

    public AlueViewModel()
    {
        MuokkaaAluettaCommand = new Command(
            execute: async () => await Shell.Current.GoToAsync("///AluePageEdit"),
            canExecute: () => ValittuAlue != null
        );
    }

    // When ValittuAlue changes, call:
    // (MuokkaaAluettaCommand as Command)?.ChangeCanExecute();
}