namespace DialogGenerator.Core
{
    public static class Constants
    {
        // region names

        public static string MenuRegion = "MenuRegion";
        public static string StatusBarRegion = "StatusBarRegions";
        public static string NavigationRegion = "NavigationRegion";
        public static string ContentRegion = "ContentRegion";

        // session constants

        public const string CHARACTERS = "Characters";
        public const string DIALOG_MODELS = "DialogModels";
        public const string WIZARDS = "Wizards";
        public const string FORCED_CH_COUNT = "SelectedCharactesOn";
        public const string NEXT_CH_1 = "NextCharacter1";
        public const string NEXT_CH_2 = "NextCharacter2";
        public const string FORCED_CH_1 = "SelectedIndex1";
        public const string FORCED_CH_2 = "SelectedIndex2";
        public const string DIALOG_SPEED = "DialogSpeed";
        public const string SELECTED_DLG_MODEL = "SelectedDialogModel";
        public const string COMPLETED_DLG_MODELS = "CompletedDialogModels";
        public const string BLE_DATA_PROVIDER = "BLEDataProvider";
        public const string FILENAME_CHECK_REGEX = @"^[-a-zA-Z0-9_' ]+$";
        public const string CHARACTER_EDIT_MODE = "CharacterEditMode";
        public const string NEW_CHARACTER = "NewCharacter";
        public const string CREATE_CHARACTER_VIEW_MODEL = "CreateCharacterViewModel";
        public const string BLE_MODE_ON = "BLEModeSwitchedOn";
        public const string NEEDS_RESTART = "NeedsRestart";
        
    }
}
