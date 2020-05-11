using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodingGame_File_Combiner {
	public partial class Form1 : Form {

		private FolderBrowserDialog openDialog;
		private SaveFileDialog saveDialog;
		private FileSystemWatcher watcher;

		private string openDir;
		private string saveDir;

		private volatile bool filesChanged = false;

		public Form1() {
			InitializeComponent();

			openDialog = new FolderBrowserDialog();
			openDir = Properties.Settings.Default.PreviousDirectory;
			if (!string.IsNullOrEmpty(openDir)) {
				openDialog.SelectedPath = openDir;
				CreateWatcher();
			}
			openDialog.ShowNewFolderButton = true;
			label1.Text = openDir;

			saveDialog = new SaveFileDialog();
			saveDialog.OverwritePrompt = true;
			saveDialog.RestoreDirectory = true;
			saveDir = Properties.Settings.Default.SaveDirectory;
			if (!string.IsNullOrEmpty(saveDir)) {
				saveDialog.FileName = saveDir;
			}
			label2.Text = saveDir;

			lock (this) {
				filesChanged = true;
			}
			timer1.Start();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			timer1.Stop();
			DeleteWatcher();
		}

		private void button1_Click(object sender, EventArgs e) {
			if ((openDialog.ShowDialog() == DialogResult.OK) && !string.IsNullOrWhiteSpace(openDialog.SelectedPath)) {
				openDir = openDialog.SelectedPath;
				Properties.Settings.Default.PreviousDirectory = openDir;
				Properties.Settings.Default.Save();
				label1.Text = openDir;
				CreateWatcher();
				lock (this) {
					filesChanged = true;
				}
			}
		}

		private void DeleteWatcher() {
			try {
				if (watcher != null) {
					watcher.EnableRaisingEvents = false;
					watcher.Changed -= FileWatcher_Event;
					watcher.Created -= FileWatcher_Event;
					watcher.Deleted -= FileWatcher_Event;
					watcher.Dispose();
				}
			} catch (Exception) {
				Console.WriteLine("Error deleting watcher");
			} finally {
				watcher = null;
			}
		}

		private void CreateWatcher() {
			try {
				DeleteWatcher();
				watcher = new FileSystemWatcher(openDir + "\\");
				//watcher.Filter = "*.cs";
				//watcher.NotifyFilter = NotifyFilters.LastWrite;
				watcher.Changed += FileWatcher_Event;
				watcher.Created += FileWatcher_Event;
				watcher.Deleted += FileWatcher_Event;
				watcher.Renamed += FileWatcher_Event;
				watcher.EnableRaisingEvents = true;
			} catch (Exception) {
				Console.WriteLine("Error creating watcher");
				DeleteWatcher();
			}
		}

		private void button2_Click(object sender, EventArgs e) {
			if (saveDialog.ShowDialog() == DialogResult.OK) {
				saveDir = saveDialog.FileName;
				Properties.Settings.Default.SaveDirectory = saveDir;
				Properties.Settings.Default.Save();
				label2.Text = saveDir;
				lock (this) {
					filesChanged = true;
				}
			}
		}

		private void FileWatcher_Event(object sender, FileSystemEventArgs e) {
			if (e.Name.Trim().EndsWith(".cs")) {
				lock (this) {
					filesChanged = true;
				}
			}
		}

		private void button3_Click(object sender, EventArgs e) {
			lock (this) {
				filesChanged = true;
			}
		}

		private void timer1_Tick(object sender, EventArgs e) {
			bool update = false;
			lock (this) {
				if (filesChanged) {
					update = true;
					filesChanged = false;
				}
			}
			if (update) UpdateFile();
		}

		private void UpdateFile() {
			try {
				FileInfo file = new FileInfo(saveDir);
				if (Directory.Exists(openDir)) {
					using (StreamWriter writer = new StreamWriter(file.FullName, false)) {
						string[] files = Directory.GetFiles(openDir);
						if (!CombineFiles(files, writer)) throw new Exception("Error while combining.");
					}
				}
			} catch (Exception ex) {
				//if (sender != this) {
				Console.WriteLine("ERROR: {0}", ex.Message);
				MessageBox.Show("Unable to combine or save files!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//}
			}
		}

		private static bool CombineFiles(string[] files, StreamWriter output) {
			try {
				List<string> usings = new List<string>();
				List<string> sources = new List<string>();
				foreach (string file in files.Where(x => x.Trim().EndsWith(".cs"))) {
					using (StreamReader reader = new StreamReader(File.OpenRead(file))) {
						if (!GetInfo(reader, usings, sources)) {
							return false;
						}
					}
				}

				foreach (string u in usings) {
					output.WriteLine(u);
				}
				output.WriteLine();
				foreach (string source in sources) {
					output.WriteLine(source);
				}
				return true;
			} catch (Exception) {
				return false;
			}
		}

		private static bool GetInfo(StreamReader reader, List<string> usings, List<string> sources) {
			string source = reader.ReadToEnd();
			foreach (string line in source.Split('\n')) {
				string str = line.Trim();
				if (str.StartsWith("using")) {
					if (!usings.Contains(str)) {
						usings.Add(str.TrimEnd());
					}
				} else {
					sources.Add(line.TrimEnd());
				}
			}
			/*
						int index = source.IndexOf("namespace");
						int bracket1 = source.IndexOf('{', index);
						int bracket2 = source.LastIndexOf('}');
						if((bracket1 > index) && (bracket2 > bracket1)) {
							source = source.Substring(bracket1 + 1, bracket2 - bracket1 - 1);

						}
						*/
			return true;
		}

	}
}
