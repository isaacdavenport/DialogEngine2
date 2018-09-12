using Autofac;

namespace DialogGenerator.DataAccess
{
    public class DataAccessModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DialogDataRepository>().As<IDialogDataRepository>();
            builder.RegisterType<CharacterRepository>().As<ICharacterRepository>();
            builder.RegisterType<DialogModelRepository>().As<IDialogModelRepository>();
            builder.RegisterType<WizardRepository>().As<IWizardRepository>();

            base.Load(builder);
        }
    }
}
