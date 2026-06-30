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
        private static string? token = null;

        public static string GetToken()
        {
            if (!string.IsNullOrEmpty(token)){
                return token;
            }

            if (!File.Exists(tokenFile))
            {
                throw new Exception(Messages.Get("token_not_found"));
            }

            try
            {
                token = File.ReadAllText(tokenFile);
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception(Messages.Get("token_read_failed"));
                }
                return token;
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
