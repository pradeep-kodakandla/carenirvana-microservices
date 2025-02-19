using System.Reflection;
using iCare4H.Common;

namespace iCare4H.DataAccess
{
    public class ScriptsHelper
    {
        public static string GetScript(string scriptName)
        {
            var resourceHandler = new ResourceHandler(Assembly.GetExecutingAssembly());
            return resourceHandler.AsString(scriptName);
        }

        public class Scripts
        {
            public static readonly string GetUserScript = "iCare4H.DataAccess.Scripts.Postgres.xxxx.sql";
        }
    }
}
