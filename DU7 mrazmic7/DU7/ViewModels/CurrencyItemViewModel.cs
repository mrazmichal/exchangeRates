using DU7.Model;
using DU7.Support;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using static DU7.Model.DataRetriever;

namespace DU7.ViewModels
{
    internal class CurrencyItemViewModel : ViewModelBase
    {
        

        private string _currencyName;

        public CurrencyItemViewModel()
        {

        }

        
        public string CurrencyName
        {
            get
            {
                return _currencyName;
            }
            set
            {
                _currencyName = value;
                OnPropertyChanged("CurrencyName");
            }
        }

        public string CurrencyBase { get; set; }
        public DataRetriever Retriever { get; set; }
        

        internal async Task<CurrencyRatesHistory> getCurrencyHistoricalRates(string currencyBase)
        {
            if (Retriever == null)
            {
                return null;
            }

            CurrencyRatesHistory data;
            try
            {
                data = await Retriever.getHistoricalRatesAsync(CurrencyName, currencyBase);
            }
            catch
            {
                MessageBox.Show("Couldn't get historical rates data.");
                return null;
            }

            return data;

        }
    }


}