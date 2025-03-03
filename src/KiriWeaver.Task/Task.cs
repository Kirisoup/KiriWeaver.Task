using KiriLib.ErrorHandling;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace KiriWeaver.Task;

public abstract class WeaverTask(string taskName) : Microsoft.Build.Utilities.Task
{
	public string TaskName { get; } = taskName;

	[Required]
	public required string InputAssembly { get; set; }

	[Required]
	public required string IntermediateAssembly { get; set; }

	[Required]
	public required string OutputAssembly { get; set; }

	public const string WeaveTrace = "WeaveTrace";

	private string GetInputPath(out string traceFile) {
		var dir = Path.GetDirectoryName(IntermediateAssembly);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

		traceFile = Path.Combine(dir, WeaveTrace);

		if (!File.Exists(traceFile)) {
			File.Create(traceFile).Close();
			return InputAssembly;
		}
		if (!File.ReadAllLines(traceFile).Contains(TaskName)) return IntermediateAssembly;
		File.WriteAllText(traceFile, "");
		return InputAssembly;
	}

	public override bool Execute() 
	{
		var inputPath = GetInputPath(out var traceFile);

		if (!Weave(inputPath).IsOk(out var value, out Exception? error)) {
			Log.LogError($"{nameof(WeaverTask)} {TaskName} failed because: \r\n {error}");
			return false;
		}

		if (value is null) return true;

		using (var weaveResult = value) {
			weaveResult.Write(OutputAssembly);
		}

		File.Copy(OutputAssembly, IntermediateAssembly, overwrite: true);
		File.AppendAllText(traceFile, TaskName + Environment.NewLine);

		return true;
	}

	public abstract Result<AssemblyDefinition?, Exception> Weave(string inputAssembly);
}

