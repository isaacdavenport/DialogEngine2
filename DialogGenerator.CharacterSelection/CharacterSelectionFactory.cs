namespace DialogGenerator.CharacterSelection
{
    public class CharacterSelectionFactory : ICharacterSelectionFactory
    {
        private readonly ICharacterSelection mArenaCharacterSelection;

        public CharacterSelectionFactory(ICharacterSelection arenaCharacterSelection)
        {
            this.mArenaCharacterSelection = arenaCharacterSelection;
        }

        public ICharacterSelection Create()
        {
            return mArenaCharacterSelection;
        }
    }
}
