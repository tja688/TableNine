using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	#if UNITY_EDITOR
	/// <summary>
	/// This class lets you specify (in code, by editing it) symbols that will be added to the build settings' define symbols list automatically
	/// </summary>
	[InitializeOnLoad]
	public class NiceVibrationsDefineSymbols
	{
		/// <summary>
		/// A list of all the symbols you want added to the build settings
		/// </summary>
		public static readonly string[] Symbols = new string[]
		{
			"MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED"
		};

		/// <summary>
		/// As soon as this class has finished compiling, adds the specified define symbols to the build settings
		/// </summary>
		static NiceVibrationsDefineSymbols()
		{
			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			string scriptingDefinesString = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
			List<string> scriptingDefinesStringList = scriptingDefinesString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			bool changed = false;

			foreach (string symbol in Symbols.Except(scriptingDefinesStringList))
			{
				scriptingDefinesStringList.Add(symbol);
				changed = true;
			}

			if (changed)
			{
				PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", scriptingDefinesStringList.ToArray()));
			}
		}
	}
	#endif
}
