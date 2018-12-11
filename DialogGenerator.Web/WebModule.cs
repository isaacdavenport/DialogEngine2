using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace DialogGenerator.Web
{
    public class WebModule : IModule
    {
        private IUnityContainer mContainer;

        public WebModule(IUnityContainer container)
        {
            mContainer = container;
        }

        public void Initialize()
        {
            mContainer.RegisterType<IContentProvider, GoogleDriveContentProvider>();
        }
    }
}
