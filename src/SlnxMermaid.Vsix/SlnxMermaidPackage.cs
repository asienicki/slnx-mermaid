using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SlnxMermaid.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("slnx-mermaid", "Generate Mermaid diagram for .sln/.slnx files", "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(SolutionExistsContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class SlnxMermaidPackage : AsyncPackage
    {
        private const string SolutionExistsContextGuid = "f1536ef8-92ec-443c-9ed7-fdadf150da82";

        public const string PackageGuidString = "8f17fd67-7aa8-4fd6-8f7a-2f4bad8b9400";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await CreateMermaidDiagramCommand.InitializeAsync(this);
        }
    }
}
