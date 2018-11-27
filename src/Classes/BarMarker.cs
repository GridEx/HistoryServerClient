using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace GridEx.HistoryServerClient.Classes
{
	internal class BarMarker : MarketValueMarker
	{
		public override void RenderHistoryMarker(DrawingContext dc, HistoryValue historyValue, CoordinateTransform transform)
		{
			double size = Size / 2;

			long tick = historyValue.Time.Ticks / 10_000_000;

			var p1 = transform.DataToScreen(new Point(tick, historyValue.Volume));
			var p2 = transform.DataToScreen(new Point(tick, 0));

			dc.DrawRectangle(Fill, null, new Rect(new Point(p1.X - size, p1.Y), new Point(p2.X + size, p2.Y)));
		}
	}
}
