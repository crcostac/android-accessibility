using Subzy.ViewModels;

namespace Subzy;

public partial class DebugPage : ContentPage
{
    public DebugPage(DebugViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
