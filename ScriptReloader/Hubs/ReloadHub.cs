using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using Qlik.Engine;

namespace ScriptReloader.Hubs
{
	public class ReloadHub : Microsoft.AspNet.SignalR.Hub
	{
		private static string _activeServer = string.Empty;
		private List<string> _connections = new List<string>();

		[MethodImpl(MethodImplOptions.NoInlining)]
		public string GetCurrentMethod()
		{
			StackTrace st = new StackTrace();
			StackFrame sf = st.GetFrame(1);

			return sf.GetMethod().Name;
		}

		public ReloadHub()
        {
	        try
	        {
				var task = Task.Factory.StartNew(async () =>
				{
					while (true)
					{
						try
						{
							//Wait for the SignalR to startup.
							await Task.Delay(5000);
							FillConnections();
						}
						catch (Exception ex)
						{
							NotifyClients(ex);
						}
					}
				}); 
			}
	        catch (Exception ex)
	        {
				NotifyClients(ex);
			}
		}

		private void FillConnections()
		{
			try
			{
				if (_connections.Any())
				{
					_connections.Clear();
				}
				_connections.Add("ws://127.0.0.1:4848");
				_connections.Add("http://127.0.0.1:4848");
				Clients.All.newConnections(_connections);
			}
			catch (Exception ex)
			{
				NotifyClients(ex);
			}
		}

		private static ILocation InitializeLocation()
		{
			var location = Qlik.Engine.Location.FromUri(new Uri(_activeServer));
			if (_activeServer.Contains("4848"))
			{
				location.AsDirectConnectionToPersonalEdition();
			}
			else
			{
				location.AsNtlmUserViaProxy();
			}
			return location;
		}

		private void NotifyClients(Exception ex)
		{
			Clients.All.sendMessage(GetCurrentMethod() + "Exception " + ex.Message);
		}

		private void NotifyClients(string msg)
		{
			Clients.All.sendMessage(msg);
		}

		private void GetApps()
	    {
		    try
		    {
			    if (_activeServer != string.Empty)
			    {
				    var location = InitializeLocation();
				    var appIdentifiers = location.GetAppIdentifiers(noVersionCheck: true).ToArray();
				    List<string> apps = appIdentifiers.Select(appIdentifier => appIdentifier.AppName).ToList();
				    Clients.All.newApps(apps);
			    }
		    }
		    catch (Exception ex)
		    {
				NotifyClients(ex);
		    }
	    }

		public void SetActiveServer(string con)
		{
			_activeServer = con;
			NotifyClients("Getting apps for " + _activeServer);
			GetApps();
		}

		public void Reload(string con, string appName, string script)
		{
			try
			{
				var location = InitializeLocation();

				var appIdentifier = location.AppWithNameOrDefault(appName, noVersionCheck: true);

				var app = location.App(appIdentifier, noVersionCheck: true);
				Session.WithApp(appIdentifier, SessionType.Random);

				app.SetScript(script);
				if (app.DoReload())
				{
					NotifyClients(appIdentifier.AppName + " last reloaded at " + app.GetAppLayout().LastReloadTime);
				}
				else
				{
					NotifyClients(appIdentifier.AppName + " reload failed!");
				}
				app.SetScript(string.Empty);
			}
			catch (Exception ex)
			{
				NotifyClients(ex);
			}
		}
	}
}