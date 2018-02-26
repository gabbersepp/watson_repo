using System.Collections.Generic;

namespace WebApplication1
{
    public class StaticStorage
    {
        private static Dictionary<string, object> dict = new Dictionary<string, object>();
        public static void AddData(string sessionId, object data)
        {
            dict[sessionId] = data;
        }

        public static T GetData<T>(string sessionId)
        {
            object data;
            dict.TryGetValue(sessionId, out data);
            return (T) data;
        }
    }
}