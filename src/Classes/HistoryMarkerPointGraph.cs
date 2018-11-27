using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace GridEx.HistoryServerClient.Classes
{
	internal abstract class HistoryMarkerPointGraph : MarkerPointsGraph
	{
		public new MarketValueMarker Marker { get; set; }

		IEnumerable<HistoryValue> _dataSource;
		public new IEnumerable<HistoryValue> DataSource
		{
			get => _dataSource;
			set
			{
				_dataSource = value;
				Update();
			}
		}

		public HistoryMarkerPointGraph() : base()
		{
			
		}

		public void ReDraw()
		{
			Update();
		}

		protected void UpdateBoundsIfNeed(double xMin, double yMin, double xMax, double yMax)
		{
			if (_updateBoundsIsNeeded)
			{
				ContentBounds = MathHelper.CreateRectByPoints(xMin, yMin, xMax, yMax);
				_updateBoundsIsNeeded = false;
			}
			else
			{
				ContentBounds = MathHelper.CreateRectByPoints(ContentBounds.X, yMin, ContentBounds.X + ContentBounds.Width, yMax);
			}

			//Viewport.Visible = new Rect(Viewport.Visible.X, yMin, Viewport.Visible.Width, yMax - yMin);
		}

		private bool _updateBoundsIsNeeded = true;
	}

	internal class PriceMarkerGraph : HistoryMarkerPointGraph
	{
		protected override void OnRenderCore(DrawingContext dc, RenderState state)
		{
			if (DataSource == null || !DataSource.Any())
			{
				return;
			}

			var transform = GetTransform();

			double xMin = Double.PositiveInfinity;
			double xMax = Double.NegativeInfinity;

			double yMin = Double.PositiveInfinity;
			double yMax = Double.NegativeInfinity;

			foreach (var item in this.DataSource)
			{
				var ticks = item.Time.Ticks / TimeSpan.TicksPerSecond;

				var topLeft = transform.DataTransform.DataToViewport(new Point(ticks, item.High));
				var bottomRight = transform.DataTransform.DataToViewport(new Point(ticks, item.Low));

				xMin = Math.Min(xMin, topLeft.X);
				xMax = Math.Max(xMax, bottomRight.X);

				yMin = Math.Min(yMin, bottomRight.Y);
				yMax = Math.Max(yMax, topLeft.Y);

				Marker.RenderHistoryMarker(dc, item, transform);
			}

			UpdateBoundsIfNeed(xMin, yMin, xMax, yMax);
		}
	}

	internal class VolumeMarkerGraph : HistoryMarkerPointGraph
	{
		protected override void OnRenderCore(DrawingContext dc, RenderState state)
		{
			if (this.DataSource == null || !DataSource.Any())
			{
				return;
			}

			var transform = GetTransform();

			double xMin = Double.PositiveInfinity;
			double xMax = Double.NegativeInfinity;
			double yMax = Double.NegativeInfinity;

			foreach (var item in this.DataSource)
			{
				var ticks = item.Time.Ticks / TimeSpan.TicksPerSecond;

				var topLeft = transform.DataTransform.DataToViewport(new Point(ticks, item.Volume));
				var bottomRight = transform.DataTransform.DataToViewport(new Point(ticks, 0));

				xMin = Math.Min(xMin, topLeft.X);
				xMax = Math.Max(xMax, bottomRight.X);

				yMax = Math.Max(yMax, topLeft.Y);

				Marker.RenderHistoryMarker(dc, item, transform);
			}

			UpdateBoundsIfNeed(xMin, 0, xMax, yMax);
		}
	}
}
