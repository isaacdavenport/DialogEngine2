using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Xml.Serialization;

namespace DialogGenerator.Core
{
    public class ApplicationData
    {
        private static string msFileName = "Application.xml";
        private static ApplicationData msInstance;
        private static Object msLocker = new Object();
        private static XmlSerializer msSerializer;

        private string mRootDirectory;
        private string mDataDirectory;
        private string mTempDirectory;
        private string mVideoDirectory;
        private string mTutorialDirectory;
        private string mAudioDirectory;
        private string mImagesDirectory;

        static ApplicationData()
        {
            msSerializer = new XmlSerializer(typeof(ApplicationData));
        }

        public ApplicationData()
        {
        }

        private static ApplicationData _deserialize(string path)
        {

            using (var _fileStream = new FileStream(path, FileMode.Open))
            {
                return (ApplicationData)msSerializer.Deserialize(_fileStream);
            }
        }

        public void Save()
        {
            using (var _fileStream = new FileStream(Path.Combine(RootDirectory, msFileName), FileMode.Create))
            {
                msSerializer.Serialize(_fileStream, Instance);
            }
        }


        public static void Reload()
        {
            msInstance = null;
        }

        public static ApplicationData Instance
        {
            get
            {
                if (msInstance == null)
                {
                    lock (msLocker)
                    {
                        if (File.Exists(msFileName))
                        {
                            try
                            {
                                msInstance = _deserialize(msFileName);
                            }
                            catch (Exception)
                            {
                                // If error occured while deserializing application data from file, create new, empty instance of application data class
                                msInstance = new ApplicationData();
                            }
                        }
                        else
                        {
                            msInstance = new ApplicationData();

                            msInstance.Save();
                        }
                    }
                }

                return msInstance;
            }
        }

        public bool MonitorMessageParseFails { get; set; } = false;
        public bool CheckStuckTransmissions { get; set; } = false;
        public bool MonitorReceiveBufferSize { get; set; } = false;
        public bool ShowDupePhrases { get; set; } = false;
        public string JSONEditorExeFileName { get; set; } = "JSONedit.exe"; 
        public string WebsiteUrl { get; set; } = "www.toys2life.net";
        public string TutorialFileName { get; set; } = "tutorial.pdf";
        public string DecimaSerialLoggerKey { get; set; } = "DecimalSerialLog";
        public string DecimalSerialDirectBLELoggerKey { get; set; } = "DecimalSerialLogDirectBLE";
        public string DialogLoggerKey { get; set; } = "LogDialog";
        public string DefaultLoggerKey { get; set; } = "DefaultLog";
        public string DefaultImage { get; set; } = "avatar.png";

        public int NumberOfRadios { get; set; } = 6;

        [Editable(true)]
        [Description("Check which tags will not be used in dialog.")]
        [DisplayName("Check tag usage:")]
        public bool TagUsageCheck { get; set; } = false;

        [Editable(true)]
        [Description("Is dialog text visible during dialog.")]
        [DisplayName("Text dialog enabled:")]
        public bool TextDialogsOn { get; set; } = true;

        [Editable(true)]
        [Description(" Use serial or random selection of characters.")]
        [DisplayName("Use serial port:")]
        public bool UseSerialPort { get; set; } = false;

        [Editable(true)]
        [Description("COM port name")]
        [DisplayName("COM port name:")]
        [RegularExpression(@"^COM{1}\d+$", ErrorMessage = @"Text must starts wit 'COM' and finish with digit.")]
        public string ComPortName { get; set; } = "COM4";

        [Description("Determine how long current dialog can play, if new characters selected. Value is in seconds.")]
        [DisplayName("Max time to play .mp3 file:")]
        [RegularExpression(@"^[0-9]([.,][0-9]{1,3})?$", ErrorMessage = @"Field requires decimal number.")]
        public double MaxTimeToPlayFile { get; set; } = 1.5;

        [Editable(true)]
        [Description("  G - General Audiences, PG - Parental Guidance Suggested, PG13 - Parents Strongly Cautioned , R - Restricted - under 17 requires accompanying parent")]
        [DisplayName("Current parental rating:")]
        [RegularExpression(@"^(?:PG|G|PG13|R)$", ErrorMessage = @"Allowed values: PG,G,PG13,R.")]
        public string CurrentParentalRating { get; set; } = "PG";

        [Description("Delay between 2 phrases in dialog.")]
        [DisplayName("Delay between phrases:")]
        [RegularExpression(@"^[0-9]([.,][0-9]{1,3})?$", ErrorMessage = @"Field requires decimal number.")]
        public double DelayBetweenPhrases { get; set; } = 1.0;

        [DisplayName("Number of dialog models:")]
        [RegularExpression(@"^[1-9]+$", ErrorMessage = @"Field requires number.")]
        public int NumberOfDialogModelsCompleted { get; set; } = 5;

        [XmlIgnore]
        public string RootDirectory
        {
            get
            {
                if (mRootDirectory == null)
                {
                    mRootDirectory = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
                }

                return mRootDirectory;
            }
        }

        [XmlIgnore]
        public string DataDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mDataDirectory))
                {
                    mDataDirectory = Path.Combine(Instance.RootDirectory, "Data");
                }

                return mDataDirectory;
            }
        }

        [XmlIgnore]
        public string AudioDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mAudioDirectory))
                {
                    mAudioDirectory = Path.Combine(Instance.RootDirectory, "Audio");
                }

                return mAudioDirectory;
            }
        }

        [XmlIgnore]
        public string VideoDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mVideoDirectory))
                {
                    mVideoDirectory = Path.Combine(Instance.RootDirectory, "Video");
                }

                return mVideoDirectory;
            }
        }

        [XmlIgnore]
        public string TutorialDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mTutorialDirectory))
                {
                    mTutorialDirectory = Path.Combine(Instance.RootDirectory, "Tutorial");
                }

                return mTutorialDirectory;
            }
        }

        [XmlIgnore]
        public string TempDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mTempDirectory))
                {
                    mTempDirectory = Path.Combine(Instance.RootDirectory, "Temp");
                }

                return mTempDirectory;
            }
        }

        [XmlIgnore]
        public string ImagesDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(mImagesDirectory))
                {
                    mImagesDirectory = Path.Combine(Instance.RootDirectory, "Images");
                }

                return mImagesDirectory;
            }
        }
    }
}
