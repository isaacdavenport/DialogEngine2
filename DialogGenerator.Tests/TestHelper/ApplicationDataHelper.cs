using System;
using System.IO;

namespace DialogGenerator.Tests.TestHelper
{
    public static class ApplicationDataHelper
    {
        public static string BaseDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,"TestData");
        public static string DataDirectory = Path.Combine(BaseDirectory,"Data");
    }
}
