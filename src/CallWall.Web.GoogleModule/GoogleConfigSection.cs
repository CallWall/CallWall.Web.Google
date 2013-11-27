using System;
using System.Configuration;

namespace CallWall.Web.GoogleModule
{
    public sealed class GoogleConfigSection : ConfigurationSection
    {
        private const string InvalidConfigMessage = @"The Google module configuration is invalid. A ClientId and ClientSecret must be provided to the GoogleConfig section. 
i.e. 
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""GoogleConfig"" type=""CallWall.Web.GoogleModule.GoogleConfigSection, CallWall.Web.GoogleModule"" />
    </configSections>

    <GoogleConfig 
        clientId=""123456789000-abc123abc123abc123abc123abc123xxs.apps.googleusercontent.com""
        clientSecret=""randomlettersgohere""
        />
</configuration>";

        public static GoogleConfigSection GetConfig()
        {
            try
            {
                return ConfigurationManager.GetSection("GoogleConfig") as GoogleConfigSection;
            }
            catch (Exception innerException)
            {
                throw new ConfigurationErrorsException(InvalidConfigMessage, innerException);
            }
        }

        [ConfigurationProperty("clientId", IsRequired = true)]
        public string ClientId
        {
            get { return (string)base["clientId"]; }
        }

        [ConfigurationProperty("clientSecret", IsRequired = true)]
        public string ClientSecret
        {
            get { return (string)base["clientSecret"]; }
        }

        public static void EnsureIsValid()
        {
            var config = GetConfig();
            if (config == null
                || string.IsNullOrWhiteSpace(config.ClientId)
                || string.IsNullOrWhiteSpace(config.ClientSecret))

                throw new ConfigurationErrorsException(InvalidConfigMessage);
        }
    }
}
