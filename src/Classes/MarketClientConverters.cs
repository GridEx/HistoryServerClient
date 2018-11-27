using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using InteractiveDataDisplay.WPF;

namespace HistoryServerClient.Classes
{
	public class BarGraphRightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				if (value == null)
					return value;
				DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
				if (model != null)
				{
					return System.Convert.ToDouble(model.Sources["Time"], CultureInfo.InvariantCulture) +
						System.Convert.ToDouble(model.Sources["Width"], CultureInfo.InvariantCulture) / 2;
				}
				else
					return 0;
			}
			catch (Exception exc)
			{
				Debug.WriteLine("Cannot convert value: " + exc.Message);
				return 0;
			}
		}

		/// <summary>
		/// This method is not supported.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public class BarGraphLeftConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				if (value == null)
					return value;
				DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
				if (model != null)
				{
					return System.Convert.ToDouble(model.Sources["Time"], CultureInfo.InvariantCulture) -
						System.Convert.ToDouble(model.Sources["Width"], CultureInfo.InvariantCulture) / 2;
				}
				else
					return 0;
			}
			catch (Exception exc)
			{
				Debug.WriteLine("Cannot convert value: " + exc.Message);
				return 0;
			}
		}

		/// <summary>
		/// This method is not supported.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
