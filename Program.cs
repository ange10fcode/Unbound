using System;
using System.Windows.Forms;
using Unbound.Shell;
using Unbound.UI;

namespace Unbound;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (ShellCommandRouter.TryHandle(args))
            return;

        Application.Run(new MainForm());
    }
}
