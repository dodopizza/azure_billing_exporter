using System.Collections.Generic;

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
    }
}