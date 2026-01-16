/*
Copyright (C) 2018 Wampa842

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace RailRoadCrossing
{
	public class RailwayCommand : ConsoleCommand
	{
		public override string Name => "rrc";
		public override string Help => "railroad crossing debug command";
		private readonly RailRoadCrossing _mod;

		public RailwayCommand(RailRoadCrossing mod)
		{
			this._mod = mod;
        }
        public override void Run(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
				return;
            }
            try
            {
				switch (args[0].ToLower())
				{
					case "down":
					case "d":
						foreach (var o in _mod.Signs)
							o.GetComponent<CrossingBehaviour>().Lower();
						break;

					case "up":
					case "u":
						foreach (var o in _mod.Signs)
							o.GetComponent<CrossingBehaviour>().Raise();
                        break;

                    case "go":
					case "g":
						if (args.Length < 2 || !int.TryParse(args[1], out int n))
						{
							ModConsole.Print("Must enter a number (0-5)");
							return;
						}
						if (_mod.SignPos.ElementAtOrDefault(n) == default)
						{
							ModConsole.Print("Number out of range");
							return;
						}
						GameObject.Find("PLAYER").transform.position = _mod.SignPos[n];
						break;

					case "help":
					case "h":
						PrintHelp();
						break;
					default:
						ModConsole.Print($"Unknown command: {args[0]}");
						PrintHelp();
						break;
				}
            }
            catch (Exception ex)
            {
                ModConsole.Error(ex.ToString());
            }
        }
		private void PrintHelp()
        {
            ModConsole.Print("Railroad crossing debug commands\nhelp|h: show this help\ndown|d: lower all barriers\nup|u: raise all barriers\ngo|g (0-5): teleport to specified sign");
		}
    }

    public class RailRoadCrossing : Mod
	{
		public override string ID => "RailRoadCrossing";
		public override string Name => "Railroad crossing";
		public override string Author => "Wampa842 / JimmyKing";
		public override string Version => "2.0.1";
		public override Game SupportedGames => Game.MySummerCar | Game.MyWinterCar;

		static SettingsCheckBox Verbose;
        static SettingsCheckBox ShowTriggers;

        static SettingsCheckBox EnableSound;
        static SettingsCheckBox EnableBarrier;
		static SettingsCheckBox Breakable;
        
		public GameObject Parent;
		public GameObject[] Signs;
		public Vector3[] SignPos => new Vector3[]
		{
			new Vector3(1960.87f, -1.4f, -80.48f),
			new Vector3(1966.07f, -1.1f, -87.48f),
			new Vector3(1004.8f, -1.6f, -733f),
			new Vector3(1014.8f, -1.4f, -741f),
			new Vector3(237.409f, -1.4f, -1262.977f),
			new Vector3(251.109f, -1.1f, -1261.577f)
		};
		public Quaternion[] SignRot => new Quaternion[]
		{
			Quaternion.Euler(0.0f, 355.0f, 0.0f),
			Quaternion.Euler(0.0f, 175.0f, 0.0f),
			Quaternion.Euler(0.0f, 336.0f, 0.0f),
			Quaternion.Euler(0.0f, 156.0f, 0.0f),
			Quaternion.Euler(0.0f, 290.0f, 0.0f),
			Quaternion.Euler(0.0f, 107.0f, 0.0f)
		};
		public Vector3[] TriggerPos => new Vector3[]
		{
			new Vector3(1963.67f, 1.0f, -83.48f),
			new Vector3(1014f, 1.0f, -734.6998f),
			new Vector3(244.109f, 1.0f, -1262.577f)
		};
		public Quaternion TriggerRot => Quaternion.Euler(0.0f, 145.57f, 0.0f);

		public RailRoadCrossing()
		{
			// Sorry about this
			CrossingTriggerBehaviour.Mod = this;
		}

		private void UpdateSettings()
		{
			foreach(var o in GameObject.FindObjectsOfType<GameObject>().Where(e => e.GetComponent<CrossingBehaviour>() != null))
			{
				o.GetComponent<CrossingBehaviour>().UpdateSettings((bool)EnableSound.GetValue(), (bool)EnableBarrier.GetValue(), (bool)Breakable.GetValue());
			}
		}
		private void UpdateDebugSettings()
		{
			foreach (var o in GameObject.FindObjectsOfType<GameObject>().Where(e => e.GetComponent<CrossingTriggerBehaviour>() != null))
			{
				o.GetComponent<Renderer>().enabled = (bool)ShowTriggers.GetValue();
				o.GetComponent<CrossingTriggerBehaviour>().Verbose = (bool)Verbose.GetValue();
			}
		}
		public override void ModSetup()
		{
			SetupFunction(Setup.OnLoad, Mod_OnLoad);
			SetupFunction(Setup.ModSettings, Mod_Settings);
		}

		private void Mod_Settings()
        {
			// Create settings
			Verbose = Settings.AddCheckBox("VerboseLogging", "[DEBUG] Verbose logging", false, UpdateDebugSettings);
            ShowTriggers = Settings.AddCheckBox("ShowTriggers", "[DEBUG] Show triggers", false, UpdateDebugSettings);
			Settings.AddText("Anything below here won't update during gameplay and will only update during game load so if you made any changes, you have to reload the save");
			EnableSound = Settings.AddCheckBox("EnableSound", "Enable sounds", true);
            EnableBarrier = Settings.AddCheckBox("EnableBarrier", "Use barrier", true, EnableBarrierAction);
			Breakable = Settings.AddCheckBox("BreakableBarrier", "Breakable barriers", true);
			//Settings.AddButton(this, ApplySettings, "MSCLoader settings are absolute bollocks.");
		}

		private void EnableBarrierAction()
        {
            Breakable.SetVisibility(EnableBarrier.GetValue());
		}

        private void Mod_OnLoad()
		{
			// Hide the originals
			foreach (var o in GameObject.FindObjectsOfType<GameObject>().Where(e => e.name == "sign_railroad"))
			{
				o.transform.parent.gameObject.SetActive(false);
			}

			// Load original
			AssetBundle ab = LoadAssets.LoadBundle("RailRoadCrossing.railway_crossing.unity3d");
			GameObject original = ab.LoadAsset<GameObject>("railway_crossing.prefab");
			original.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

			// Parent object
			Parent = new GameObject();
			Parent.name = "RAILWAY_SIGNS";

			// Signs
			Signs = new GameObject[6];
			for (int i = 0; i < 6; ++i)
			{
				Signs[i] = GameObject.Instantiate<GameObject>(original);
				Signs[i].name = "railway_crossing_" + i.ToString();
				Signs[i].transform.position = SignPos[i];
				Signs[i].transform.rotation = SignRot[i];
				Signs[i].transform.SetParent(Parent.transform, true);
				Signs[i].AddComponent<CrossingBehaviour>();
			}
			ConsoleCommand.Add(new RailwayCommand(this));

			// Triggers - enter
			GameObject triggerEnter1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			triggerEnter1.name = "railway_enter_trigger_1";
			triggerEnter1.GetComponent<Collider>().isTrigger = true;
			triggerEnter1.transform.SetParent(Parent.transform, true);
			triggerEnter1.transform.position = TriggerPos[0];
			triggerEnter1.transform.rotation = TriggerRot;
			triggerEnter1.transform.localScale = new Vector3(500.0f, 5.0f, 4.0f);

			GameObject triggerEnter2 = GameObject.Instantiate<GameObject>(triggerEnter1);
			triggerEnter2.name = "railway_enter_trigger_2";
			triggerEnter2.transform.position = TriggerPos[1];

			GameObject triggerEnter3 = GameObject.Instantiate<GameObject>(triggerEnter1);
			triggerEnter3.name = "railway_enter_trigger_3";
			triggerEnter3.transform.position = TriggerPos[2];

			triggerEnter1.AddComponent<CrossingEnterTriggerBehaviour>().Signs = new GameObject[] { Signs[0], Signs[1] };
			triggerEnter2.AddComponent<CrossingEnterTriggerBehaviour>().Signs = new GameObject[] { Signs[2], Signs[3] };
			triggerEnter3.AddComponent<CrossingEnterTriggerBehaviour>().Signs = new GameObject[] { Signs[4], Signs[5] };

			// Triggers - exit
			GameObject triggerExit1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			triggerExit1.name = "railway_exit_trigger_1";
			triggerExit1.GetComponent<Collider>().isTrigger = true;
			triggerExit1.transform.SetParent(Parent.transform, true);
			triggerExit1.transform.position = TriggerPos[0];
			triggerExit1.transform.rotation = TriggerRot;
			triggerExit1.transform.localScale = new Vector3(300.0f, 5.0f, 4.0f);

			GameObject triggerExit2 = GameObject.Instantiate<GameObject>(triggerExit1);
			triggerExit2.name = "railway_exit_trigger_2";
			triggerExit2.transform.position = TriggerPos[1];

			GameObject triggerExit3 = GameObject.Instantiate<GameObject>(triggerExit1);
			triggerExit3.name = "railway_exit_trigger_3";
			triggerExit3.transform.position = TriggerPos[2];

			triggerExit1.AddComponent<CrossingExitTriggerBehaviour>().Signs = new GameObject[] { Signs[0], Signs[1] };
			triggerExit2.AddComponent<CrossingExitTriggerBehaviour>().Signs = new GameObject[] { Signs[2], Signs[3] };
			triggerExit3.AddComponent<CrossingExitTriggerBehaviour>().Signs = new GameObject[] { Signs[4], Signs[5] };

			// Apply settings
			UpdateSettings();
            UpdateDebugSettings();
            
			// Unload assets
            GameObject.Destroy(original);
			ab.Unload(false);
		}
	}
}
