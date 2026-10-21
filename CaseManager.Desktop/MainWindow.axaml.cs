using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CaseManager.Core.Models;
using CaseManager.Desktop.ViewModels;

namespace CaseManager.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Одинарный - TappedEvent,  двойной - DoubleTappedEvent.
        AddHandler(DoubleTappedEvent, OnDocumentTapped, RoutingStrategies.Bubble);
    }

    private void OnDocumentTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control source) return;

        CaseDocument? doc = null;
        Visual? v = source;
        while (v != null)
        {
            if (v is Control c && c.DataContext is CaseDocument d)
            {
                doc = d;
                break;
            }
            v = v.GetVisualParent();
        }

        if (doc == null) return;

        if (DataContext is MainWindowViewModel vm)
        {
            try { vm.OpenDocument(doc, this); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OpenDocument failed: " + ex);
            }
        }
    }
    private void OnEditDocumentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is CaseDocument doc
            && DataContext is MainWindowViewModel vm)
        {
            vm.EditDocument(doc);
        }
    }

    private void OnDeleteDocumentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is CaseDocument doc
            && DataContext is MainWindowViewModel vm)
        {
            vm.DeleteDocument(doc);
        }
    }


}