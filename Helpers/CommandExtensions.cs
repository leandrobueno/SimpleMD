using System.Threading.Tasks;
using System.Windows.Input;

namespace SimpleMD.Helpers
{
    public static class CommandExtensions
    {
        public static async Task ExecuteAsync(this ICommand command, object? parameter = null)
        {
            if (command.CanExecute(parameter))
            {
                await Task.Run(() => command.Execute(parameter));
            }
        }
    }
}
