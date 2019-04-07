using System.Collections.Generic;

namespace ApptilausSDK {

    public class JsonDictionaryWriter {

        /// <summary>
        /// Serializes a string dictionary to a nicely formatted json
        /// </summary>
        /// <param name="dictionary">Dictionary to serialize</param>
        /// <returns>Json string</returns>
        public static string Serialize(Dictionary<string, string> dictionary) {
            if (dictionary == null || dictionary.Count == 0) {
                return "{}";
            }
            string result = "{\n";
            int counter = 0;
            foreach (KeyValuePair<string,string> param in dictionary) {
                result += "\t\"" + param.Key + "\": \""+ param.Value + "\"";
                counter++;
                if (counter < dictionary.Count) {
                    result += ",\n";
                }
            }
            result += "\n}";
            return result;
        }
    }
}
