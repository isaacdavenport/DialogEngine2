﻿using System.Collections.ObjectModel;
using DialogGenerator.DataAccess;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;

namespace DialogGenerator.UI.Data
{
    public class DialogModelDataProvider : IDialogModelDataProvider
    {
        private IDialogModelRepository mDialogModelRepository;

        public DialogModelDataProvider(IDialogModelRepository _dialogModelRepository)
        {
            mDialogModelRepository = _dialogModelRepository;
        }

        public ObservableCollection<ModelDialogInfo> GetAll()
        {
            return mDialogModelRepository.GetAll();
        }

        public ObservableCollection<ModelDialogInfo> GetAllByState(ModelDialogState state)
        {
            return mDialogModelRepository.GetAllByState(state);
        }

        public ModelDialogInfo GetByName(string name)
        {
            return mDialogModelRepository.GetByName(name);
        }
    }
}
