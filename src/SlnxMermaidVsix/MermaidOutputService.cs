using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace SlnxMermaidVsix
{
    internal sealed class MermaidOutputService
    {
        private static readonly Guid OutputPaneGuidValue =
            new Guid("7DB87FC4-AE9B-4AAB-9E32-F893A4B23DBB");

        public const string OutputPaneTitle = "Slnx Mermaid";

        private readonly AsyncPackage package;

        public MermaidOutputService(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public async Task<IVsOutputWindowPane> GetOrCreateOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            IVsOutputWindow outputWindow =
                await this.package.GetServiceAsync(typeof(SVsOutputWindow))
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

        public async Task LogAsync(
            IVsOutputWindowPane pane,
            string message)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            pane.OutputString(
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public async Task SendMessageToStatusBarAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(this.package.DisposalToken);

            var statusBar = (await this.package.GetServiceAsync(typeof(SVsStatusbar)))
                as IVsStatusbar;

            if (statusBar != null)
            {
                statusBar.Clear();
                statusBar.SetText(message);
            }
        }
    }
}
