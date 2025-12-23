using System.Linq;
using System.Windows;
using NetworkService.ViewModel;
using NetworkService.Views;

namespace NetworkService.Services
{
    public class WindowService : IWindowService
    {
        private TerminalWindow _terminal;   // koristimo konkretan Window

        public bool IsTerminalOpen => _terminal != null;

        public void ShowTerminal(MainWindowViewModel mainVm)
        {
            if (_terminal != null)
            {
                _terminal.Activate();
                return;
            }

            _terminal = new TerminalWindow
            {
                Owner = Application.Current?.Windows?.OfType<Window>()?.FirstOrDefault(w => w.IsActive)
                        ?? Application.Current?.MainWindow,
                DataContext = new TerminalViewModel(mainVm, this)
            };

            _terminal.Closed += (_, __) => { _terminal = null; };
            _terminal.Show();
            _terminal.Activate();
        }

        public void CloseTerminal()
        {
            _terminal?.Close();
            _terminal = null;
        }
    }
}
