using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;

namespace PepperDash_Essentials_DM.Config
{
	public class HdPsXxxPropertiesConfig
	{
		[JsonProperty("control")]
		public ControlPropertiesConfig Control { get; set; }

		[JsonProperty("inputs")]
		public Dictionary<uint, string> Inputs { get; set; } 
		
		[JsonProperty("outputs")]
		public Dictionary<uint, string> Outputs { get; set; }

		// "inputPriorities": "1,4,3,2"
		[JsonProperty("inputPriorities")]
		public string InputPriorities { get; set; }

		/// <summary>
		/// Which HDMI/DM Lite output each analog aux output takes its program audio from, keyed by aux
		/// output number. An aux output has no source selection of its own - it is a mixer whose program
		/// channels are the audio of the outputs - so this states the wiring rather than switching
		/// anything. Defaults to aux N following output N.
		/// "auxAudioFollowsOutput": { "1": 1, "2": 2 }
		/// </summary>
		[JsonProperty("auxAudioFollowsOutput")]
		public Dictionary<uint, uint> AuxAudioFollowsOutput { get; set; }

		/// <summary>
		/// When true, mutes the other program channels on each aux mixer at startup so the aux output
		/// carries only the output named in <see cref="AuxAudioFollowsOutput"/>. Off by default: this mix
		/// is normally set in the device's own web UI, and enabling it writes over that.
		/// </summary>
		[JsonProperty("applyAuxSourceMix")]
		public bool ApplyAuxSourceMix { get; set; }

		public HdPsXxxPropertiesConfig()
		{
			Inputs = new Dictionary<uint, string>();
			Outputs = new Dictionary<uint, string>();
		}
	}
}