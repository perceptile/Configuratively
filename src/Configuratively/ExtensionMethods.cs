using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configuratively
{
    public static class ExtensionMethods
    {
        public static void Upsert(this IDictionary<string, object> source, string key, object value)
        {
            if (source.ContainsKey(key))
            {
                source[key] = value;
            }
            else
            {
                source.Add(key, value);
            }
        }
    }
}
