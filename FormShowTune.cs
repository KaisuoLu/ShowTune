using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using AudioUtils;

namespace ShowWaveForm
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class FormShowTune : System.Windows.Forms.Form
	{
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem file;
        private System.Windows.Forms.MenuItem fileOpen;
        private IContainer components;
		
		WaveFile wave;
		StatusBarPanel sbpMainPanel;
        private MenuItem fileSave;
        private MenuItem fileSaveAs;
        private MenuItem menuItem1;
        private MenuItem menuItemSettingsDrawTune;
        private MenuItem menuItem2;
        private MenuItem menuItem3;
		bool m_DrawWave = true;

		public FormShowTune()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			
			BackColor = SystemColors.Window;
			ForeColor = SystemColors.WindowText;
			ResizeRedraw = true;

			StatusBar sb = new StatusBar();
			sb.Parent = this;
			sb.ShowPanels = true;
			
			sbpMainPanel = new StatusBarPanel( );
			sbpMainPanel.Text = "Ready";
			sbpMainPanel.AutoSize = StatusBarPanelAutoSize.Spring;

			sb.Panels.Add( sbpMainPanel );
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.file = new System.Windows.Forms.MenuItem();
            this.fileOpen = new System.Windows.Forms.MenuItem();
            this.fileSave = new System.Windows.Forms.MenuItem();
            this.fileSaveAs = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemSettingsDrawTune = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.file,
            this.menuItem2,
            this.menuItem3,
            this.menuItem1});
            // 
            // file
            // 
            this.file.Index = 0;
            this.file.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.fileOpen,
            this.fileSave,
            this.fileSaveAs});
            this.file.Text = "&File";
            // 
            // fileOpen
            // 
            this.fileOpen.Index = 0;
            this.fileOpen.Text = "&Open";
            this.fileOpen.Click += new System.EventHandler(this.fileOpen_Click);
            // 
            // fileSave
            // 
            this.fileSave.Index = 1;
            this.fileSave.Text = "&Save ";
            // 
            // fileSaveAs
            // 
            this.fileSaveAs.Index = 2;
            this.fileSaveAs.Text = "Save &As";
            this.fileSaveAs.Click += new System.EventHandler(this.fileSaveAs_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 3;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemSettingsDrawTune});
            this.menuItem1.Text = "&Settings";
            // 
            // menuItemSettingsDrawTune
            // 
            this.menuItemSettingsDrawTune.Checked = true;
            this.menuItemSettingsDrawTune.Index = 0;
            this.menuItemSettingsDrawTune.Text = "Draw Tune (On/Off)";
            this.menuItemSettingsDrawTune.Click += new System.EventHandler(this.menuItemSettingsDrawTune_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "&Record";
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "&Edit";
            // 
            // FormShowTune
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(896, 437);
            this.Menu = this.mainMenu1;
            this.Name = "FormShowTune";
            this.Text = "ShowTune";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form1_Paint);
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new FormShowTune());
		}

        string filename = null;
		private void fileOpen_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "Wave files (*.wav)|*.wav|MP3 files (*.mp3)|*.mp3";

			if ( fileDlg.ShowDialog() == DialogResult.OK )
			{
                filename = fileDlg.FileName;
				wave = new WaveFile( fileDlg.FileName );

				sbpMainPanel.Text = "Reading .WAV file...";

				wave.Read( );

				sbpMainPanel.Text = "Finished Reading .WAV file...";

                this.Text = "ShowTune--" + filename;

				Refresh( );

			}
		}

		private void Form1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Pen pen = new Pen( ForeColor );
            if (wave != null)
            {
                if (m_DrawWave)
                {
                    sbpMainPanel.Text = "Drawing .WAV file...";

                    wave.Draw(e, pen);

                    sbpMainPanel.Text = "Finished drawing .WAV file...";
                }
            }
		}

		protected override void OnMouseWheel( MouseEventArgs mea )
		{
			if ( mea.Delta * SystemInformation.MouseWheelScrollLines / 120 > 0 )
				wave.ZoomIn( );
			else
				wave.ZoomOut( );

			Refresh( );
		}

        private void fileSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialogSave = new SaveFileDialog();
            if (dialogSave.ShowDialog() == DialogResult.OK)
            {
                wave.Write(dialogSave.FileName);

            }
        }

        private void menuItemSettingsDrawTune_Click(object sender, EventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            mi.Checked = !mi.Checked;
            m_DrawWave = mi.Checked;
            if (wave != null)
            {
                Refresh();
            }
        }

	}
}
