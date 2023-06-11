using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ffCutAssist
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private LibVLC _vlc;
		private Media _media = null;

		public MainWindow()
		{
			InitializeComponent();
			textInstr.Text = @"Basic Control:
Ctrl + o: Open Video

Seek Control:
Left / Right: Skip 0.01 sec
Shift + Left/Right: Skip 0.5 sec
Ctrl + Left / Right: Skip 1 sec
Ctrl + Alt + Left / Right: Skip 5 sec
Ctrl + Shift + Alt + Left / Right: Skip 60 sec

Play Control:
Space: Preview Play / Pause
Ctrl + Space: Setting Play / Pause
Down / Up: Volume

Set Control:
Z: Set start setting mode
Ctrl + Z: ...and put current time
X: Set end setting mode
Ctrl + X: ...and put current time
";
			_vlc = new LibVLC();
			vlcP.MediaPlayer = new MediaPlayer(_vlc);
			SetModeText();
		}

		private void textCmd_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var cmd = ((TextBlock)sender).Text;
			Clipboard.SetText(cmd);
		}

		private bool isStartMode = true;
		private long startTimeMs = 0;
		private long endTimeMs = 0;

		private long previousPreviewStartTime = 0;

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			bool ctrl = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl);
			bool alt = e.KeyboardDevice.IsKeyDown(Key.LeftAlt);
			bool shift = e.KeyboardDevice.IsKeyDown(Key.LeftShift);

			switch (e.Key)
			{
				case Key.O:
					if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
					{
						var dial = new OpenFileDialog();
						if (dial.ShowDialog() == true)
						{
							if (File.Exists(dial.FileName))
							{
								var media = new Media(_vlc, new Uri(dial.FileName));
								vlcP.MediaPlayer.Media = media;
							}
						}
					}

					break;

				case Key.Space:
					if (vlcP.MediaPlayer.IsPlaying)
					{
						vlcP.MediaPlayer.Pause();
						if (ctrl)
							SetTimeViaMode(GetCurrentMs());
						else
							vlcP.MediaPlayer.SeekTo(TimeSpan.FromMilliseconds(previousPreviewStartTime));
					}
					else
					{
						previousPreviewStartTime = GetCurrentMs();
						vlcP.MediaPlayer.Play();
					}
					break;

				case Key.Left:
				case Key.Right:
					if (ctrl && shift && alt)
						SeekVideo(60_000 * (e.Key == Key.Right ? 1 : -1));
					else if (ctrl && alt)
						SeekVideo(5_000 * (e.Key == Key.Right ? 1 : -1));
					else if (ctrl)
						SeekVideo(1_000 * (e.Key == Key.Right ? 1 : -1));
					else if (shift)
						SeekVideo(0_100 * (e.Key == Key.Right ? 1 : -1));
					else
						SeekVideo(0_010 * (e.Key == Key.Right ? 1 : -1));

					SetTimeViaMode(previousPreviewStartTime = GetCurrentMs());
					break;

				case Key.Z:
				case Key.X:
					SetMode(e.Key == Key.Z);
					if (ctrl || shift || alt)
					{
						SetTimeViaMode(GetCurrentMs());
					}
					break;

				case Key.Down:
				case Key.Up:
					vlcP.MediaPlayer.Volume += e.Key == Key.Up ? 10 : -10;
					SetModeText();
					break;
			}
		}

		private void SetMode(bool mode)
		{
			isStartMode = mode;
			SetModeText();
		}

		private void SetModeText(float? pos = null, long? dur = null)
		{
			textMode.Text = $"Scope: {(isStartMode ? "Start" : "End")} | Vol: {vlcP.MediaPlayer.Volume}";
		}

		private void SetTimeViaMode(long time)
		{
			if (isStartMode)
				SetStartAndEnd(startMs: time);
			else
				SetStartAndEnd(endMs: time);
		}

		private void SetStartAndEnd(long? startMs = null, long? endMs = null)
		{
			if (startMs.HasValue && startMs.Value >= 0)
				startTimeMs = startMs.Value;
			if (endMs.HasValue && endMs.Value >= 0)
				endTimeMs = endMs.Value;

			textCmd.Text = $"-ss {(float)startTimeMs / 1000.0} -t {(float)(endTimeMs - startTimeMs) / 1000.0}";
		}

		private long GetCurrentMs()
		{
			return (long)(vlcP.MediaPlayer.Position * vlcP.MediaPlayer.Length);
		}

		private long SeekVideo(long amount)
		{
			if (vlcP.MediaPlayer.Length < 0)
				return -1;

			long newPos = GetCurrentMs() + amount;
			if (newPos < 0)
				newPos = 0;
			else if (newPos > (float)vlcP.MediaPlayer.Length * 1000)
				newPos = vlcP.MediaPlayer.Length;

			vlcP.MediaPlayer.SeekTo(TimeSpan.FromMilliseconds(newPos));
			return newPos;
		}
	}
}
