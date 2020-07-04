using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureBillingExporter
{
    public class CostResultRows
    {
        public double Cost
        {
            get
            {
                var preTaxCosts = GetByColumnName("PreTaxCost");

                if (string.IsNullOrEmpty(preTaxCosts))
                {
                    return 0;
                }
                
                return double.Parse(preTaxCosts);
            }
        }


        public string Date 
        {
            get { return GetByColumnName("UsageDate"); }
        }

        public string GetByColumnName(string name)
        {
            for (var i = 0; i < ColumnNames.Count; i++)
            {
                if (ColumnNames[i] == name)
                {
                    return Values[i];
                }
            }

            return null;
        }

        public double GetValueByColumnName(string name)
        {
            var strVal = GetByColumnName(name);
            
            if (string.IsNullOrEmpty(strVal))
            {
                return 0;
            }
                
            return double.Parse(strVal);
        }
        public List<string> ColumnNames { get; } = new List<string>();
        public List<string> Values { get;  } = new List<string>();
        
        public static CostResultRows Cast(dynamic columns, dynamic singleRow)
        {
            var parsedRow = new CostResultRows();

            parsedRow.Values.AddRange(ClearParse(singleRow.ToString()));
            
            foreach (var column in columns)
            {
                parsedRow.ColumnNames.Add(column.name.ToString());
            }

            return parsedRow;
        }

        private static IEnumerable<string> ClearParse(dynamic s)
        {
            using JsonReader jsonReader = new JsonTextReader(new StringReader(s));
            jsonReader.DateParseHandling = DateParseHandling.None;

            var array = JArray.Load(jsonReader);

            var res = new List<string>();
            foreach (var item in array) {
                var itemValue = item.Value<string>();
                res.Add(itemValue);
            }

            return res;
        }
    }
}