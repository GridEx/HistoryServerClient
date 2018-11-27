using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GridEx.HistoryServerClient.Config;

namespace GridEx.HistoryServerClient
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static ConnectionConfig ConnectionConfig;
		public static Options Options;

		static App()
		{
			ConnectionConfig = new ConnectionConfig("config.xml");
			Options = new Options("options.xml");
		}
	}
}
