using DialogGenerator.Model;

namespace DialogGenerator.Events.EventArgs
{
    public class NewDialogLineEventArgs
    {
        public Character Character { get; set; }
        public string DialogLine { get; set; }
    }
}
