using System;
using System.Collections.Generic;

using Xwt;
using Microsoft.Build.Logging.StructuredLogger;
using Xwt.Drawing;

namespace StructuredLogViewer.XWT.Controls
{
	public class SearchAndResultsControl : VBox
    {
		SearchTextEntry searchTextEntry;

		ListView list;
		ListStore store;

		DataField<string> name = new DataField<string>();
		DataField<ProxyNode> node = new DataField<ProxyNode>();


		public Func<object, IEnumerable<object>> ResultsTreeBuilder { get; set; }
		public event Action WatermarkDisplayed;

		TypingConcurrentOperation typingConcurrentOperation = new TypingConcurrentOperation();

		public SearchAndResultsControl()
		{
			BuildUI();
			typingConcurrentOperation.DisplayResults += DisplaySearchResults;
			typingConcurrentOperation.SearchComplete += TypingConcurrentOperation_SearchComplete;

			ResultsTreeBuilder = BuildResultTree;
		}

		void TypingConcurrentOperation_SearchComplete(string searchText, object arg2)
		{
			SettingsService.AddRecentSearchText(searchText, discardPrefixes: true);
		}

		void BuildUI ()
		{
			searchTextEntry = new SearchTextEntry();
			searchTextEntry.Changed += SearchTextChanged;
			PackStart(searchTextEntry);

			store = new ListStore(name, node);
			list = new ListView(store) {
				HeadersVisible = false,
				ExpandVertical = true,
			};
			list.DataSource = store;
			list.Columns.Add("Name", name);
			PackStart(list, true);
		}

		public Func<string, object> ExecuteSearch
        {
            get => typingConcurrentOperation.ExecuteSearch;
            set => typingConcurrentOperation.ExecuteSearch = value;
        }

		void SearchTextChanged(object sender, EventArgs e)
        {
			var searchText = searchTextEntry.Text;
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 3)
            {
                typingConcurrentOperation.Reset();
                DisplaySearchResults(null);
                return;
            }

            typingConcurrentOperation.TextChanged(searchText);
        }

        void DisplaySearchResults(object results)
        {
            if (results == null)
            {
                //watermark.Visibility = Visibility.Visible;
                WatermarkDisplayed?.Invoke();
            }
            else
            {
                //watermark.Visibility = Visibility.Collapsed;
            }

			var items = ResultsTreeBuilder(results);
			UpdateListStore(items);
        }

		void UpdateListStore(IEnumerable<object> results)
		{
			store.Clear();

			if (results == null) return;

			foreach (ProxyNode result in results) {
				var r = store.AddRow();
				store.SetValue (r, name, result.ToString());
				store.SetValue (r, node, result);
			}
		}

		//public object WatermarkContent
        //{
        //    get => watermark.Content;

        //    set
        //    {
        //        watermark.Content = value;
        //    }
        //}

		IEnumerable<object> BuildResultTree(object resultsObject)
		{
			var results = resultsObject as IEnumerable<SearchResult>;
			if (results == null) {
				return results;
			}

			var root = new Folder();

			// root.Children.Add(new Message { Text = "Elapsed " + Elapsed.ToString() });

			foreach (var result in results) {
				TreeNode parent = root;

				var parentedNode = result.Node as ParentedNode;
				if (parentedNode != null) {
					var chain = parentedNode.GetParentChain();
					var project = parentedNode.GetNearestParent<Project>();
					if (project != null) {
						var projectProxy = root.GetOrCreateNodeWithName<ProxyNode>(project.Name);
						projectProxy.Original = project;
						if (projectProxy.Highlights.Count == 0) {
							projectProxy.Highlights.Add(project.Name);
						}

						parent = projectProxy;
						parent.IsExpanded = true;
					}
				}

				var proxy = new ProxyNode();
				proxy.Original = result.Node;
				proxy.Populate(result);
				parent.Children.Add(proxy);
			}

			if (!root.HasChildren) {
				root.Children.Add(new Message { Text = "No results found." });
			}

			return root.Children;
		}
    }
}
