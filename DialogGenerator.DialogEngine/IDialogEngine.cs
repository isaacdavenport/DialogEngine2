using System.Threading.Tasks;

namespace DialogGenerator.DialogEngine
{
    public interface IDialogEngine
    {
        Task StartDialogEngine();
        void StopDialogEngine();

        void Initialize();
    }
}
