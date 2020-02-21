using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.UI.ViewModels
{
    public class EditPhraseViewModel : BindableBase
    {
        private PhraseEntry mPhraseEntry;
        private string mDialogLineText;
        private string mFileName;
        private string mEditFileName;
        private string mPhraseWeights = string.Empty;
        private ICharacterDataProvider mCharacterDataProvider;
        private Character mCharacter;

        public EditPhraseViewModel(Character _Character, PhraseEntry _PhraseEntry, ICharacterDataProvider _CharacterDataProvider)
        {
            mPhraseEntry = _PhraseEntry;
            DialogLineText = mPhraseEntry.DialogStr;
            mCharacterDataProvider = _CharacterDataProvider;
            mCharacter = _Character;

            FileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + ".mp3");
            EditFileName = Path.Combine(ApplicationData.Instance.AudioDirectory, _Character.CharacterPrefix + "_" + mPhraseEntry.FileName + "_edit.mp3");
            
            if(File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }

            File.Copy(FileName, EditFileName);

            foreach (var entry in mPhraseEntry.PhraseWeights)
            {
                if(string.IsNullOrEmpty(PhraseWeights)) {
                    PhraseWeights = "";
                } else
                {
                    PhraseWeights += ", ";
                }

                PhraseWeights += entry.Key;
                PhraseWeights += "/";
                PhraseWeights += entry.Value.ToString();
            }

            CloseCommand = new DelegateCommand(_viewClose_Execute);

        }

        

        #region Properties

        public string PhraseWeights
        {
            get
            {
                return mPhraseWeights;
            }

            set
            {
                mPhraseWeights = value;
                RaisePropertyChanged();
            }
        }

        public string DialogLineText
        {
            get
            {
                return mDialogLineText;
            }

            set
            {
                mDialogLineText = value;
                RaisePropertyChanged();
            }
        }

        public string FileName
        {
            get
            {
                return mFileName;
            }

            set
            {
                mFileName = value;
                RaisePropertyChanged();
            }
        }

        public string EditFileName
        {
            get
            {
                return mEditFileName;
            }
            
            set
            {
                mEditFileName = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public DelegateCommand CloseCommand { get; set; }

        public async Task SaveChanges()
        {
            File.Delete(FileName);
            File.Copy(EditFileName, FileName);
            foreach(var _phraseEntry in mCharacter.Phrases)
            {
                if(_phraseEntry.Equals(mPhraseEntry))
                {
                    _phraseEntry.DialogStr = DialogLineText;
                    string[] _tokens = PhraseWeights.Split(',');
                    if(_tokens.Length > 0)
                    {
                        _phraseEntry.PhraseWeights.Clear();
                        foreach (var _token in _tokens)
                        {
                            string[] _parts = _token.Trim(' ').Split('/');
                            if(_parts.Length == 1)
                            {
                                string _key = _parts[0].Trim(' ');
                                _parts = new string[2];
                                _parts[0] = _key;
                                _parts[1] = "10";
                            }

                            _phraseEntry.PhraseWeights.Add(_parts[0].Trim(' '), Double.Parse(_parts[1].Trim(' ')));
                        }
                    }                    
                }
            }

            await mCharacterDataProvider.SaveAsync(mCharacter);            
        }

        private void _viewClose_Execute()
        {
            if(!string.IsNullOrEmpty(EditFileName) && File.Exists(EditFileName))
            {
                File.Delete(EditFileName);
            }
        }
    }
}
