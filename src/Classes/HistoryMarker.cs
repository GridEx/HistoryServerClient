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
	internal class HistoryMarker : MarketValueMarker
	{
		public new double Size
		{
			get => base.Size;
			set
			{
				_downPen = new Pen(_downFill, 1);
				_upPen = new Pen(_upFill, 1);
				base.Size = value;
			}
		}

		public HistoryMarker()
		{
			Size = 5;
		}

		public override void RenderHistoryMarker(DrawingContext dc, HistoryValue historyValue, CoordinateTransform transform)
		{
			double size = Size;

			long tick = historyValue.Time.Ticks / TimeSpan.TicksPerSecond;

			var max = Math.Max(historyValue.High, historyValue.Low);
			var min = Math.Min(historyValue.High, historyValue.Low);
			var top = Math.Max(historyValue.Open, historyValue.Close);
			var bottom = Math.Min(historyValue.Open, historyValue.Close);

			var topCenter = transform.DataToScreen(new Point(tick, max));
			var p1 = topCenter;
			var p2 = transform.DataToScreen(new Point(tick, min));

			var pen = historyValue.Open < historyValue.Close ? _upPen : _downPen;

			dc.DrawLine(pen, new Point(topCenter.X - size, topCenter.Y), new Point(topCenter.X + size, topCenter.Y));
			dc.DrawLine(pen, new Point(topCenter.X, topCenter.Y), p2);
			dc.DrawLine(pen, new Point(p2.X - size, p2.Y), new Point(p2.X + size, p2.Y));
			p1 = transform.DataToScreen(new Point(tick, top));
			p2 = transform.DataToScreen(new Point(tick, bottom));
			dc.DrawRectangle(pen.Brush, 
				null, 
				new Rect(new Point(p1.X - size, p1.Y), new Point(p2.X + size, p2.Y)));
		}

		private static Pen _simplePen = new Pen(Brushes.Black, 1);
		private static Brush _upFill = Brushes.DarkGreen;
		private static Brush _downFill = Brushes.Red;

		private Pen _upPen;
		private Pen _downPen;
	}
}
