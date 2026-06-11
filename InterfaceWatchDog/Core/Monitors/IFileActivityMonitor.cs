using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Monitors;

public interface IFileActivityMonitor
{
    FileActivityStatus Check(PdfFolderConfig config);
}
