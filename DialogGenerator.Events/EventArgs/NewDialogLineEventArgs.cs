using DialogGenerator.Model;

namespace DialogGenerator.Events.EventArgs
{
    public class NewDialogLineEventArgs
    {
        public string CharacterName { get; set; }
        public string DialogLine { get; set; }
    }
}
