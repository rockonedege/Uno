using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.SourceGeneration;
using Uno.UI.SourceGenerators.Helpers;

namespace Uno.UI.SourceGenerators.RemoteControl
{
	public class RemoteControlGenerator : SourceGenerator
	{
		public override void Execute(SourceGeneratorContext context)
		{
			if (
				context.GetProjectInstance().GetPropertyValue("Configuration") == "Debug"
				&& IsRemoteControlClientInstalled(context)
				&& IsApplication(context.GetProjectInstance()))
			{
				var sb = new IndentedStringBuilder();

				BuildEndPointAttribute(context, sb);
				BuildSearchPaths(context, sb);

				context.AddCompilationUnit("RemoteControl", sb.ToString());
			}
		}

		private static bool IsRemoteControlClientInstalled(SourceGeneratorContext context)
			=> context.Compilation.GetTypeByMetadataName("Uno.UI.RemoteControl.RemoteControlClient") != null;

		private static void BuildSearchPaths(SourceGeneratorContext context, IndentedStringBuilder sb)
		{
			var projectInstance = context.GetProjectInstance();

			sb.AppendLineInvariant($"[assembly: global::Uno.UI.RemoteControl.ProjectConfigurationAttribute(");
			sb.AppendLineInvariant($"@\"{projectInstance.FullPath}\",\n");

			var sources = new[] {
					"Page",
					"ApplicationDefinition",
					"ProjectReference",
				};

			IEnumerable<string> BuildSearchPath(string s)
				=> projectInstance
					.GetItems(s)
					.Select(v => v.EvaluatedInclude)
					.Select(v => Path.IsPathRooted(v) ? v : Path.Combine(projectInstance.Directory, v));

			var xamlPaths = from item in sources.SelectMany(BuildSearchPath)
							select Path.GetDirectoryName(item);

			var distictPaths = string.Join(",\n", xamlPaths.Distinct().Select(p => $"@\"{p}\""));

			sb.AppendLineInvariant("{0}", $"new[]{{{distictPaths}}}");

			sb.AppendLineInvariant($")]");
		}


		private static void BuildEndPointAttribute(SourceGeneratorContext context, IndentedStringBuilder sb)
		{
			var unoRemoteControlPort = context.GetProjectInstance().GetPropertyValue("UnoRemoteControlPort");

			if (string.IsNullOrEmpty(unoRemoteControlPort))
			{
				unoRemoteControlPort = "0";
			}

			var unoRemoteControlHost = context.GetProjectInstance().GetPropertyValue("UnoRemoteControlHost");

			if (string.IsNullOrEmpty(unoRemoteControlHost))
			{
				var addresses = NetworkInterface.GetAllNetworkInterfaces()
					.SelectMany(x => x.GetIPProperties().UnicastAddresses)
					.Where(x => !IPAddress.IsLoopback(x.Address));

				foreach (var addressInfo in addresses)
				{
					sb.AppendLineInvariant($"[assembly: global::Uno.UI.RemoteControl.ServerEndpointAttribute(\"{addressInfo.Address}\", {unoRemoteControlPort})]");
				}
			}
			else
			{
				sb.AppendLineInvariant($"[assembly: global::Uno.UI.RemoteControl.ServerEndpointAttribute(\"{unoRemoteControlHost}\", {unoRemoteControlPort})]");
			}
		}

		private bool IsApplication(ProjectInstance projectInstance)
		{
			var isAndroidApp = projectInstance.GetPropertyValue("AndroidApplication")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
			var isiOSApp = projectInstance.GetPropertyValue("ProjectTypeGuids")?.Equals("{FEACFBD2-3405-455C-9665-78FE426C6842};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var ismacOSApp = projectInstance.GetPropertyValue("ProjectTypeGuids")?.Equals("{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var isExe = projectInstance.GetPropertyValue("OutputType")?.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? false;
			var isWasm = projectInstance.GetPropertyValue("WasmHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

			return isAndroidApp
				|| (isiOSApp && isExe)
				|| (ismacOSApp && isExe)
				|| isWasm;
		}
	}
}

