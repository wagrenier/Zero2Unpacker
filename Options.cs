using System.Collections.Generic;
using CommandLine;

namespace Zero2Unpacker
{
    public class Options
    {
		[Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
		public IEnumerable<string> InputFiles { get; set; }

		// Omitting long name, defaults to name of property, ie "--verbose"
		[Option(
		  Default = false,
		  HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[Option("stdin",
		  Default = false,
		  HelpText = "Read from stdin")]
		public bool stdin { get; set; }

		[Value(0, MetaName = "offset", HelpText = "File offset.")]
		public long? Offset { get; set; }
	}

	[Verb("add", HelpText = "Add file contents to the index.")]
	public class AddOptions
	{
		//normal options here
	}

	[Verb("commit", HelpText = "Record changes to the repository.")]
	public class CommitOptions
	{
		//commit options here
	}

	[Verb("clone", HelpText = "Clone a repository into a new directory.")]
	public class CloneOptions
	{
		//clone options here
	}
}
