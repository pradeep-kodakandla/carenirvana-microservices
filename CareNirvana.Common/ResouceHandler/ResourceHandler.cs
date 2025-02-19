using System.Reflection;

namespace iCare4H.Common
{
    public class ResourceHandler(Assembly resourceAssembly)
    {
        private readonly Assembly resourceAssembly = resourceAssembly;

        public string AsString(string resourceName)
        {
            var input = resourceAssembly.GetManifestResourceStream(resourceName);
            if (input == null)
            {
                return string.Empty;
            }
            using var sr = new StreamReader(input);
            return sr.ReadToEnd();
        }
    }
}
