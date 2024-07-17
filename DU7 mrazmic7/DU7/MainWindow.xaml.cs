using DU7.Model;
using DU7.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static DU7.Model.DataRetriever;
using static DU7.ViewModels.CurrencyManagerViewModel;

namespace DU7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CurrencyManagerViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            viewModel = (CurrencyManagerViewModel)Application.Current.TryFindResource("ViewModel");

            viewModel.ModelUpdated += Model_ModelUpdated; // register event listener // multiple event listeners can be registered this way
        }

        private void Model_ModelUpdated(DataForGraphDisplay data) // the delegate definiton in ViewModels decides the parameter type
        {
            // Plot data on the graph

            if (!data.shouldDraw)
            {
                return;
            }

            if (data.drawEmpty)
            {
                linegraph.Description = data.description;
                linegraph.Plot(new List<int>(), new List<double>());
                return;
            }

            linegraph.Description = data.description;
            linegraph.Plot(data.x, data.y);
        }


    }
}
