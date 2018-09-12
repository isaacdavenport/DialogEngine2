using Autofac;

namespace DialogGenerator.DialogEngine
{
    public class DialogEngineModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DialogEngine>().As<IDialogEngine>()
                .SingleInstance();
            base.Load(builder);
        }
    }
}
