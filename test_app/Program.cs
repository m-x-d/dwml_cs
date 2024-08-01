using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using mxd.Dwml;

namespace test_app
{
	internal class Program
	{
		private const string TESTS_PATH = "../../tests/";
		
		static void Main(string[] args)
		{
			Console.WriteLine("Dwml test app");

			// Gather test files
			var tests = GetTestPaths(args);
			if (tests.Count == 0)
			{
				Console.WriteLine("No files to test!");
				Console.ReadKey();

				return;
			}

			// Run tests
			int num_errors = RunTests(tests);

			// Done
			Console.WriteLine($"\nDone{(num_errors > 0 ? $" with {num_errors} errors" : "")}! Press any key to quit.");
			Console.ReadKey();
		}

		private static int RunTests(Dictionary<string, string> tests) // <test.xml, test.tex>
		{
			int num_errors = 0;
			
			foreach (var group in tests)
			{
				// Load ooml
				var xmldoc = new XmlDocument();
				xmldoc.Load(group.Key);

				var oMathNodes = FindMathNodes(xmldoc.DocumentElement);

				if (oMathNodes.Count == 0)
				{
					Console.WriteLine($"Failed to find oMath nodes in {group.Key}!");
					continue;
				}

				// Print filename
				Console.WriteLine($"\n{Path.GetFileName(group.Key)}:");

				// Convert nodes
				var tex_list = new List<string>();
				foreach (XmlNode n in oMathNodes)
					tex_list.Add(MLConverter.Convert(n));

				// Load expected result
				var check_list = new List<string>();
				if (!string.IsNullOrEmpty(group.Value))
					check_list = File.ReadAllLines(group.Value).ToList();

				// Compare with expected results
				if (check_list.Count == tex_list.Count)
				{
					for (int i = 0; i < tex_list.Count; i++)
					{
						if (check_list[i] != tex_list[i])
						{
							Console.WriteLine($"Result mismatch:\nexpected:\n{check_list[i]}\nactual:\n{tex_list[i]}");
							num_errors++;
						}
						else
						{
							Console.WriteLine(tex_list[i]);
						}
					}
				}
				else
				{
					if (check_list.Count > 0)
						Console.WriteLine($"Results count mismatch: expected {tex_list.Count}, got {check_list.Count}!");

					foreach (string tex in tex_list)
						Console.WriteLine(tex);
				}
			}

			return num_errors;
		}

		private static Dictionary<string, string> GetTestPaths(string[] args)
		{
			var result = new Dictionary<string, string>();
			
			// Use args...
			if (args.Length > 0)
			{
				foreach (string file in args)
				{
					if (!File.Exists(file))
					{
						Console.WriteLine($"Invalid data path: '{file}'!");
						continue;
					}

					var tex_file = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".tex";
					if (!File.Exists(tex_file))
						tex_file = string.Empty;

					result.Add(file, tex_file);
				}
			}
			else // Use bundled tests...
			{
				var files = Directory.GetFiles(TESTS_PATH, "*.xml");
				
				foreach (var file in files)
				{
					var tex_file = $"{TESTS_PATH}{Path.GetFileNameWithoutExtension(file)}.tex";
					if (!File.Exists(tex_file))
						tex_file = string.Empty;

					result.Add(file, tex_file);
				}
			}

			return result;
		}

		private static List<XmlNode> FindMathNodes(XmlNode n)
		{
			var result = new List<XmlNode>();

			if (n.LocalName == "oMath")
			{
				result.Add(n);
				return result;
			}

			foreach (XmlNode cn in n.ChildNodes)
				result.AddRange(FindMathNodes(cn));

			return result;
		}

	}
}
