using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GridEx.API.MarketHistory.Responses;
using GridEx.HistoryServerClient.Classes;
using GridEx.HistoryServerClient.Config;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;

namespace GridEx.HistoryServerClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			_logHistory = new List<string>(LogHistoryStringsMaximum);

			InitializeComponent();

			_volumeAxis = (DateTimeAxis)volumePlot.HorizontalAxis;
			_priceAxis = (DateTimeAxis)pricePlot.HorizontalAxis;

			supportedTimeFraims.ItemsSource = GridEx.API.MarketHistory.SupportedTimeFrames.Values;
			supportedTimeFraims.SelectedIndex = 0;

			Pen p = new Pen(Brushes.Red, MarkerSize);
			p.Freeze();

			priceMarkers.Marker = new HistoryMarker() { Pen = p, Fill = p.Brush, Size = MarkerSize };
			volumeMarkers.Marker = new BarMarker() { Pen = p, Fill = p.Brush, Size = MarkerSize };

			volumePlot.SetHorizontalAxisMapping(new Func<double, DateTime>(val => new DateTime((long)val * 10_000_000)),
				 new Func<DateTime, double>(time => time.Ticks / 10_000_000.0));
			pricePlot.SetHorizontalAxisMapping(new Func<double, DateTime>(val => new DateTime((long)val * 10_000_000)),
				 new Func<DateTime, double>(time => time.Ticks / 10_000_000.0));

			((ContentControl)pricePlot.VerticalAxis).SizeChanged += VerticalAxis_SizeChanged;
			((ContentControl)volumePlot.VerticalAxis).SizeChanged += VerticalAxis_SizeChanged;
			
			volumePlot.HorizontalAxis.TicksChanged += HorizontalAxis_TicksChanged;
			pricePlot.MouseNavigation.Remove();
			volumePlot.MouseNavigation.Remove();

			((ContentControl)pricePlot.HorizontalAxis).Visibility = Visibility.Collapsed;

			Loaded += MainWindow_Loaded;
		}

		private void SetDefauiltRangeByTimeFrame()
		{
			var now = DateTime.Now;
			_maxTime = now.Date.AddMinutes((int)(now.TimeOfDay.TotalMinutes + _timeFrame));
			setViewToDefault_Click(null, null);
		}

		private void HorizontalAxis_TicksChanged(object sender, EventArgs e)
		{
			var maxVal = _volumeAxis.ConvertToDouble(_maxTime);

			var maxAxisVal = volumePlot.Visible.X + volumePlot.Visible.Width;
			if (maxAxisVal > maxVal)
			{
				volumePlot.Visible = new Rect(volumePlot.Visible.X, volumePlot.Visible.Y, maxVal - volumePlot.Visible.X, volumePlot.Visible.Height);
			}
			else
			{
				pricePlot.Visible = new Rect(volumePlot.Visible.X, pricePlot.Visible.Y, volumePlot.Visible.Width, pricePlot.Visible.Height);
			}
		}

		private void VerticalAxis_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var priceAxis = (ContentControl)pricePlot.VerticalAxis;
			var valueAxis = (ContentControl)volumePlot.VerticalAxis; 
			var maxWidth = Math.Max(priceAxis.ActualWidth, valueAxis.ActualWidth);
			priceAxis.Margin = new Thickness(maxWidth - priceAxis.ActualWidth, 0, 0, 0);
			valueAxis.Margin = new Thickness(maxWidth - valueAxis.ActualWidth, 0, 0, 0); ;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= MainWindow_Loaded;
			SetDefauiltRangeByTimeFrame();
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			IPWindow iPWindow = new IPWindow(ref App.ConnectionConfig) { Owner = this };
			iPWindow.ShowDialog();
		}

		private void ConnectToMarketButton_Unchecked(object sender, RoutedEventArgs e)
		{
			FinishWatchMarket();

			ConnectToMarketButton.IsEnabled = false;
			ConnectToMarketButton.Header = "Disconnecting from server";

			Dispatcher.BeginInvoke(new Action(() =>
			{
				_processesStartedEvent.Reset();

				_enviromentExitWait.Wait(5000);

				if (!_enviromentExitWait.IsSet)
				{
					FinishWatchMarket();
					StopClient();
					_enviromentExitWait.Set();
				}

				ConnectToMarketButton.Header = "Disconnected from server (press to connect)";
				ConnectToMarketButton.IsEnabled = true;
			}), DispatcherPriority.Background);
		}

		private void ConnectToMarketButton_Checked(object sender, RoutedEventArgs e)
		{
			_stop = false;
			supportedTimeFraims.IsEnabled = false;
			ConnectToMarketButton.IsEnabled = false;
			ConnectToMarketButton.Header = "Connecting to server";
			ConnectToMarketButton.ToolTip = "Trying to connect";
			Dispatcher.Invoke(new Action(() =>
			{
				freshestBar = null;
				_firstTickChange = true;
				SetDefauiltRangeByTimeFrame();
				CreateDataThread();
				CreateDataCollectionThread();
			}), DispatcherPriority.Background);
		}

		private void AddMessageToLog(string message)
		{
			Dispatcher.BeginInvoke(
				new Action(() =>
				{
					var text = $"{DateTime.Now.ToString("hh:mm:ss.fff")}: {message}";
					_logHistory.Add(text);

					if (_logHistory.Count > LogHistoryStringsMaximum)
					{
						_logHistory.RemoveRange(0, LogHistoryStrings);
						log.Text = _logHistory.Aggregate(string.Empty, (res, str) => $"{res}{str}{Environment.NewLine}").ToString();
					}
					else
					{
						log.Text += $"{text}{Environment.NewLine}";
					}

					logHistoruContainer.IsExpanded = true;
				}));
		}

		private void OnException(MarketClient client, Exception exception)
		{
			AddMessageToLog(exception.Message);
			Dispatcher.BeginInvoke(
				new Action(() =>
				{
					ConnectToMarketButton.IsChecked = false;
				}));
		}

		private void OnDisconnected(MarketClient client)
		{
			AddMessageToLog("Disconnected from server");
		}

		private void OnError(MarketClient client, SocketError socketError)
		{
			AddMessageToLog(socketError.ToString());
			Dispatcher.BeginInvoke(
				new Action(() =>
				{
					AddMessageToLog($"Error: {socketError.ToString()}");
					ConnectToMarketButton.IsChecked = false;
				}));
		}

		private void OnConnected(MarketClient client)
		{
			AddMessageToLog("Connected to server");
		}

		private void StopClient()
		{
			_stop = true;

			var marketClient = _marketClient;
			if (marketClient != null)
			{
				marketClient.OnError -= OnError;
				marketClient.OnDisconnected -= OnDisconnected;
				marketClient.OnConnected -= OnConnected;
				marketClient.OnException -= OnException;
				marketClient.OnHistory -= OnHistory;
				marketClient.OnHistoryStatus -= OnHistoryStatus;
				marketClient.OnLastHistory -= OnLastHistory;
				marketClient.OnRequestRejected -= OnRequestRejected;
				marketClient.OnRestrictionsViolated -= OnRestrictionsViolated;
				marketClient.OnTickChange -= OnTickChange;
				marketClient.Dispose();
			}

			AddMessageToLog("Client stopped");

			Dispatcher.BeginInvoke(new Action(() =>
			{
				supportedTimeFraims.IsEnabled = true;
				_ticksPS = 0;
				priceMarkers.DataSource = null;
				volumeMarkers.DataSource = null;
			}), DispatcherPriority.Normal);
		}

		private void FinishWatchMarket()
		{
			_stop = true;
		}

		private void CollectData()
		{
			_processesStartedEvent.Wait(5000);

			var marketClient = _marketClient;

			if (!_processesStartedEvent.IsSet || marketClient == null || !marketClient.IsConnected)
			{
				Dispatcher.Invoke(new Action(() =>
				{
					StopClient();
					FinishWatchMarket();
					ConnectToMarketButton.IsChecked = false;

					MessageBox.Show(this,
						"Couldn't connect to server.\nPlease check destination IP and your network connection.",
						"Connection problem!",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					ConnectToMarketButton.IsChecked = false;
				}));
			}
			else
			{
				try
				{
					Dispatcher.Invoke(new Action(() =>
					{
						ConnectToMarketButton.Header = "Connected to market (press to disconnect)";
						ConnectToMarketButton.ToolTip = "Press to disconnect from Market Depth Server";
						ConnectToMarketButton.IsEnabled = true;
					}));
				}
				catch { }

				var pauseEvent = new ManualResetEventSlim();

				while (!_stop)
				{
					if (marketClient.IsConnected)
					{
						try
						{
							Dispatcher.Invoke(new Action(() =>
							{
								var history = _history.ToArray();
								var ticksPS = Interlocked.Exchange(ref _ticksPS, 0);
								chartContainer.Header = $"Market chart / Ticks: {ticksPS}";
								priceMarkers.DataSource = history;
								priceMarkers.UpdateLayout();
								volumeMarkers.DataSource = history;
								volumeMarkers.UpdateLayout();

								if (_needToSetViewToDefault)
								{
									_needToSetViewToDefault = false;
									setViewToDefault_Click(null, null);
								}

							}), DispatcherPriority.Normal);
						}
						catch { }
					}
					pauseEvent.Reset();
					pauseEvent.Wait(1000);
				}

				StopClient();
			}
		}

		private void CreateDataCollectionThread()
		{
			_dataCollectionThread = new Thread(new ThreadStart(CollectData));
			_dataCollectionThread.SetApartmentState(ApartmentState.STA);
			_dataCollectionThread.Start();
		}

		private void CreateDataThread()
		{
			_timeFrame = (ushort)supportedTimeFraims.SelectedValue;
			_ticksPerFrame = _timeFrame * TimeSpan.TicksPerMinute;
			_dataThread = new Thread(new ThreadStart(DataThread))
			{
				Priority = ThreadPriority.Highest,
				IsBackground = true
			};
			_dataThread.SetApartmentState(ApartmentState.MTA);
			_dataThread.Start();
		}

		private void DataThread()
		{
			_enviromentExitWait.Reset();

			_cancellationTokenSource = new CancellationTokenSource();
			_processesStartedEvent = new ManualResetEventSlim();

			var marketClient = new MarketClient(_enviromentExitWait, _processesStartedEvent, _timeFrame);

			_marketClient = marketClient;

			marketClient.OnConnected += OnConnected;
			marketClient.OnDisconnected += OnDisconnected;
			marketClient.OnError += OnError;
			marketClient.OnException += OnException;
			marketClient.OnHistory += OnHistory;
			marketClient.OnHistoryStatus += OnHistoryStatus;
			marketClient.OnLastHistory += OnLastHistory;
			marketClient.OnRequestRejected += OnRequestRejected;
			marketClient.OnRestrictionsViolated += OnRestrictionsViolated;
			marketClient.OnTickChange += OnTickChange;

			marketClient.Run(App.ConnectionConfig.IP.MapToIPv4(), App.ConnectionConfig.Port, ref _cancellationTokenSource, ref _enviromentExitWait);
		}

		private void OnTickChange(MarketClient client, TickChange tickChange)
		{
			Interlocked.Increment(ref _ticksPS);
			var ticks = HistoryValue.EpoxTicks + tickChange.Time;

			if (freshestBar != null && (ticks - freshestBar.Time.Ticks) <= _ticksPerFrame)
			{
				freshestBar.UpdateState(ref tickChange);
			}
			else
			{
				if (_firstTickChange)
				{
					_firstTickChange = false;
					var index = freshestBar != null 
						? ((ticks - freshestBar.Time.Ticks) / _ticksPerFrame)
						: History.HistoryLength;
					if (index > 0)
					{
						for (int i = 0; i < index; i++)
						{
							_history.Add(freshestBar = new HistoryValue(0, tickChange.Price, tickChange.Price, tickChange.Price, tickChange.Price, tickChange.Time - _ticksPerFrame * (index - i)));
						}
					}
				}

				_history.Add(freshestBar = new HistoryValue(tickChange.Volume, tickChange.Price, tickChange.Price, tickChange.Price, tickChange.Price, tickChange.Time));
				if (freshestBar.Time.Ticks > _maxTime.Ticks)
				{
					_maxTime = freshestBar.Time.Date.AddMinutes((int)(freshestBar.Time.TimeOfDay.TotalMinutes + _timeFrame * 2));
					Dispatcher.Invoke(new Action(() =>
					{
						var max = _volumeAxis.ConvertToDouble(_maxTime);
						volumePlot.Visible = new Rect(volumePlot.Visible.X, volumePlot.Visible.Y, max - volumePlot.Visible.X, volumePlot.Visible.Height);
					}));
				}
			}
		}

		private void OnRestrictionsViolated(MarketClient arg1, HistoryRestrictionsViolated arg2)
		{
			
		}

		private void OnRequestRejected(MarketClient arg1, HistoryRequestRejected arg2)
		{
			
		}

		private void OnLastHistory(MarketClient socket, ref LastHistory history)
		{
			
		}

		private void OnHistoryStatus(MarketClient arg1, HistoryStatus arg2)
		{
			
		}

		private unsafe void OnHistory(MarketClient socket, ref History history)
		{
			_history = new ConcurrentBag<HistoryValue>();
			for (int i = history.BarsQuantity - 1; i >= 0; i--)
			{
				_history.Add(freshestBar = new HistoryValue(history.Volume[i], history.Open[i], history.Close[i], history.High[i], history.Low[i], history.Time[i]));
			}
			_needToSetViewToDefault = true;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			FinishWatchMarket();
			_enviromentExitWait.Wait(5000);

			if (!_enviromentExitWait.IsSet && _marketClient != null)
				StopClient();

			App.ConnectionConfig.Save();
		}

		private void supportedTimeFraims_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_timeFrame = (ushort)supportedTimeFraims.SelectedValue;
		}

		private void setViewToDefault_Click(object sender, RoutedEventArgs e)
		{
			var maxTime = _maxTime;
			var max = _volumeAxis.ConvertToDouble(maxTime);
			var min = _volumeAxis.ConvertToDouble(maxTime.AddMinutes(-_timeFrame * (CountOfBars + 1)));
			volumePlot.Visible = new Rect(min, volumePlot.Visible.Y, max - min, volumePlot.Visible.Height);
		}

		private void setVerticalToDefault_Click(object sender, RoutedEventArgs e)
		{
			if (volumeMarkers.ContentBounds.IsEmpty || priceMarkers.ContentBounds.IsEmpty)
			{
				return;
			}

			var fivePercentValue = volumeMarkers.ContentBounds.Height * 0.05;
			volumePlot.Visible = new Rect(volumePlot.Visible.X, 
				volumeMarkers.ContentBounds.Top - fivePercentValue,
				volumePlot.Visible.Width,
				volumeMarkers.ContentBounds.Height + fivePercentValue * 2);

			fivePercentValue = priceMarkers.ContentBounds.Height * 0.05;
			pricePlot.Visible = new Rect(pricePlot.Visible.X,
				priceMarkers.ContentBounds.Top - fivePercentValue,
				pricePlot.Visible.Width,
				priceMarkers.ContentBounds.Height + fivePercentValue * 2);
		}

		private void showAll_Click(object sender, RoutedEventArgs e)
		{
			if (volumeMarkers.ContentBounds.IsEmpty || priceMarkers.ContentBounds.IsEmpty)
			{
				return;
			}

			var fivePercentValueVertical = volumeMarkers.ContentBounds.Height * 0.05;
			var fivePercentValueHorizontal = volumeMarkers.ContentBounds.Width * 0.05;

			var maxTime = _maxTime;
			var max = _volumeAxis.ConvertToDouble(maxTime);

			volumePlot.Visible = new Rect(volumeMarkers.ContentBounds.X - fivePercentValueHorizontal,
				volumeMarkers.ContentBounds.Top - fivePercentValueVertical,
				max - volumeMarkers.ContentBounds.X + fivePercentValueHorizontal * 2,
				volumeMarkers.ContentBounds.Height + fivePercentValueVertical * 2);

			fivePercentValueVertical = priceMarkers.ContentBounds.Height * 0.05;

			pricePlot.Visible = new Rect(volumePlot.Visible.X,
				priceMarkers.ContentBounds.Top - fivePercentValueVertical,
				volumePlot.Visible.Width,
				priceMarkers.ContentBounds.Height + fivePercentValueVertical * 2);
		}

		private const ushort LogHistoryStrings = 50;
		private const ushort LogHistoryStringsMaximum = 100;
		private const int CountOfBars = 20;
		private const int MarkerSize = 5;

		private Thread _dataCollectionThread;
		private Thread _dataThread;
		private MarketClient _marketClient;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private ManualResetEventSlim _enviromentExitWait = new ManualResetEventSlim(true);
		private ManualResetEventSlim _processesStartedEvent = new ManualResetEventSlim();
		private bool _stop;
		
		private List<string> _logHistory;
		private ConcurrentBag<HistoryValue> _history = new ConcurrentBag<HistoryValue>();
		private HistoryValue freshestBar = null;
		private long _ticksPS = 0;
		private long _ticksPerFrame;
		private ushort _timeFrame;
		private bool _firstTickChange = true;

		private DateTime _maxTime;
		private DateTimeAxis _priceAxis;
		private DateTimeAxis _volumeAxis;

		private bool _needToSetViewToDefault = false;
	}
}
