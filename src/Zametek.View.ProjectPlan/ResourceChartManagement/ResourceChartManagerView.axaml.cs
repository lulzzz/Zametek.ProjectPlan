using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OxyPlot;
using OxyPlot.Avalonia;

namespace Zametek.View.ProjectPlan
{
    public partial class ResourceChartManagerView
        : UserControl
    {
        public ResourceChartManagerView()
        {
            InitializeComponent();
            var oxyplot = this.FindControl<PlotView>("oxyplot");
            oxyplot.ActualController.UnbindMouseDown(OxyMouseButton.Right);
            oxyplot.ActualController.UnbindMouseWheel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
