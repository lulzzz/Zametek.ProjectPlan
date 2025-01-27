﻿using Avalonia.Controls;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Zametek.Contract.ProjectPlan;

namespace Zametek.ViewModel.ProjectPlan
{
    public class ActivitiesManagerViewModel
        : ToolViewModelBase, IActivitiesManagerViewModel
    {
        #region Fields

        private readonly object m_Lock;

        private readonly ICoreViewModel m_CoreViewModel;
        private readonly IDialogService m_DialogService;

        #endregion

        #region Ctors

        public ActivitiesManagerViewModel(
            ICoreViewModel coreViewModel,//!!,
            IDialogService dialogService)//!!)
        {
            m_Lock = new object();
            m_CoreViewModel = coreViewModel;
            m_DialogService = dialogService;
            SelectedActivities = new ConcurrentDictionary<int, IManagedActivityViewModel>();
            m_HasActivities = false;

            SetSelectedManagedActivitiesCommand = ReactiveCommand.Create<SelectionChangedEventArgs>(SetSelectedManagedActivities);
            AddManagedActivityCommand = ReactiveCommand.CreateFromTask(AddManagedActivityAsync);
            RemoveManagedActivitiesCommand = ReactiveCommand.CreateFromTask(RemoveManagedActivitiesAsync, this.WhenAnyValue(am => am.HasActivities));

            m_IsBusy = this
                .WhenAnyValue(am => am.m_CoreViewModel.IsBusy)
                .ToProperty(this, am => am.IsBusy);

            m_HasStaleOutputs = this
                .WhenAnyValue(am => am.m_CoreViewModel.HasStaleOutputs)
                .ToProperty(this, am => am.HasStaleOutputs);

            m_ShowDates = this
                .WhenAnyValue(am => am.m_CoreViewModel.ShowDates)
                .ToProperty(this, am => am.ShowDates);

            m_HasCompilationErrors = this
                .WhenAnyValue(am => am.m_CoreViewModel.HasCompilationErrors)
                .ToProperty(this, am => am.HasCompilationErrors);

            Id = Resource.ProjectPlan.Titles.Title_ActivitiesView;
            Title = Resource.ProjectPlan.Titles.Title_ActivitiesView;
        }

        #endregion

        #region Properties

        public IDictionary<int, IManagedActivityViewModel> SelectedActivities { get; }

        #endregion

        #region Private Methods

        private void SetSelectedManagedActivities(SelectionChangedEventArgs args)
        {
            lock (m_Lock)
            {
                if (args.AddedItems is not null)
                {
                    foreach (var managedActivityViewModel in args.AddedItems.OfType<ManagedActivityViewModel>())
                    {
                        SelectedActivities.TryAdd(managedActivityViewModel.Id, managedActivityViewModel);
                    }
                }
                if (args.RemovedItems is not null)
                {
                    foreach (var managedActivityViewModel in args.RemovedItems.OfType<ManagedActivityViewModel>())
                    {
                        SelectedActivities.Remove(managedActivityViewModel.Id);
                    }
                }

                HasActivities = SelectedActivities.Any();
            }
        }

        private async Task AddManagedActivityAsync()
        {
            try
            {
                lock (m_Lock)
                {
                    m_CoreViewModel.AddManagedActivity();
                }
                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                await m_DialogService.ShowErrorAsync(
                    Resource.ProjectPlan.Titles.Title_Error,
                    ex.Message);
            }
        }

        private async Task RemoveManagedActivitiesAsync()
        {
            try
            {
                lock (m_Lock)
                {
                    ICollection<int> activityIds = SelectedActivities.Keys;

                    if (!activityIds.Any())
                    {
                        return;
                    }

                    m_CoreViewModel.RemoveManagedActivities(activityIds);
                }
                await RunAutoCompileAsync();
            }
            catch (Exception ex)
            {
                await m_DialogService.ShowErrorAsync(
                    Resource.ProjectPlan.Titles.Title_Error,
                    ex.Message);
            }
        }

        private async Task RunAutoCompileAsync() => await Task.Run(() => m_CoreViewModel.RunAutoCompile());

        #endregion

        #region IActivityManagerViewModel Members

        private readonly ObservableAsPropertyHelper<bool> m_IsBusy;
        public bool IsBusy => m_IsBusy.Value;

        private readonly ObservableAsPropertyHelper<bool> m_HasStaleOutputs;
        public bool HasStaleOutputs => m_HasStaleOutputs.Value;

        private readonly ObservableAsPropertyHelper<bool> m_ShowDates;
        public bool ShowDates => m_ShowDates.Value;

        private readonly ObservableAsPropertyHelper<bool> m_HasCompilationErrors;
        public bool HasCompilationErrors => m_HasCompilationErrors.Value;

        private bool m_HasActivities;
        public bool HasActivities
        {
            get => m_HasActivities;
            set
            {
                lock (m_Lock)
                {
                    m_HasActivities = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public ReadOnlyObservableCollection<IManagedActivityViewModel> Activities => m_CoreViewModel.Activities;

        public ICommand SetSelectedManagedActivitiesCommand { get; }

        public ICommand AddManagedActivityCommand { get; }

        public ICommand RemoveManagedActivitiesCommand { get; }

        #endregion
    }
}
