using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GridEx.API.MarketHistory.Responses;

namespace GridEx.HistoryServerClient.Classes
{
	internal class HistoryValue
	{
		public HistoryValue(double volume,
							double open,
							double close,
							double high,
							double low,
							long unixEpoxTimeTicks)
		{
			Volume = volume;
			Open = open;
			Close = close;
			High = high;
			Low = low;
			Time = Epox.AddTicks(unixEpoxTimeTicks);
		}

		public void UpdateState(ref TickChange state)
		{
			Volume += state.Volume;
			Close = state.Price;
			High = Math.Max(High, Close);
			Low = Math.Min(Low, Close);
		}

		public double Volume
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public double Open
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public double Close
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public double High
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public double Low
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public DateTime Time
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public static DateTime Epox = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local).AddTicks(DateTimeOffset.Now.Offset.Ticks);
		public static long EpoxTicks = Epox.Ticks;
	}
}