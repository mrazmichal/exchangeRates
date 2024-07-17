using DU7.Support;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using DU7.ViewModels;

namespace DU7.Model
{
    // Retrieve and cache data
    public class DataRetriever
    {
        public class CurrencyRatesHistory
        {
            public List<KeyValuePair<DateTime, double>> data;
            public DateTime retrievalDate;
            public string currency;
            public string currencyBase;
        }

        Dictionary<string, Dictionary<string, CurrencyRatesHistory>> dictionary; // Currency -> CurrencyBase -> CurrencyRatesHistory

        public DataRetriever() {
         
        }

        public async Task<CurrencyRatesHistory> getHistoricalRatesAsync(string currency, string baseCurrency)
        {
            if (currency == baseCurrency)
            {
                return new CurrencyRatesHistory() { currency = currency, currencyBase = baseCurrency, retrievalDate = DateTime.Now.Date };
            }

            // Try to return cached data if they exist and are fresh enough
            try
            {
                dictionary.TryGetValue(currency, out Dictionary<string, CurrencyRatesHistory> dict);
                dict.TryGetValue(baseCurrency, out CurrencyRatesHistory history);
                // return the cached data only if they were retrieved today
                if (history.retrievalDate.Date == DateTime.Now.Date)
                {
                    if (history.data != null) // unnecessary?
                    {
                        return history;
                    }
                }
            }
            catch
            {

            }

            // 5 years period ending now
            DateTime end = DateTime.Now.Date;
            DateTime start = end.AddYears(-5).Date;

            // Download
            // Could throw error - will bubble up
            JObject json = await downloadHistoricalRatesAsync(new List<string> { currency }, baseCurrency, start, end);

            // Transform the data from json to pairs (date, rate)
            List<KeyValuePair<DateTime, double>> data = transformHistoricalRatesJson(json);

            // Save it
            dictionary = dictionary ?? new Dictionary<string, Dictionary<string, CurrencyRatesHistory>>();
            if (!dictionary.ContainsKey(currency))
            {
                dictionary.Add(currency, new Dictionary<string, CurrencyRatesHistory>());
            }
            dictionary.TryGetValue(currency, out Dictionary<string, CurrencyRatesHistory> dict2);
            CurrencyRatesHistory history2 = new CurrencyRatesHistory() { data = data, retrievalDate = DateTime.Now.Date, currencyBase = baseCurrency, currency = currency };
            dict2.Add(baseCurrency, history2);

            return history2;
        }

        class Sorter : IComparer<KeyValuePair<DateTime, double>>
        {
            public int Compare(KeyValuePair<DateTime, double> x, KeyValuePair<DateTime, double> y)
            {
                DateTime xDate = x.Key;
                DateTime yDate = y.Key;

                // CompareTo() method 
                return xDate.CompareTo(yDate);
            }
        }

        private List<KeyValuePair<DateTime, double>> transformHistoricalRatesJson(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            JObject rates = (JObject)json["rates"];
            var props = rates.Properties().ToList();

            // Construct list of pairs (date, rate)
            List<KeyValuePair<DateTime, double>> list = new List<KeyValuePair<DateTime, double>>();
            foreach (JProperty prop in props)
            {
                string dateString = prop.Name;

                // The navigation in JSON is weird but this works...
                JToken tmp = prop.First.First.First;
                double rate = tmp.Value<double>();

                list.Add(new KeyValuePair<DateTime, double>(DateTime.ParseExact(dateString, "yyyy-MM-dd", null), rate));
            }

            // Sort it
            Sorter sorter = new Sorter();
            list.Sort(sorter);

            return list;
        }

        public async Task<JObject> downloadHistoricalRatesAsync(List<String> currencies, String baseCurrency, DateTime start, DateTime end)
        {
            String symbols = createStringFromCurrencies(currencies, baseCurrency);

            if (symbols.Length <= 0)
            {
                return null;
            }

            String startFormatted = formatDateTime(start);
            String endFormatted = formatDateTime(end);

            String url = @"https://api.exchangeratesapi.io/history?" +
                "symbols=" + symbols +
                "&base=" + baseCurrency +
                "&start_at=" + startFormatted +
                "&end_at=" + endFormatted;

            JObject json = await downloadJsonAsync(url);

            return json;
        }

        public async Task<JObject> downloadLatestRatesAsync(List<String> currencies, String baseCurrency)
        {
            String symbols = createStringFromCurrencies(currencies, baseCurrency);

            String url = @"https://api.exchangeratesapi.io/latest?" +
                "symbols=" + symbols +
                "&base=" + baseCurrency;

            // Can throw exception
            JObject json = await downloadJsonAsync(url);

            return json;
        }

        /// <summary>
        /// Download JSON from url and convert it to JObject
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Returns JSON JObject or throws error</returns>
        private async Task<JObject> downloadJsonAsync(string url)
        {
            JObject o;

            using (var httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync(url);

                // Now parse
                o = JObject.Parse(json);
            }

            return o;
        }

        private string formatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }


        private string createStringFromCurrencies(List<string> currencies, string baseCurrency)
        {
            String symbols = "";
            foreach (string currency in currencies)
            {
                if (symbols.Length > 0)
                {
                    symbols = symbols + ",";
                }
                if (currency != baseCurrency)
                {
                    symbols = symbols + currency;
                }
            }
            return symbols;
        }
    }
}

