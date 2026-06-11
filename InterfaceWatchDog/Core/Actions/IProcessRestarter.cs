using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Actions;

public interface IProcessRestarter
{
    RestartResult TryRestart(ProgramConfig config);
}
