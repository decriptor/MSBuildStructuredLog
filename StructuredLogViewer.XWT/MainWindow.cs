using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Logging.StructuredLogger;
using Xwt;
using Xwt.Drawing;
using Xwt.Formats;
using System.Diagnostics;
using StructuredLogViewer.XWT.Controls;
using System.Collections;
using System.Linq;

namespace StructuredLogViewer.XWT
{
	public class MainWindow : Window
	{
		const string DefaultTitle = "MSBuild Structured Log Viewer";
		string binlogFile;

		StatusIcon statusIcon;

		public MainWindow()
		{
			Title = DefaultTitle;
			Width = 1000;
			Height = 800;

			try {
				statusIcon = Application.CreateStatusIcon();
				statusIcon.Menu = new Menu();
				statusIcon.Menu.Items.Add(new MenuItem("Test"));
				statusIcon.Image = Image.FromResource(GetType(), "Images/StructuredLogger.ico");
			} catch {
				Console.WriteLine("Status icon could not be shown");
			}
			MainMenu = BuildMenu();
		}

		Menu BuildMenu()
		{
			Menu menu = new Menu();
			var file = new MenuItem("_File") {
				SubMenu = new Menu()
			};

			var startPageMenuItem = new MenuItem("Start Page");
			var buildSolutionProjectMenuItem = new MenuItem("_Build Solution/Project...");
			var rebuildSolutionProjectMenuItem = new MenuItem("Rebuild Solution/Project...");
			var openLogMenuItem = new MenuItem("_Open Log...");
			openLogMenuItem.Clicked += (s, e) => {
				var fileDialog = new OpenFileDialog("Find a binlog file") {
					Multiselect = false
				};

				if (fileDialog.Run(this))
					binlogFile = fileDialog.FileName;

				if (binlogFile != null)
					OpenLogFile(binlogFile);
			};
			var reloadMenuItem = new MenuItem("ReloadMenu");
			var saveAsMenuItem = new MenuItem("_Save Log As...");
			var recentProjectsMenuItem = new MenuItem("Recent Projects");
			var recentLogsMenuItem = new MenuItem("Recent Logs");
			var setMSBuildPathMenuItem = new MenuItem("Set _MSBuild Path");
			var exitMenuItem = new MenuItem("E_xit");
			exitMenuItem.Clicked += (s, e) => Application.Exit();

			file.SubMenu.Items.Add(startPageMenuItem);
			file.SubMenu.Items.Add(buildSolutionProjectMenuItem);
			file.SubMenu.Items.Add(rebuildSolutionProjectMenuItem);
			file.SubMenu.Items.Add(openLogMenuItem);
			file.SubMenu.Items.Add(reloadMenuItem);
			file.SubMenu.Items.Add(saveAsMenuItem);
			file.SubMenu.Items.Add(recentProjectsMenuItem);
			file.SubMenu.Items.Add(recentLogsMenuItem);
			file.SubMenu.Items.Add(setMSBuildPathMenuItem);
			file.SubMenu.Items.Add(exitMenuItem);
			menu.Items.Add(file);

			var help = new MenuItem("_Help") {
				SubMenu = new Menu()
			};
			var projectHome = new MenuItem("Project home");
			projectHome.Clicked += ProjectHomeClicked;
			help.SubMenu.Items.Add(projectHome);
			menu.Items.Add(help);

			return menu;
		}

		void ProjectHomeClicked(Object sender, EventArgs e)
		{
			Process.Start ("https://github.com/KirillOsenkov/MSBuildStructuredLog");
		}

		async void OpenLogFile(string filePath = "msbuild.binlog")
		{
			if (!File.Exists(filePath)) {
				return;
			}

			//DisplayBuild(null);
			//this.logFilePath = filePath;
			SettingsService.AddRecentLogFile(filePath);
			//UpdateRecentItemsMenu();
			Title = filePath + " - " + DefaultTitle;

			var progress = new BuildProgress();
			progress.ProgressText = "Opening " + filePath + "...";
			//SetContent(progress);

			bool shouldAnalyze = true;

			var currentBuild = await System.Threading.Tasks.Task.Run(() => {
				try {
					return Serialization.Read(filePath);
				} catch (Exception ex) {
					ex = ExceptionHandler.Unwrap(ex);
					shouldAnalyze = false;
					return GetErrorBuild(filePath, ex.ToString());
				}
			});

			if (currentBuild == null) {
				currentBuild = GetErrorBuild(filePath, "");
				shouldAnalyze = false;
			}

			if (shouldAnalyze) {
				progress.ProgressText = "Analyzing " + filePath + "...";
				await System.Threading.Tasks.Task.Run(() => BuildAnalyzer.AnalyzeBuild(currentBuild));
			}

			progress.ProgressText = "Rendering tree...";
			//await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded); // let the progress message be rendered before we block the UI again

			//DisplayBuild(build);
			Content = new BuildControl(currentBuild, filePath);
		}



		static Build GetErrorBuild(string filePath, string message)
		{
			var build = new Build() { Succeeded = false };
			build.AddChild(new Error() { Text = "Error when opening file: " + filePath });
			build.AddChild(new Error() { Text = message });
			return build;
		}

		//void BuildTreeViewSelectionChanged(Object sender, EventArgs e)
		//{
		//	if (buildTreeView.SelectedRow == null)
		//		return;

		//	var node = buildTreeStore.GetNavigatorAt(buildTreeView.SelectedRow).GetValue(baseNode);
		//	textView.LoadText(sender.ToString(), TextFormat.Plain);
		//}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (statusIcon != null)
				statusIcon.Dispose();
		}
	}
}
