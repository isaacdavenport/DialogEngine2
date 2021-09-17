using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.DialogEngine
{
    public interface IDialogEngine : INotifyPropertyChanged
    {
        CancellationTokenSource PauseCancellationTokenSource { get; set; }
        bool Running { get; set; }
        Task StartDialogEngine();
        void StopDialogEngine();

        void Initialize();
    }
}
