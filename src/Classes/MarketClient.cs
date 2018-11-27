using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GridEx.API;
using GridEx.API.MarketHistory;
using GridEx.API.MarketHistory.Responses;

using GridEx.API.MarketHistory.Requests;

namespace GridEx.HistoryServerClient.Classes
{
	public delegate void OnLastHistoryDelegateMarketClient(MarketClient marketClient, ref LastHistory history);

	public delegate void OnHistoryDelegateMarketClient(MarketClient marketClient, ref History history);

	public class MarketClient : IDisposable
	{
		public Action<MarketClient, Exception> OnException = delegate { };
		public Action<MarketClient> OnConnected = delegate { };
		public Action<MarketClient, SocketError> OnError = delegate { };
		public Action<MarketClient> OnDisconnected = delegate { };
		public Action<string> AddMessageToFileLog = delegate { };
		public Action <MarketClient, TickChange> OnTickChange = delegate { };
		public Action <MarketClient, HistoryRestrictionsViolated> OnRestrictionsViolated = delegate { };
		public Action <MarketClient, HistoryRequestRejected> OnRequestRejected = delegate { };
		public Action <MarketClient, HistoryStatus> OnHistoryStatus = delegate { };

		public event OnHistoryDelegateMarketClient OnHistory;
		public event OnLastHistoryDelegateMarketClient OnLastHistory;

		public MarketClient(ManualResetEventSlim enviromentExitWait, ManualResetEventSlim processesStartedEvent, ushort timeFrame)
		{
			_timeFrame = timeFrame;
			_enviromentExitWait = enviromentExitWait;
			_processesStartedEvent = processesStartedEvent;
		}

		public bool IsConnected
		{
			get
			{
				var socket = _marketHistorySocket;
				return socket != null && socket.IsConnected;
			}
		}
			
		public void Run(IPAddress serverAddress, int serverPort, 
			ref CancellationTokenSource cancellationTokenSource,
			ref ManualResetEventSlim enviromentExitWait)
		{
			_enviromentExitWait = enviromentExitWait;
			_cancellationTokenSource = cancellationTokenSource;

			var socket = new MarketHistorySocket();
			_marketHistorySocket = socket;
			socket.OnError += OnErrorHandler;
			socket.OnDisconnected += OnDisconnectedHandler;
			socket.OnConnected += OnConnectedHandler;
			socket.OnException += OnExceptionHandler;
			socket.OnHistory += Socket_OnHistory;
			socket.OnHistoryStatus += Socket_OnHistoryStatus;
			socket.OnLastHistory += Socket_OnLastHistory;
			socket.OnRequestRejected += Socket_OnRequestRejected;
			socket.OnRestrictionsViolated += Socket_OnRestrictionsViolated;
			socket.OnTickChange += Socket_OnTickChange;

			void RunSocket()
			{
				try
				{
					socket.Connect(new IPEndPoint(serverAddress.MapToIPv4(), serverPort));
					socket.Send(new GetHistoryStatus(idRequest++, _timeFrame));
					socket.WaitResponses(_cancellationTokenSource.Token);
				}
				catch
				{
					try
					{
						socket.Dispose();
					}
					catch { }
				}
				finally
				{
					Dispose();
				}
			}
			Task.Factory.StartNew(
				() => RunSocket(),
				TaskCreationOptions.LongRunning);
		}

		private void Socket_OnTickChange(MarketHistorySocket arg1, TickChange arg2)
		{
			OnTickChange?.Invoke(this, arg2);
		}

		private void Socket_OnRestrictionsViolated(MarketHistorySocket arg1, HistoryRestrictionsViolated arg2)
		{
			OnRestrictionsViolated?.Invoke(this, arg2);
		}

		private void Socket_OnRequestRejected(MarketHistorySocket arg1, HistoryRequestRejected arg2)
		{
			OnRequestRejected?.Invoke(this, arg2);
		}

		private void Socket_OnLastHistory(MarketHistorySocket socket, ref LastHistory history)
		{
			OnLastHistory?.Invoke(this, ref history);
		}

		private void Socket_OnHistoryStatus(MarketHistorySocket arg1, HistoryStatus arg2)
		{
			arg1.Send(new GetHistory(idRequest++, SupportedTimeFrames.Values[0], arg2.LastFilledBarTime));
			//OnHistoryStatus?.Invoke(this, arg2);
		}

		private void Socket_OnHistory(MarketHistorySocket socket, ref History history)
		{
			//socket.Send(new LastHistory())
			OnHistory?.Invoke(this, ref history);
		}

		public void Disconnect(bool waitLittlePause = false)
		{
			if (!_cancellationTokenSource.IsCancellationRequested)
			{
				_cancellationTokenSource.Cancel();
			}

			var socket = _marketHistorySocket;
			if (socket != null)
			{
				try
				{
					socket.OnError -= OnErrorHandler;
					socket.OnDisconnected -= OnDisconnectedHandler;
					socket.OnConnected -= OnConnectedHandler;
					socket.OnException -= OnExceptionHandler;
					socket.OnHistory -= Socket_OnHistory;
					socket.OnHistoryStatus -= Socket_OnHistoryStatus;
					socket.OnLastHistory -= Socket_OnLastHistory;
					socket.OnRequestRejected -= Socket_OnRequestRejected;
					socket.OnRestrictionsViolated -= Socket_OnRestrictionsViolated;
					socket.OnTickChange -= Socket_OnTickChange;
					socket.Dispose();
				}
				catch
				{

				}
				finally
				{
					_marketHistorySocket = null;
				}
			}

			if (waitLittlePause)
			{
				Thread.Sleep(1000);
			}

			if (!_enviromentExitWait.IsSet)
			{
				_enviromentExitWait.Set();
			}
		}

		public void Dispose()
		{
			Disconnect();
		}

		void OnExceptionHandler(GridExSocketBase socket, Exception exception)
		{
			var s = _marketHistorySocket;
			if (s != null)
			{
				s.OnException -= OnExceptionHandler;
			}

			Disconnect();
			OnException?.Invoke(this, exception);
		}

		void OnErrorHandler(GridExSocketBase socket, SocketError error)
		{
			OnError?.Invoke(this, error);
		}

		void OnDisconnectedHandler(GridExSocketBase socket)
		{
			Disconnect();
			OnDisconnected?.Invoke(this);
		}

		void OnConnectedHandler(GridExSocketBase socket)
		{
			_processesStartedEvent.Set();
			OnConnected?.Invoke(this);
		}

		private MarketHistorySocket _marketHistorySocket;
		private ManualResetEventSlim _enviromentExitWait;
		private ManualResetEventSlim _processesStartedEvent;
		private CancellationTokenSource _cancellationTokenSource;
		private long idRequest = 0;
		private ushort _timeFrame;
	}
}
