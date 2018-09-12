using Autofac;

namespace DialogGenerator.Core
{
    public class CoreModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Logger>().As<ILogger>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
