using System.Configuration;

namespace Panteon.Host.Infrastructure
{
    internal class Config
    {
        public static readonly string JobsFolderName = ConfigurationManager.AppSettings.Get("PANTEON_JOBS_FOLDER");
        public static readonly string ApiStartUrl = ConfigurationManager.AppSettings.Get("PANTEON_REST_API_URL");
    }
}