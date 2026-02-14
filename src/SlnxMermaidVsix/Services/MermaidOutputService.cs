using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace SlnxMermaidVsix
{
    /// <summary>
    /// Dostarcza operacje związane z komunikacją z użytkownikiem w Visual Studio:
    /// Output Window (logowanie) oraz pasek statusu.
    /// </summary>
    internal sealed class MermaidOutputService
    {
        private static readonly Guid OutputPaneGuidValue =
            new Guid("7DB87FC4-AE9B-4AAB-9E32-F893A4B23DBB");

        public const string OutputPaneTitle = "Slnx Mermaid";

        private readonly AsyncPackage package;

        /// <summary>
        /// Tworzy serwis outputu oparty o bieżący pakiet VSIX.
        /// </summary>
        public MermaidOutputService(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Pobiera istniejący lub tworzy nowy panel wyjścia rozszerzenia i go aktywuje.
        /// </summary>
        public async Task<IVsOutputWindowPane> GetOrCreateOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(package.DisposalToken);

            IVsOutputWindow outputWindow =
                await package.GetServiceAsync(typeof(SVsOutputWindow))
                as IVsOutputWindow;

            if (outputWindow == null)
                throw new InvalidOperationException(
                    "Unable to acquire SVsOutputWindow service.");

            var paneGuid = OutputPaneGuidValue;

            outputWindow.CreatePane(
                ref paneGuid,
                OutputPaneTitle,
                1,
                1);

            outputWindow.GetPane(
                ref paneGuid,
                out IVsOutputWindowPane pane);

            if (pane == null)
                throw new InvalidOperationException(
                    "Unable to acquire the output pane.");

            pane.Activate();

            return pane;
        }

        /// <summary>
        /// Zapisuje pojedynczy wpis logu do panelu wyjścia wraz ze znacznikiem czasu.
        /// </summary>
        public async Task LogAsync(
            IVsOutputWindowPane pane,
            string message)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(package.DisposalToken);

            pane.OutputString(
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        /// <summary>
        /// Ustawia wiadomość na pasku statusu Visual Studio.
        /// </summary>
        public async Task SendMessageToStatusBarAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(package.DisposalToken);

            var statusBar = (await package.GetServiceAsync(typeof(SVsStatusbar)))
                as IVsStatusbar;

            if (statusBar != null)
            {
                statusBar.Clear();
                statusBar.SetText(message);
            }
        }
    }
}
