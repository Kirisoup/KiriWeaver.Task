using KiriLib.ErrorHandling;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace KiriWeaver.Task;

public abstract class WeaverTask(string taskName) : Microsoft.Build.Utilities.Task
{
	[Required]
	public required string InputAssembly { get; set; }

	[Required]
	public required string IntermediateAssembly { get; set; }

	[Required]
	public required string OutputAssembly { get; set; }

	public override bool Execute() {
		var input = File.Exists(IntermediateAssembly)
			? IntermediateAssembly
			: InputAssembly;

		if (!Weave(input).IsOk(out var value, out Exception? error)) {
			Log.LogError($"{nameof(WeaverTask)} {taskName} failed because: \r\n {error}");
			return false;
		}

		using (var weaveResult = value) {
			weaveResult.Write(OutputAssembly);
		}

		var dir = Path.GetDirectoryName(IntermediateAssembly);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

		File.Copy(OutputAssembly, IntermediateAssembly, overwrite: true);
		
		return true;
	}

	public abstract Result<AssemblyDefinition?, Exception> Weave(string inputAssembly);
}