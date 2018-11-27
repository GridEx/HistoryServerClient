using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GridEx.HistoryServerClient.Classes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace GridEx.HistoryServerClient.Classes
{
	abstract internal class MarketValueMarker : ShapePointMarker
	{
		public override void Render(DrawingContext dc, Point screenPoint)
		{
			throw new NotImplementedException("Please use \"RenderHistoryMarker(DrawingContext dc, HistoryValue historyValue, CoordinateTransform transform)\" call");
		}

		public abstract void RenderHistoryMarker(DrawingContext dc, HistoryValue historyValue, CoordinateTransform transform);
	}
}
