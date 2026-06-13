using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class Token
    {
        private static readonly string tokenFile = Path.Combine(AppContext.BaseDirectory, "bot_token.txt");
        private static string token = null;

        public static string GetToken()
        {
            if (!string.IsNullOrEmpty(token)){
                return token;
            }

            if (!File.Exists(tokenFile))
            {
                throw new FileNotFoundException($"{tokenFile} not found. Make sure to locate bot_token.txt file on the game directory..");
            }

            try
            {
                token = File.ReadAllText(tokenFile);
                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException("Failed to read bot_token.txt file..");
                }
                return token;
            }
            catch(Exception e)
            {
                throw new IOException("Unexpected error has been occured..", e);
            }
        }
    }
}
