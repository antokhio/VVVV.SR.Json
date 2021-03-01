using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Lib;
using VVVV.PluginInterfaces.V2;

namespace VVVV.SR.JSON
{
    [PluginInfo(Name = "S", Category = "String", Version = "JSON", Author = "antokhio", Help = "JSON desirializer to S Advanced", AutoEvaluate = true)]
    public class SJson : IPluginEvaluate, IDisposable
    {
        [Input("Input", StringType = StringType.Filename, IsSingle = true)]
        IDiffSpread<string> FIn;

        [Input("Apply", IsBang = true, IsSingle = true)]
        ISpread<bool> FInApply;

        [Output("Keys")]
        ISpread<string> FOutKeys;

        [Output("Values")]
        ISpread<string> FOutValues;


        private StringDataHolder stringDataHolder = StringDataHolder.Instance;

        public void Evaluate(int SpreadMax)
        {
            if (FInApply[0])
            {
                if (FIn.Count > 0 && FIn[0] != "" && File.Exists(FIn[0]))
                {
                    try
                    {
                        var sr = new StreamReader(FIn[0]);
                        string json = sr.ReadToEnd();
                        sr.Close();

                        //https://stackoverflow.com/questions/32782937/generically-flatten-json-using-c-sharp
                        var data = DeserializeAndFlatten(json);

                        int i = 0;

                        FOutKeys.SliceCount = data.Count;
                        FOutValues.SliceCount = data.Count;

                        foreach (var prop in data)
                        {
                            FOutKeys[i] = prop.Key;
                            FOutValues[i] = prop.Value.ToString();
                            i++;

                            stringDataHolder.AddInstance(prop.Key);
                            stringDataHolder.UpdateData(prop.Key, new List<string> { prop.Value.ToString() });
                        }

                    }
                    catch
                    {

                    }

                }
            }
        }
        public void Dispose()
        {
            foreach (var key in FOutKeys)
            {
                stringDataHolder.RemoveInstance(key);
            }
        }

        // https://www.bfcamara.com/post/75172803617/flatten-json-object-to-send-within-an-azure-hub
        public static Dictionary<string, object> DeserializeAndFlatten(string json)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "");
            return dict;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }


                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }
        private static string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "/" + name);
        }


    }
}
