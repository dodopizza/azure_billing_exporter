using System.Collections.Generic;
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
        public List<string> ColumnNames { get; } = new List<string>();
        public List<string> Values { get;  } = new List<string>();
        
        public static CostResultRows Cast(dynamic columns, dynamic singleRow)
        {
            var parsedRow = new CostResultRows();
            foreach (var val in JArray.Parse(singleRow.ToString()))
            {
                parsedRow.Values.Add(val.ToString());
            }

            foreach (var column in columns)
            {
                parsedRow.ColumnNames.Add(column.name.ToString());
            }

            return parsedRow;
        }
    }
}