using Autofac;

namespace DialogGenerator.Utilities
{
    public class UtilitiesModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MP3Player>().As<IMP3Player>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
