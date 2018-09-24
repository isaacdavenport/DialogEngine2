﻿using DialogGenerator.Model.Enum;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace DialogGenerator.Model
{
    /// <summary>
    /// This is subset of <see cref="ModelDialog" />
    /// It is used to store basic information about dialog models, which should be faster then parsing all json files 
    /// </summary>
    public class ModelDialogInfo:INotifyPropertyChanged
    {
        private ModelDialogState mState;
        private int mSelectedModelDialogIndex=-1;

        [JsonProperty("ArrayOfDialogModels")]
        public List<ModelDialog> ArrayOfDialogModels { get; set; }

        [JsonProperty("ModelsCollectionName")]
        public string ModelsCollectionName { set; get; }

        /// <summary>
        /// Index of selected dialog model from dialog .json file
        /// </summary>
        [JsonIgnore]
        public int SelectedModelDialogIndex
        {
            get { return mSelectedModelDialogIndex; }
            set
            {
                mSelectedModelDialogIndex = value;
                OnPropertyChanged("SelectedModelDialogIndex");
            }
        }

        /// <summary>
        /// Represents state of dialog .json file
        /// States are [On, Off]
        /// Default state is On
        /// On - we want to load dialog models from dialog .json file
        /// Off - ignore dilogs from dialog .json file
        /// </summary>
        [JsonIgnore]
        public ModelDialogState State
        {
            get { return mState; }
            set
            {
                mState = value;
                OnPropertyChanged("State");
            }
        }

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public int JsonArrayIndex { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }
    }
}
