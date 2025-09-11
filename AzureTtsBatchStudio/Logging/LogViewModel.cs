using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AzureTtsBatchStudio.Logging
{
    public partial class LogViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LogEvent> _logEvents = new();

        [ObservableProperty]
        private LogLevel _minimumLevel = LogLevel.Information;

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private bool _autoScroll = true;

        public void AddLogEvent(LogEvent logEvent)
        {
            if (logEvent.Level >= MinimumLevel)
            {
                LogEvents.Add(logEvent);
                
                // Keep only last 1000 entries to prevent memory issues
                while (LogEvents.Count > 1000)
                {
                    LogEvents.RemoveAt(0);
                }
            }
        }

        [RelayCommand]
        public void ClearLogs()
        {
            LogEvents.Clear();
        }

        public ObservableCollection<LogEvent> FilteredLogs
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchFilter))
                    return LogEvents;

                var filtered = LogEvents.Where(log => 
                    log.Message.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    log.Category.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return new ObservableCollection<LogEvent>(filtered);
            }
        }

        partial void OnSearchFilterChanged(string value)
        {
            OnPropertyChanged(nameof(FilteredLogs));
        }
    }
}