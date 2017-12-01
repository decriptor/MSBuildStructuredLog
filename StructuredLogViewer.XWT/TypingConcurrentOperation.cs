﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using TPLTask = System.Threading.Tasks.Task;
using System.Threading.Tasks;

namespace StructuredLogViewer
{
    public class TypingConcurrentOperation
    {
        public event Action<object> DisplayResults;
        public Func<string, object> ExecuteSearch;

        public event Action<string, object> SearchComplete;

        public const int ThrottlingDelayMilliseconds = 300;

        public void Reset()
        {
            latestSearch = null;
        }

        private string latestSearch;

        public void TextChanged(string searchText)
        {
            latestSearch = searchText;
			TPLTask.Delay(ThrottlingDelayMilliseconds).ContinueWith(_ =>
            {
                if (latestSearch == searchText)
                {
					StartOperation(searchText);
                }
            });
        }

        // FIXME, 
        private void StartOperation(string searchText)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var results = ExecuteSearch(searchText);
            var elapsed = sw.Elapsed;
            //BuildControl.Elapsed = elapsed;
            if (latestSearch == searchText)
            {
                //Application.Current.Dispatcher.InvokeAsync(() =>
                //{
                    DisplayResults?.Invoke(results);
                    SearchComplete?.Invoke(searchText, results);
                //});
            }
        }
    }
}
