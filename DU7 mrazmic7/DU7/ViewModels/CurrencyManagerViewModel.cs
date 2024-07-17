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
using DU7.Model;
using static DU7.Model.DataRetriever;

namespace DU7.ViewModels
{
    class CurrencyManagerViewModel : ViewModelBase
    {
        public event ModelUpdatedDelegate ModelUpdated; // delegate is reference to the listening function, ModelUpdated is the event name
        public delegate void ModelUpdatedDelegate(DataForGraphDisplay data); // function header for the function located in view

        private RelayCommand _addCommand;
        private RelayCommand _deleteCommand;
        private RelayCommand _chooseBaseCommand;
        private CurrencyItemViewModel _selectedCurrencyDetail;
        private String _selectedAvailableCurrency = "";
        private CurrencyItemViewModel _baseCurrency;

        private Model.DataRetriever model;

        // Data use for displaying currency graph
        public class DataForGraphDisplay
        {
            public List<int> x = new List<int>();
            public List<double> y = new List<double>();
            public bool shouldDraw = true;
            public bool drawEmpty = false;
            public string description = "";
        }

        public ObservableCollection<CurrencyItemViewModel> CurrencyList { get; set; } = new ObservableCollection<CurrencyItemViewModel>();
        public CurrencyManagerViewModel()
        {
            model = model ?? new Model.DataRetriever();

            // Add default currencies
            CurrencyList.Add(new CurrencyItemViewModel() { CurrencyName = "EUR", Retriever = model });
            CurrencyList.Add(new CurrencyItemViewModel() { CurrencyName = "USD", Retriever = model });

            // Chose the base currency
            BaseCurrency = CurrencyList[0];
        }

        // Detail of the selected currency
        public CurrencyItemViewModel SelectedCurrencyDetail
        {
            get
            {
                return _selectedCurrencyDetail;
            }
            set
            {
                _selectedCurrencyDetail = value;
                OnPropertyChanged("SelectedCurrencyDetail");
                refreshGraphData();
            }
        }

        // Tell the view we have data for graph plotting ready
        internal void createEventModelUpdated(DataRetriever.CurrencyRatesHistory data)
        {
            DataForGraphDisplay prepared = prepareDataForGraphDisplay(data);
            ModelUpdated?.Invoke(prepared); // The delegate listener is launched if there's any
        }

        public CurrencyItemViewModel BaseCurrency
        {
            get
            {
                return _baseCurrency;
            }
            set
            {
                _baseCurrency = value;
                OnPropertyChanged("BaseCurrency");

                refreshGraphData();
            }
        }

        // We should refresh the graph because it's no longer relevant
        private async void refreshGraphData()
        {
            model = model ?? new Model.DataRetriever();

            if (SelectedCurrencyDetail != null)
            {
                CurrencyRatesHistory data = await SelectedCurrencyDetail.getCurrencyHistoricalRates(_baseCurrency.CurrencyName);
                createEventModelUpdated(data);
            } else
            {
                createEventModelUpdated(null);
            }

        }

        public ObservableCollection<String> AvailableCurrencies { get; set; } = new ObservableCollection<String>()
        {
            "CAD", "HKD", "PHP", "DKK", "HUF", "CZK", "AUD", "RON", "SEK", "IDR", "INR", "BRL", "RUB", "HRK", "JPY", "THB", "CHF", "SGD", "PLN", "BGN", "TRY", "CNY", "NOK", "NZD", "ZAR", "USD", "MXN", "ILS", "GBP", "KRW", "EUR"
        };
        
        public String SelectedAvailableCurrency
        {
            get
            {
                return _selectedAvailableCurrency;
            }
            set
            {
                _selectedAvailableCurrency = value;
                OnPropertyChanged("SelectedAvailableCurrency");
            }
        }

        public RelayCommand AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new RelayCommand(AddCurrencyItem, AddCurrencyItemCanExecute)); // delegati - odkazy na metodu
            }
        }

        private bool AddCurrencyItemCanExecute(object obj)
        {
            if (CurrencyListContains(SelectedAvailableCurrency))
            {
                return false;
            }
            if (SelectedAvailableCurrency.Length <= 0)
            {
                return false;
            }
            return true;
        }

        private bool CurrencyListContains(string currency)
        {
            foreach (CurrencyItemViewModel item in CurrencyList)
            {
                if (item.CurrencyName == currency)
                {
                    return true;
                }
            }
            return false;
        }

        private void AddCurrencyItem(object obj)
        {
            model = model ?? new Model.DataRetriever();
            CurrencyList.Add(new CurrencyItemViewModel() { CurrencyName = SelectedAvailableCurrency, Retriever = model });
        }

        public RelayCommand DeleteCommand
        {
            get { return _deleteCommand ?? (_deleteCommand = new RelayCommand(DeleteCurrencyItem, DeleteCommandCanExecute)); } // pokud _deleteCommand je null, vrati to vlevo // It's the null coalescing operator
        }

        private bool DeleteCommandCanExecute(object obj)
        {
            if (SelectedCurrencyDetail == null) { return false; }
            return true;
        }

        private void DeleteCurrencyItem(object obj)
        {
            CurrencyList.Remove(SelectedCurrencyDetail);
            SelectedCurrencyDetail = null;
            refreshGraphData();
        }

        public RelayCommand ChooseBaseCommand
        {
            get { return _chooseBaseCommand ?? (_chooseBaseCommand = new RelayCommand(ChooseBase, ChooseBaseCanExecute)); } // pokud _deleteCommand je null, vrati to vlevo // It's the null coalescing operator
        }

        private bool ChooseBaseCanExecute(object obj)
        {
            if (SelectedCurrencyDetail == null) 
            { 
                return false; 
            }
            if (SelectedCurrencyDetail.CurrencyName == BaseCurrency.CurrencyName)
            {
                return false;
            }

            return true;
        }

        private void ChooseBase(object obj)
        {
            BaseCurrency = SelectedCurrencyDetail;
        }

        private DataForGraphDisplay prepareDataForGraphDisplay(CurrencyRatesHistory data)
        {
            // No currency selected
            if (SelectedCurrencyDetail == null || data == null)
            {
                return new DataForGraphDisplay() { drawEmpty = true, description = "" };
            }

            string currencyBase = BaseCurrency.CurrencyName;
            string currency = SelectedCurrencyDetail.CurrencyName;            

            // Is the data still relevant? Didn't the user click something else before we managed to get the data?
            if (data.currency != currency || data.currencyBase != currencyBase)
            {
                return new DataForGraphDisplay() { shouldDraw = false };
            }

            // placeholder dates - one year period
            DateTime end = DateTime.Now.Date;
            DateTime start = end.AddYears(-1).Date;

            List<KeyValuePair<DateTime, double>> pairs = data.data;

            // Same currency as base
            if (data.currency == data.currencyBase)
            {
                return new DataForGraphDisplay() { 
                    drawEmpty = true, 
                    description = "" + currency + " se základem " + currencyBase 
                };
            }

            // Get the pairs that are between start and end dates
            List<KeyValuePair<DateTime, double>> pairsForDisplay = new List<KeyValuePair<DateTime, double>>();
            foreach (KeyValuePair<DateTime, double> pair in pairs)
            {
                if (pair.Key.Date >= start.Date && pair.Key.Date <= end.Date)
                {
                    pairsForDisplay.Add(pair);
                }
            }

            List<DateTime> days = new List<DateTime>();
            List<double> rates = new List<double>();
            foreach (KeyValuePair<DateTime, double> pair in pairsForDisplay)
            {
                days.Add(pair.Key.Date);
                rates.Add(pair.Value);
            }

            var x = days;
            var y = rates;

            // Couldn't get the graph to display dates so we display them as numbers (most recent day has number 0)
            var xDayNumbers = new List<int>();
            foreach (DateTime date in x)
            {   
                // Convert to number
                long ticks = date.Ticks - end.Date.Ticks;
                // Number of ticks in a day
                long dayTicks = ((long)10000000) * 3600 * 24;
                int ticksAsDays = (int)(ticks / dayTicks);
                xDayNumbers.Add(ticksAsDays);
            }

            return new DataForGraphDisplay()
            {
                x = xDayNumbers,
                y = y,
                description = "" + currency + " se základem " + currencyBase
            };


        }
    }
}

