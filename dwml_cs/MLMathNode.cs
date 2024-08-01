using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using mxd.Dwml.Tools;

namespace mxd.Dwml
{
	// Convert oMath element of omml to latex. Named 'oMath2Latex' in Python version.
	internal class MLMathNode : MLNodeBase
	{
		private static readonly HashSet<string> direct_tags = new HashSet<string> { "box", "sSub", "sSup", "sSubSup", "num", "den", "deg", "e" };

		public string Text { get; }

		public MLMathNode(XmlNode oMathNode)
		{
			//mxd. Sanity check...
			if (oMathNode.LocalName != "oMath")
				throw new InvalidDataException($"Expected 'oMath' node, but got '{oMathNode.LocalName}'!");

			Text = FixDoubleBraces(ProcessChildren(oMathNode)).Replace(" }", "}");
		}

		//mxd. Because C# doesn't support named string.Format args and I'm too lazy to write a custom string.Format implementation...
		// {{a}} -> {a}
		// {{a}{b}} -> keep as is
		// PYTHON string.format: {template}		-> value
		// PYTHON string.format: {{template}}	-> {template} !!!
		// PYTHON string.format: {{{template}}}	-> {value}
		private static string FixDoubleBraces(string s)
		{
			int search_start = 0;

			while (true)
			{
				int start = s.IndexOf("{{", search_start, StringComparison.OrdinalIgnoreCase);
				if (start == -1) 
					break;

				int next_start = start + 1;
				int end;
				int level = 0;
				while (true)
				{
					int next = s.IndexOf('{', next_start + 1);

					end = s.IndexOf('}', next_start + 1);
					if (end == -1 || end == s.Length - 1)
						return s;

					if (next != -1 && next < end)
					{
						level++;
						next_start = end + 1;

						continue;
					}

					level--;

					if (level < 1)
						break;
				}

				if (s[end + 1] != '}')
				{
					search_start = end + 2;
					continue;
				}

				// Remove double braces...
				s = s.Remove(end, 1);
				s = s.Remove(start, 1);

				// Move start position...
				search_start = end - 2;
			}

			return s;
		}

		public override string ToString() => Text;

		protected override TeXNode ProcessTag(string tag, XmlNode elm)
		{
			switch (tag)
			{
				case "acc": return DoAcc(elm);
				case "r": return DoR(elm);
				case "bar": return DoBar(elm);
				case "sub": return DoSub(elm);
				case "sup": return DoSup(elm);
				case "f": return DoF(elm);
				case "func": return DoFunc(elm);
				case "fName": return DoFName(elm);
				case "groupChr": return DoGroupChr(elm);
				case "d": return DoD(elm);
				case "rad": return DoRad(elm);
				case "eqArr": return DoEqArr(elm);
				case "limLow": return DoLimLow(elm);
				case "limUpp": return DoLimUpp(elm);
				case "lim": return DoLim(elm);
				case "m": return DoM(elm);
				case "mr": return DoMr(elm);
				case "nary": return DoNary(elm);
				default: return null;
			}
		}

		protected override TeXNode ProcessUnknown(XmlNode elm, string tag)
		{
			if (direct_tags.Contains(tag))
				return new TeXNode(ProcessChildren(elm));

			if (tag.EndsWith("Pr"))
				return new TeXNode(new MLPropertiesNode(elm));

			return null;
		}

		// The Accent function
		private TeXNode DoAcc(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var pr = children["accPr"];
			var latex_str = GetValue(pr.GetAttributeValue("chr"), CHR_DEFAULT["ACC_VAL"], CHR);

			return new TeXNode(string.Format(latex_str, children["e"]));
		}

		// The Bar function
		private TeXNode DoBar(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var pr = children["barPr"];
			var latex_str = GetValue(pr.GetAttributeValue("pos"), POS_DEFAULT["BAR_VAL"], POS);

			return new TeXNode(pr + string.Format(latex_str, children["e"]));
		}

		// The Delimiter object
		private TeXNode DoD(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var pr = children["dPr"];

			var null_val = D_DEFAULT["null"];
			var beg_chr = GetValue(pr.GetAttributeValue("begChr"), D_DEFAULT["left"], T);
			var end_chr = GetValue(pr.GetAttributeValue("endChr"), D_DEFAULT["right"], T);

			var result = D.Replace("{left}",  string.IsNullOrEmpty(beg_chr) ? null_val : EscapeLatex(beg_chr))
						  .Replace("{right}", string.IsNullOrEmpty(end_chr) ? null_val : EscapeLatex(end_chr))
						  .Replace("{text}", children["e"].ToString());

			return new TeXNode(pr + result);
		}

		// The Substript object
		private TeXNode DoSub(XmlNode elm)
		{
			return new TeXNode(string.Format(SUB, ProcessChildren(elm)));
		}

		// The Superstript object
		private TeXNode DoSup(XmlNode elm)
		{
			return new TeXNode(string.Format(SUP, ProcessChildren(elm)));
		}

		// The Fraction object
		private TeXNode DoF(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var pr = children["fPr"];
			var latex_str = GetValue(pr.GetAttributeValue("type"), F_DEFAULT, F);

			string result = latex_str.Replace("{num}", children["num"].ToString())
									 .Replace("{den}", children["den"].ToString());

			return new TeXNode(pr + result);
		}

		// The Function-Apply object (examples: sin cos)
		private TeXNode DoFunc(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var func_name = children["fName"].ToString();

			return new TeXNode(func_name.Replace(FUNC_PLACE, children["e"].ToString()));
		}

		// The func name
		private TeXNode DoFName(XmlNode elm)
		{
			var latex_func_names = new List<string>();
			foreach (var info in ProcessChildrenList(elm))
			{
				var fkey = info.Node.ToString();

				if (info.Tag == "r")
				{
					if (FUNC.ContainsKey(fkey))
						latex_func_names.Add(FUNC[fkey]);
					else
						throw new NotSupportedException($"Not supported func '{fkey}'!");
				}
				else
				{
					latex_func_names.Add(fkey);
				}
			}

			var func_names = string.Join("", latex_func_names);
			return new TeXNode(func_names.Contains(FUNC_PLACE) ? func_names : func_names + FUNC_PLACE);
		}

		// The Group-Character object
		private TeXNode DoGroupChr(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var pr = children["groupChrPr"];
			var latex_str = GetValue(pr.GetAttributeValue("chr"));

			return new TeXNode(pr + string.Format(latex_str, children["e"]));
		}

		// The radical object
		private TeXNode DoRad(XmlNode elm)
		{
			var children = ProcessChildrenDict(elm);
			var text = children["e"].ToString();

			string result;
			if (children.ContainsKey("deg") && !string.IsNullOrEmpty(children["deg"].ToString()))
			{
				var deg = children["deg"].ToString();
				result = RAD.Replace("{deg}", deg).Replace("{text}", text);
			}
			else
			{
				result = RAD_DEFAULT.Replace("{text}", text);
			}
			
			return new TeXNode(result);
		}

		// The Array object
		private TeXNode DoEqArr(XmlNode elm)
		{
			var include = new HashSet<string> { "e" };
			var text = string.Join(BRK, ProcessChildrenList(elm, include).Select(t => t.Node));
			var result = ARR.Replace("{text}", text);
			
			return new TeXNode(result);
		}

		// The Lower-Limit object
		private TeXNode DoLimLow(XmlNode elm)
		{
			var include = new HashSet<string> { "e", "lim" };
			var children = ProcessChildrenDict(elm, include);
			var latex_str = LIM_FUNC[children["e"].ToString()];
			
			if (string.IsNullOrEmpty(latex_str))
				throw new NotSupportedException($"Not supported lim function: '{children["e"]}'!");

			var result = latex_str.Replace("{lim}", children["lim"].ToString());
			return new TeXNode(result);
		}

		// The Upper-Limit object
		private TeXNode DoLimUpp(XmlNode elm)
		{
			var include = new HashSet<string> { "e", "lim" };
			var children = ProcessChildrenDict(elm, include);

			var result = LIM_UPP.Replace("{lim}", children["lim"].ToString())
									  .Replace("{text}", children["e"].ToString());

			return new TeXNode(result);
		}

		// The lower limit of the limLow object and the upper limit of the limUpp function
		private TeXNode DoLim(XmlNode elm)
		{
			var result = ProcessChildren(elm).Replace(LIM_TO[0], LIM_TO[1]);
			return new TeXNode(result);
		}

		// The Matrix object
		private TeXNode DoM(XmlNode elm)
		{
			var rows = new List<string>();
			foreach (var info in ProcessChildrenList(elm))
				if (info.Tag == "mr")
					rows.Add(info.Node.ToString());

			var result = M.Replace("{text}", string.Join(BRK, rows));
			return new TeXNode(result);
		}

		// A single row of the matrix m
		private TeXNode DoMr(XmlNode elm)
		{
			var include = new HashSet<string> { "e" };
			var result = string.Join(ALN, ProcessChildrenList(elm, include).Select(t => t.Node));
			
			return new TeXNode(result);
		}

		// The n-ary object
		private TeXNode DoNary(XmlNode elm)
		{
			var res = new List<string>();
			string bo = "";
			
			foreach (var info in ProcessChildrenList(elm))
			{
				if (info.Tag == "naryPr")
				{
					bo = GetValue(info.Node.GetAttributeValue("chr"), store: CHR_BO);

					//TODO: mxd. VERY unsure, but MSWord seems to assume this as default?
					if (bo == null)
						bo = "\\int";
				}
				else if (info.Tag == "e") //mxd. Wrap equation in {}...
				{
					res.Add($"{{{info.Node}}}");
				}
				else
				{
					res.Add($"{info.Node}");
				}
			}

			return new TeXNode(bo + string.Join("", res));
		}

		// Get text from 'r' element and try convert them to latex symbols
		private TeXNode DoR(XmlNode elm)
		{
			var sb = new StringBuilder();
			var t = elm.GetChildByLocalName("t");
			if (t == null)
				return null;

			//mxd. We need to iterate UTF-8 chars...
			var char_enumerator = StringInfo.GetTextElementEnumerator(t.InnerText);
			while (char_enumerator.MoveNext())
			{
				var s = char_enumerator.GetTextElement();
				sb.Append(T.ContainsKey(s) ? T[s] : s);
			}

			return new TeXNode(EscapeLatex(sb.ToString()));
		}

		private static string EscapeLatex(string str)
		{
			char last = '\0';
			var sb = new StringBuilder();
			str = str.Replace(@"\\", "\\");
			
			foreach (char c in str)
			{
				if (CHARS.Contains(c) && last != BACKSLASH[0])
					sb.Append(BACKSLASH + c);
				else
					sb.Append(c);

				last = c;
			}

			return sb.ToString();
		}

		private static string GetValue(string key, string default_value = null, Dictionary<string, string> store = null)
		{
			if (key == null)
				return default_value;
			
			if (store == null)
				store = CHR;
			
			return store.ContainsKey(key) ? store[key] : key;
		}

	}
}