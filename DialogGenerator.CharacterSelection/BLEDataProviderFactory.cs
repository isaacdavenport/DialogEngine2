using DialogGenerator.CharacterSelection.Data;
using DialogGenerator.CharacterSelection.Model;
using System;

namespace DialogGenerator.CharacterSelection
{
    public class BLEDataProviderFactory : IBLEDataProviderFactory
    {
        private readonly Func<BLEDataProviderType, IBLEDataProvider> mfactoryFactory;

        public BLEDataProviderFactory(Func<BLEDataProviderType, IBLEDataProvider> _factoryFactory)
        {
            this.mfactoryFactory = _factoryFactory;
        }

        public IBLEDataProvider Create(BLEDataProviderType _selectionType)
        {
            return mfactoryFactory(_selectionType);
        }
    }
}
