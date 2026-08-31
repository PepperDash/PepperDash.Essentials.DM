using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.DM.Config;
using PepperDash.Essentials.DM.Routing;

namespace PepperDash.Essentials.DM.Chassis
{
	[Description("Wrapper class for HD-MD-NxM-4KZ-E switchers")]
	public class HdMdNxM4kzEController : CrestronGenericBridgeableBaseDevice, IRoutingMidpointWithFeedback, IHasNamedRoutingSlots, IHasFeedback
	{
		private readonly HdMdNxM4kzE _chassis;

		public event EventHandler<RoutingNumericEventArgs> NumericSwitchChange;

		// Named-slot view over InputPorts/OutputPorts for IHasNamedRoutingSlots, fed from the same
		// switch-change feedback as CurrentRoutes.
		private RoutingPortNamedSlots _namedSlots;

		IReadOnlyDictionary<string, IRoutingSlotInfo> IHasNamedRoutingSlots.InputSlots =>
			_namedSlots?.InputSlots ?? new Dictionary<string, IRoutingSlotInfo>();
		IReadOnlyDictionary<string, IRoutingOutputSlotInfo> IHasNamedRoutingSlots.OutputSlots =>
			_namedSlots?.OutputSlots ?? new Dictionary<string, IRoutingOutputSlotInfo>();

		public Dictionary<uint, string> InputNames { get; set; }
		public Dictionary<uint, string> OutputNames { get; set; }

		public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
		public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

		public FeedbackCollection<BoolFeedback> VideoInputSyncFeedbacks { get; private set; }
		public FeedbackCollection<IntFeedback> VideoOutputRouteFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> InputNameFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> OutputNameFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> OutputRouteNameFeedbacks { get; private set; }
		public FeedbackCollection<BoolFeedback> InputHdcpEnableFeedback { get; private set; }
		public StringFeedback DeviceNameFeedback { get; private set; }
		public BoolFeedback AutoRouteFeedback { get; private set; }

		#region Constructor

		public HdMdNxM4kzEController(string key, string name, HdMdNxM4kzE chassis,
			HdMdNxM4kzEPropertiesConfig props)
			: base(key, name, chassis)
		{
			_chassis = chassis;
			Name = name;

			if (props == null)
			{
				Debug.LogDebug(this, "HdMdNxM4kzEController properties are null, failed to build the device");
				return;
			}

			InputNames = props.Inputs ?? new Dictionary<uint, string>();
			OutputNames = props.Outputs ?? new Dictionary<uint, string>();

			DeviceNameFeedback = new StringFeedback(() => Name);
			AutoRouteFeedback = new BoolFeedback(() => _chassis.AutoRouteOnFeedback.BoolValue);

			VideoInputSyncFeedbacks = new FeedbackCollection<BoolFeedback>();
			VideoOutputRouteFeedbacks = new FeedbackCollection<IntFeedback>();
			InputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputRouteNameFeedbacks = new FeedbackCollection<StringFeedback>();
			InputHdcpEnableFeedback = new FeedbackCollection<BoolFeedback>();

			InputPorts = new RoutingPortCollection<RoutingInputPort>();
			OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			for (uint i = 1; i <= _chassis.NumberOfInputs; i++)
			{
				var index = i;
				if (!InputNames.ContainsKey(index))
					InputNames[index] = string.Format("Input {0}", index);

				var inputName = InputNames[index];
				_chassis.HdmiInputs[index].Name.StringValue = inputName;

				InputPorts.Add(new RoutingInputPort(inputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, _chassis.HdmiInputs[index], this)
				{
					FeedbackMatchObject = _chassis.HdmiInputs[index]
				});

				VideoInputSyncFeedbacks.Add(new BoolFeedback(inputName,
					() => _chassis.HdmiInputs[index].VideoDetectedFeedback.BoolValue));

				InputNameFeedbacks.Add(new StringFeedback(inputName, () => InputNames[index]));

				InputHdcpEnableFeedback.Add(new BoolFeedback(inputName,
					() => _chassis.HdmiInputs[index].HdmiInputPort.HdcpSupportOnFeedback.BoolValue));
			}

			for (uint i = 1; i <= _chassis.NumberOfOutputs; i++)
			{
				var index = i;
				if (!OutputNames.ContainsKey(index))
					OutputNames[index] = string.Format("Output {0}", index);

				var outputName = OutputNames[index];

				OutputPorts.Add(new RoutingOutputPort(outputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, _chassis.HdmiOutputs[index], this)
				{
					FeedbackMatchObject = _chassis.HdmiOutputs[index]
				});

				VideoOutputRouteFeedbacks.Add(new IntFeedback(outputName,
					() => _chassis.HdmiOutputs[index].VideoOutFeedback == null
						? 0
						: (int)_chassis.HdmiOutputs[index].VideoOutFeedback.Number));

				OutputNameFeedbacks.Add(new StringFeedback(outputName, () => OutputNames[index]));

				OutputRouteNameFeedbacks.Add(new StringFeedback(outputName,
					() => _chassis.HdmiOutputs[index].VideoOutFeedback == null
						? string.Empty
						: _chassis.HdmiOutputs[index].VideoOutFeedback.NameFeedback.StringValue));
			}

			_chassis.DMInputChange += Chassis_DMInputChange;
			_chassis.DMOutputChange += Chassis_DMOutputChange;

			_namedSlots = new RoutingPortNamedSlots(InputPorts, OutputPorts);

			AddPostActivationAction(AddFeedbackCollections);
		}

		#endregion

		#region Methods

		private void OnSwitchChange(RoutingNumericEventArgs e)
		{
			var newEvent = NumericSwitchChange;
			if (newEvent != null) newEvent(this, e);
			UpdateCurrentRoute(e);
		}

		#region IRoutingMidpointWithFeedback Members

		/// <summary>
		/// Currently active routes, per IRoutingMidpointWithFeedback. Maintained from the device's
		/// switch-change feedback (see UpdateCurrentRoute / OnSwitchChange).
		/// </summary>
		public List<RouteSwitchDescriptor> CurrentRoutes { get; } = new List<RouteSwitchDescriptor>();

		/// <summary>
		/// Raised when a route changes, per IRoutingMidpointWithFeedback.
		/// </summary>
		public event RouteChangedEventHandler RouteChanged;

		/// <summary>
		/// Clears the route to an output by switching a null input (no source) to it.
		/// </summary>
		public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
		{
			ExecuteSwitch(null, outputSelector, signalType);
		}

		/// <summary>
		/// Maintains <see cref="CurrentRoutes"/> and raises <see cref="RouteChanged"/> from a numeric
		/// switch-change event so the feedback surface tracks the same routes as NumericSwitchChange.
		/// </summary>
		private void UpdateCurrentRoute(RoutingNumericEventArgs e)
		{
			if (e == null || e.OutputPort == null)
				return;

			CurrentRoutes.RemoveAll(r => ReferenceEquals(r.OutputPort, e.OutputPort));

			var descriptor = new RouteSwitchDescriptor(e.OutputPort, e.InputPort);
			if (e.InputPort != null)
				CurrentRoutes.Add(descriptor);

			_namedSlots?.HandleRouteChange(e.OutputPort, e.InputPort, e.SigType);

			var handler = RouteChanged;
			handler?.Invoke(this, descriptor);
		}

		/// <summary>
		/// Seeds <see cref="CurrentRoutes"/> (and raises <see cref="RouteChanged"/>) for every output's
		/// currently-routed input, mirroring what <see cref="Chassis_DMOutputChange"/> does on a live route
		/// change. Without this, a route already established on the hardware before Essentials started (or
		/// before this chassis reconnected) would never be reflected in the IRoutingMidpointWithFeedback
		/// surface - CurrentRoutes would stay empty until the route actually changed again, which is what
		/// makes the device appear to have no current route on the devtools Routing page.
		/// </summary>
		private void SyncCurrentRoutes()
		{
			for (uint i = 1; i <= _chassis.NumberOfOutputs; i++)
			{
				if (!OutputNames.ContainsKey(i)) continue;

				var inputNumber = _chassis.HdmiOutputs[i].VideoOutFeedback == null
					? 0
					: _chassis.HdmiOutputs[i].VideoOutFeedback.Number;

				var inPort = InputPorts.FirstOrDefault(
					p => p.FeedbackMatchObject == _chassis.HdmiOutputs[i].VideoOutFeedback);
				var outPort = OutputPorts.FirstOrDefault(
					p => p.FeedbackMatchObject == _chassis.HdmiOutputs[i]);

				OnSwitchChange(new RoutingNumericEventArgs(i, inputNumber, outPort, inPort, eRoutingSignalType.AudioVideo));
			}
		}

		#endregion

		public void EnableHdcp(uint port)
		{
			if (port <= 0 || port > _chassis.NumberOfInputs) return;

			_chassis.HdmiInputs[port].HdmiInputPort.HdcpSupportOn();
			InputHdcpEnableFeedback[InputNames[port]].FireUpdate();
		}

		public void DisableHdcp(uint port)
		{
			if (port <= 0 || port > _chassis.NumberOfInputs) return;

			_chassis.HdmiInputs[port].HdmiInputPort.HdcpSupportOff();
			InputHdcpEnableFeedback[InputNames[port]].FireUpdate();
		}

		public void EnableAutoRoute()
		{
			_chassis.AutoRouteOn();
			AutoRouteFeedback.FireUpdate();
		}

		public void DisableAutoRoute()
		{
			_chassis.AutoRouteOff();
			AutoRouteFeedback.FireUpdate();
		}

		#region PostActivate

		private void AddFeedbackCollections()
		{
			AddFeedbackToList(DeviceNameFeedback);
			AddFeedbackToList(AutoRouteFeedback);

			foreach (var fb in VideoInputSyncFeedbacks)
				AddFeedbackToList(fb);
			foreach (var fb in InputHdcpEnableFeedback)
				AddFeedbackToList(fb);
			foreach (var fb in VideoOutputRouteFeedbacks)
				AddFeedbackToList(fb);
			foreach (var fb in InputNameFeedbacks)
				AddFeedbackToList(fb);
			foreach (var fb in OutputNameFeedbacks)
				AddFeedbackToList(fb);
			foreach (var fb in OutputRouteNameFeedbacks)
				AddFeedbackToList(fb);
		}

		private void AddFeedbackToList(PepperDash.Essentials.Core.Feedback newFb)
		{
			if (newFb == null) return;

			// Feedbacks.Contains(newFb) checks by reference (FeedbackCollection<T> derives from
			// Collection<T>, whose default Contains is reference-equality), which never catches a
			// *different* Feedback instance that happens to share the same Key as one already added
			// (e.g. VideoInputSyncFeedbacks and InputHdcpEnableFeedback both keyed by the same input
			// name) - that duplicate key would throw when merged into this shared collection. Check by
			// key via the collection's own indexer instead.
			if (string.IsNullOrEmpty(newFb.Key) || Feedbacks[newFb.Key] == null)
			{
				Feedbacks.Add(newFb);
			}
		}

		#endregion

		#region IRouting Members

		public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
		{
			// Selector may be the port's own Selector object or, from mobile control's matrix
			// routing, the named slot key (= port key). See RoutingSelectorResolver.
			var input = RoutingSelectorResolver.Resolve<HdMdNxM4kzEHdmiInput>(inputSelector, InputPorts);
			var output = RoutingSelectorResolver.Resolve<HdMdNxM4kzEHdmiOutput>(outputSelector, OutputPorts);

			Debug.LogVerbose(this, "ExecuteSwitch: input={0} output={1}", input, output);

			if (output == null)
			{
				Debug.LogInformation(this, "Unable to make switch. Output selector is not HdMdNxM4kzEHdmiOutput");
				return;
			}

			var current = output.VideoOut;
			if (current != input)
				output.VideoOut = input;
		}

		#endregion

		#region IRoutingNumeric Members

		public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
		{
			var input = inputSelector == 0 ? null : _chassis.HdmiInputs[inputSelector];
			var output = _chassis.HdmiOutputs[outputSelector];

			Debug.LogVerbose(this, "ExecuteNumericSwitch: input={0} output={1}", input, output);

			ExecuteSwitch(input, output, signalType);
		}

		#endregion

		#endregion

		#region Bridge Linking

		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new HdMdNxM4kEControllerJoinMap(joinStart);

			var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

			if (!string.IsNullOrEmpty(joinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<HdMdNxM4kEControllerJoinMap>(joinMapSerialized);

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}
			else
			{
				Debug.LogInformation(this, "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
			}

			IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			DeviceNameFeedback.LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber]);

			trilist.SetSigTrueAction(joinMap.EnableAutoRoute.JoinNumber, EnableAutoRoute);
			trilist.SetSigFalseAction(joinMap.EnableAutoRoute.JoinNumber, DisableAutoRoute);
			AutoRouteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnableAutoRoute.JoinNumber]);

			for (uint i = 1; i <= _chassis.NumberOfInputs; i++)
			{
				var joinIndex = i - 1;
				var input = i;

				VideoInputSyncFeedbacks[InputNames[input]].LinkInputSig(
					trilist.BooleanInput[joinMap.InputSync.JoinNumber + joinIndex]);

				InputHdcpEnableFeedback[InputNames[input]].LinkInputSig(
					trilist.BooleanInput[joinMap.EnableInputHdcp.JoinNumber + joinIndex]);
				InputHdcpEnableFeedback[InputNames[input]].LinkComplementInputSig(
					trilist.BooleanInput[joinMap.DisableInputHdcp.JoinNumber + joinIndex]);

				trilist.SetSigTrueAction(joinMap.EnableInputHdcp.JoinNumber + joinIndex, () => EnableHdcp(input));
				trilist.SetSigTrueAction(joinMap.DisableInputHdcp.JoinNumber + joinIndex, () => DisableHdcp(input));

				InputNameFeedbacks[InputNames[input]].LinkInputSig(
					trilist.StringInput[joinMap.InputName.JoinNumber + joinIndex]);
			}

			for (uint i = 1; i <= _chassis.NumberOfOutputs; i++)
			{
				var joinIndex = i - 1;
				var output = i;

				VideoOutputRouteFeedbacks[OutputNames[output]].LinkInputSig(
					trilist.UShortInput[joinMap.OutputRoute.JoinNumber + joinIndex]);
				trilist.SetUShortSigAction(joinMap.OutputRoute.JoinNumber + joinIndex,
					(a) => ExecuteNumericSwitch(a, (ushort)output, eRoutingSignalType.AudioVideo));

				OutputNameFeedbacks[OutputNames[output]].LinkInputSig(
					trilist.StringInput[joinMap.OutputName.JoinNumber + joinIndex]);
				OutputRouteNameFeedbacks[OutputNames[output]].LinkInputSig(
					trilist.StringInput[joinMap.OutputRoutedName.JoinNumber + joinIndex]);
			}

			_chassis.OnlineStatusChange += Chassis_OnlineStatusChange;

			trilist.OnlineStatusChange += (d, args) =>
			{
				if (!args.DeviceOnLine) return;
			};
		}

		#endregion

		#region Events

		private void Chassis_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
		{
			IsOnline.FireUpdate();

			if (!args.DeviceOnLine) return;

			foreach (var feedback in Feedbacks)
			{
				feedback.FireUpdate();
			}

			AutoRouteFeedback.FireUpdate();

			SyncCurrentRoutes();
		}

		private void Chassis_DMOutputChange(Switch device, DMOutputEventArgs args)
		{
			if (args.EventId != DMOutputEventIds.VideoOutEventId) return;

			var output = args.Number;

			var inputNumber = _chassis.HdmiOutputs[output].VideoOutFeedback == null
				? 0
				: _chassis.HdmiOutputs[output].VideoOutFeedback.Number;

			if (!OutputNames.ContainsKey(output)) return;

			var outputName = OutputNames[output];
			var feedback = VideoOutputRouteFeedbacks[outputName];

			if (feedback == null) return;

			var inPort = InputPorts.FirstOrDefault(
				p => p.FeedbackMatchObject == _chassis.HdmiOutputs[output].VideoOutFeedback);
			var outPort = OutputPorts.FirstOrDefault(
				p => p.FeedbackMatchObject == _chassis.HdmiOutputs[output]);

			feedback.FireUpdate();
			OutputRouteNameFeedbacks[outputName]?.FireUpdate();

			OnSwitchChange(new RoutingNumericEventArgs(output, inputNumber, outPort, inPort, eRoutingSignalType.AudioVideo));
		}

		private void Chassis_DMInputChange(Switch device, DMInputEventArgs args)
		{
			switch (args.EventId)
			{
				case DMInputEventIds.VideoDetectedEventId:
				{
					Debug.LogDebug(this, "Event ID {0}: Updating VideoInputSyncFeedbacks", args.EventId);
					foreach (var item in VideoInputSyncFeedbacks)
					{
						item.FireUpdate();
					}
					break;
				}
				case DMInputEventIds.InputNameFeedbackEventId:
				case DMInputEventIds.InputNameEventId:
				case DMInputEventIds.NameFeedbackEventId:
				{
					Debug.LogDebug(this, "Event ID {0}: Updating name feedbacks", args.EventId);
					Debug.LogDebug(this, "Input {0} Name {1}", args.Number,
						_chassis.HdmiInputs[args.Number].NameFeedback.StringValue);
					foreach (var item in InputNameFeedbacks)
					{
						item.FireUpdate();
					}
					break;
				}
				default:
				{
					Debug.LogDebug(this, "Unhandled DM Input Event ID {0}", args.EventId);
					break;
				}
			}
		}

		#endregion

		#region Factory

		public class HdMdNxM4kzEControllerFactory : EssentialsPluginDeviceFactory<HdMdNxM4kzEController>
		{
			public HdMdNxM4kzEControllerFactory()
			{
				MinimumEssentialsFrameworkVersion = "3.0.0";
				TypeNames = new List<string>()
				{
					"hdmd4x14kze",
					"hdmd4x24kze",
					"hdmd4x44kze",
					"hdmd8x44kze",
					"hdmd8x84kze"
				};
			}

			public override EssentialsDevice BuildDevice(DeviceConfig dc)
			{
				Debug.LogDebug("Factory Attempting to create new HD-MD-NxM-4KZ-E Device");

				var props = JsonConvert.DeserializeObject<HdMdNxM4kzEPropertiesConfig>(dc.Properties.ToString());

				if (props == null)
				{
					Debug.LogDebug("Factory failed to create HD-MD-NxM-4KZ-E device, properties config was null");
					return null;
				}

				var type = dc.Type.ToLower();
				var control = props.Control;
				var ipid = control.IpIdInt;

				switch (type)
				{
					case "hdmd4x14kze":
						return new HdMdNxM4kzEController(dc.Key, dc.Name,
							new HdMd4x14kzE(ipid, Global.ControlSystem), props);
					case "hdmd4x24kze":
						return new HdMdNxM4kzEController(dc.Key, dc.Name,
							new HdMd4x24kzE(ipid, Global.ControlSystem), props);
					case "hdmd4x44kze":
						return new HdMdNxM4kzEController(dc.Key, dc.Name,
							new HdMd4x44kzE(ipid, Global.ControlSystem), props);
					case "hdmd8x44kze":
						return new HdMdNxM4kzEController(dc.Key, dc.Name,
							new HdMd8x44kzE(ipid, Global.ControlSystem), props);
					case "hdmd8x84kze":
						return new HdMdNxM4kzEController(dc.Key, dc.Name,
							new HdMd8x84kzE(ipid, Global.ControlSystem), props);
					default:
						return null;
				}
			}
		}

		#endregion
	}
}
