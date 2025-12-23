namespace NetworkService.Services
{
    public interface IWindowService
    {
        void ShowTerminal(ViewModel.MainWindowViewModel mainVm);
        void CloseTerminal();
        bool IsTerminalOpen { get; }
    }
}
