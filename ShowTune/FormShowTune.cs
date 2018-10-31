using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace ShowTune
{
    public partial class FormShowTune : Form
    {
		InputMode mode = InputMode.File;

        // MICROPHONE ANALYSIS SETTINGS
        private int RATE = 44100; // sample rate of the sound card
        private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2

        // prepare class objects
        public BufferedWaveProvider bwp;

        public FormShowTune()
        {
            InitializeComponent();
            SetupGraphLabels();
             timerReplot.Enabled = true;
        }

        void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void SetupGraphLabels()
        {
            scottPlotUC1.fig.labelTitle = "Audio PCM Data";
            scottPlotUC1.fig.labelY = "Amplitude (PCM)";
            scottPlotUC1.fig.labelX = "Time (ms)";
            scottPlotUC1.Redraw();

            scottPlotUC2.fig.labelTitle = "Audio FFT Data";
            scottPlotUC2.fig.labelY = "Power (raw)";
            scottPlotUC2.fig.labelX = "Frequency (Hz)";
            scottPlotUC2.Redraw();
        }

		WaveIn wi = null;
        public void StartListeningToMicrophone(int audioDeviceNumber = 0)
        {
            wi = new WaveIn();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * 2;
            bwp.DiscardOnBufferOverflow = true;
            try
            {
                wi.StartRecording();
            }
            catch
            {
                string msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                MessageBox.Show(msg, "ERROR");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // turn off the timer, take as long as we need to plot, then turn the timer back on
            timerReplot.Enabled = false;
            PlotLatestData();
            timerReplot.Enabled = true;
        }

		double[] pcm;

		public int numberOfDraws = 0;
        public bool needsAutoScaling = true;
        public void PlotLatestData()
        {
            // check the incoming microphone audio
            int frameSize = BUFFERSIZE;
            var audioBytes = new byte[frameSize];
			switch (mode)
			{
				case InputMode.Microphone:
					bwp.Read(audioBytes, 0, frameSize);
					break;
				case InputMode.File:
					if(wave!=null && binReader!=null)
						audioBytes = wave.ReadAudioBytes(binReader, frameSize);
					break;
			}

			// return if there's nothing new to plot
			if (audioBytes == null || audioBytes.Length == 0)
			{
				switch (mode)
				{
					case InputMode.File:
						if(binReader!=null)
							binReader.Close();
						timerReplot.Enabled = false;
						keep = true;
						break;
				}
				return;
			}
            if (audioBytes[frameSize - 2] == 0)
                return;

            // incoming data is 16-bit (2 bytes per audio point)
            int BYTES_PER_POINT = 2;

            // create a (32-bit) int array ready to fill with the 16-bit data
            int graphPointCount = audioBytes.Length / BYTES_PER_POINT;

			// create double arrays to hold the data we will graph
			if (!keep)
			{
				pcm = new double[graphPointCount];
				double[] fft = new double[graphPointCount];
				double[] fftReal = new double[graphPointCount];

				// populate Xs and Ys with double data
				for (int i = 0; i < graphPointCount; i++)
				{
					// read the int16 from the two bytes
					byte byteLow = audioBytes[i * 2 + 1];
					byte byteHigh = audioBytes[i * 2];
					Int16 val = (short)(byteHigh * 256 + byteLow);

					// store the value in Ys as a percent (+/- 100% = 200%)
					pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
				}

				// calculate the full FFT
				fft = FFT(pcm);

				// determine horizontal axis units for graphs
				double pcmPointSpacingMs = RATE / 1000;
				double fftMaxFreq = RATE/2;// / 2
				double fftPointSpacingHz = fftMaxFreq / graphPointCount;

				// just keep the real half (the other half imaginary)
				Array.Copy(fft, fftReal, fftReal.Length);

				// plot the Xs and Ys for both graphs
				scottPlotUC1.Clear();
				scottPlotUC1.PlotSignal(pcm, pcmPointSpacingMs, Color.Blue);
				scottPlotUC2.Clear();
				scottPlotUC2.PlotSignal(fftReal, pcmPointSpacingMs , Color.Blue);//fftPointSpacingHz

				// optionally adjust the scale to automatically fit the data
				if (needsAutoScaling)
				{
					scottPlotUC1.AxisAuto();
					scottPlotUC2.AxisAuto();
					needsAutoScaling = false;
				}
			}
            //scottPlotUC1.PlotSignal(Ys, RATE);

            numberOfDraws += 1;
            lblStatus.Text = $"Analyzed and graphed PCM and FFT data {numberOfDraws} times";

            // this reduces flicker and helps keep the program responsive
            Application.DoEvents(); 

        }

        private void autoScaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            needsAutoScaling = true;
        }

        private void infoMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msg = "";
            msg += "left-click-drag to pan\n";
            msg += "right-click-drag to zoom\n";
            msg += "middle-click to auto-axis\n";
            msg += "double-click for graphing stats\n";
            MessageBox.Show(msg);
        }

        private void websiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/swharden/Csharp-Data-Visualization");
        }

		double[] fft;
		bool keep = false;
		public double[] FFT(double[] data)
        {
			if (!keep)
			{
				int k = 0;
				fft = new double[data.Length];
				System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
				for (int i = 0; i < data.Length; i++)
				{
					fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
					if (data[i] > 99)
					{
						k++;
					}
				}
				//if(k>5 && k<8)
				//	keep = true;

				Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
				
				for (int i = 0; i < data.Length; i++)
					fft[i] = fftComplex[i].Magnitude;
				
				/*
				Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Backward);
				for (int i = 0; i < data.Length; i++)
					fft[i] = fftComplex[i].Real;
				*/
				/*
				int nPoints = 512; // whatever we measure must be a power of 2
				double[] _d = new double[nPoints]; // this is what we will measure
				for (int i = 0; i < _d.Length; i++)
					_d[i] = Math.Sin(i); // fill it with some data
				fft = new double[nPoints]; // this is where we will store the output (fft)
				System.Numerics.Complex[] fftComplex2 = new System.Numerics.Complex[nPoints]; // the FFT function requires complex format
				for (int i = 0; i < _d.Length; i++)
					fftComplex2[i] = new System.Numerics.Complex(_d[i], 0.0); // make it complex format
				Accord.Math.FourierTransform.FFT(fftComplex2, Accord.Math.FourierTransform.Direction.Forward);
				//for (int i = 0; i < _d.Length; i++)
				//	fft[i] = fftComplex2[i].Magnitude; // back to double
				Accord.Math.FourierTransform.FFT(fftComplex2, Accord.Math.FourierTransform.Direction.Backward);
				for (int i = 0; i < _d.Length; i++)
					fft[i] = fftComplex2[i].Real; // back to double
				*/
			}
			//
			/*
			for (int i = 0; i < data.Length; i++)
				fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
			Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Backward);
			for (int i = 0; i < data.Length; i++)
				fft[i] = fftComplex[i].Imaginary;
			*/
			return fft;
        }

		WaveFile wave;
		System.IO.BinaryReader binReader = null;
		string filename = null;
		private void menuItemFileOpen_Click(object sender, EventArgs e)
		{
			OpenFileDialog fileDlg = new OpenFileDialog();
			fileDlg.Filter = "Wave files (*.wav)|*.wav|MP3 files (*.mp3)|*.mp3";

			if (fileDlg.ShowDialog() == DialogResult.OK)
			{
				filename = fileDlg.FileName;
				wave = new WaveFile(fileDlg.FileName);

				binReader = wave.ReadWaveFormat();
				keep = false;
				mode = InputMode.File;
				timerReplot.Enabled = true;

				if (wi != null)
					wi.StopRecording();
			}
		}

		private void menuItemFileMicrophone_Click(object sender, EventArgs e)
		{
			StartListeningToMicrophone();
			mode = InputMode.Microphone;
			keep = false;
			timerReplot.Enabled = true;
		}

		private void menuItemFileSaveAs_Click(object sender, EventArgs e)
		{
			if (filename == null)
			{
				MessageBox.Show("Please open an audio file firstly!", "File Issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			SaveFileDialog dialogSave = new SaveFileDialog();
			if (dialogSave.ShowDialog() == DialogResult.OK)
			{
				timerReplot.Enabled = false;
				wave.Read();
				wave.Write(dialogSave.FileName);
			}
		}
	}
}
