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
using UnityEngine;
using MSCLoader;

namespace RailRoadCrossing
{
	class CrossingBehaviour : MonoBehaviour
	{
		private class PivotBehaviour : MonoBehaviour // Gets added to the pivot
		{
			void OnJointBreak() // Used by the joint
			{
				ModConsole.Print("[RRC] Joint broke on " + transform.parent.name); // Didnt see a point in force when it was always the same
			}
		}
		private enum BarrierStatus { Up, Down, Warning, Rising, Lowering };
		private BarrierStatus _status;
		private float _barrierTargetAngle
		{
			get
			{
				return _status == BarrierStatus.Down || _status == BarrierStatus.Lowering ? -90.0f : 0.0f;
			}
		}
		private float _barrierAngle = 0.0f;
		private float _timer = 0.0f;
		private bool _soundEnabled = true;

		private GameObject _barrier;
		private GameObject _pivot;
		private GameObject _sign;

		private Material _lightRedLMaterial;
		private Material _lightRedRMaterial;
		private Material _lightWhiteMaterial;
		private Material _lightBarrierMaterial;

		private AudioSource[] _bellSounds;
		private AudioSource _bellLoopSound;
		private AudioSource _bellSound;
		private AudioSource _motorSound;
		private struct HingeSettings // Store hinge settings (used for repairing and state change)
		{
			public Vector3 axis;
			public JointSpring spring;
			public bool useSpring;
			public JointLimits limits;
			public bool useLimits;
			public float breakForce;
			public float breakTorque;
			public bool enableCollision;
		}
		private HingeSettings _savedSettings;
		public void Raise()
		{
			_status = BarrierStatus.Rising;
		}
		public void Lower()
		{
			_status = BarrierStatus.Warning;
			_timer = 0.0f;
		}
		public bool RepairJoint()
		{
			// Only repair when theres no hinge
			if (_pivot.GetComponent<HingeJoint>() != null)
			{
				return false;
			}

			// Cancel any velocity
			Rigidbody rb = _barrier.GetComponent<Rigidbody>();
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			// Reset / set barrier position and rotation
			_barrier.transform.position = _pivot.transform.position;
			_barrier.transform.rotation = _pivot.transform.rotation * Quaternion.Euler(-90f, 0f, 0f);

			// Create new hinge with saved settings
			HingeJoint hinge = _pivot.AddComponent<HingeJoint>();
			hinge.axis = _savedSettings.axis;
			hinge.spring = _savedSettings.spring;
			hinge.useSpring = _savedSettings.useSpring;
			hinge.limits = _savedSettings.limits;
			hinge.useLimits = _savedSettings.useLimits;
			hinge.breakForce = _savedSettings.breakForce;
			hinge.breakTorque = _savedSettings.breakTorque;
			hinge.enableCollision = _savedSettings.enableCollision;
			hinge.connectedBody = _barrier.GetComponent<Rigidbody>();

			return true;
		}
		public void UpdateSettings(bool sound, bool barrier, bool breakable) // From RailRoadCrossing.cs
		{
			_soundEnabled = sound;
			_barrier.SetActive(barrier);
			if (breakable && barrier)
			{
				RepairJoint(); // Make the joint
				_barrier.GetComponent<Rigidbody>().isKinematic = false;
				_barrier.transform.parent = gameObject.transform;
			}
			else
			{
				_barrier.GetComponent<Rigidbody>().isKinematic = true;
				_barrier.transform.parent = _pivot.transform;
				_barrier.transform.position = _pivot.transform.position;
				_barrier.transform.rotation = _pivot.transform.rotation * Quaternion.Euler(-90f, 0f, 0f);
				Destroy(_pivot.GetComponent<HingeJoint>()); // Destroy the Joint (not needed)
			}
		}
		void Awake() // Used by Unity when the Crossing is first made
		{
			// Find components
			_barrier = gameObject.transform.FindChild("railway_barrier").gameObject;
			_sign = gameObject.transform.FindChild("railway_sign").gameObject;
			_pivot = gameObject.transform.FindChild("barrier_pivot").gameObject;
			_pivot.AddComponent<PivotBehaviour>();

			// There now no need to create materials when we already have them

			// Get references from existing materials
			_lightRedLMaterial = _sign.transform.FindChild("railway_sign_left").gameObject.GetComponent<Renderer>().material;
			_lightRedRMaterial = _sign.transform.FindChild("railway_sign_right").gameObject.GetComponent<Renderer>().material;
			_lightWhiteMaterial = _sign.transform.FindChild("railway_sign_white").gameObject.GetComponent<Renderer>().material;
			_lightBarrierMaterial = _barrier.transform.FindChild("barrier_light_1").gameObject.GetComponent<Renderer>().material;

			// Assign the same emissive trigger for barrier light 2
			_barrier.transform.FindChild("barrier_light_2").gameObject.GetComponent<Renderer>().material = _lightBarrierMaterial;

			// Store the hinge data from the model
			var hinge = _pivot.GetComponent<HingeJoint>();
			_savedSettings = new HingeSettings
			{
				axis = hinge.axis,
				spring = hinge.spring,
				useSpring = hinge.useSpring,
				limits = hinge.limits,
				useLimits = hinge.useLimits,
				breakForce = hinge.breakForce,
				breakTorque = hinge.breakTorque,
				enableCollision = hinge.enableCollision
			};

			// Find audio sources
			_bellSounds = _sign.transform.FindChild("bell_sounds").GetComponents<AudioSource>(); // Single source rather than 2
			_bellSound = _bellSounds[0];
			_bellLoopSound = _bellSounds[1];
			_motorSound = transform.FindChild("motor_sound").gameObject.GetComponent<AudioSource>();
		}
		void Update() // Unity update function
		{
			// Movement
			if (_status == BarrierStatus.Warning)
			{
				_timer += Time.deltaTime;
				if (_timer >= 3.0f)
				{
					_status = BarrierStatus.Lowering;
				}
			}
			if (_status == BarrierStatus.Rising)
			{
				if (_barrierAngle < _barrierTargetAngle)
				{
					_barrierAngle += 30.0f * Time.deltaTime;
					_pivot.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _barrierAngle);
				}
				else
				{
					_status = BarrierStatus.Up;
				}
			}
			if (_status == BarrierStatus.Lowering)
			{
				if (_barrierAngle > _barrierTargetAngle)
				{
					_barrierAngle -= 30.0f * Time.deltaTime;
					_pivot.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _barrierAngle);
				}
				else
				{
					_status = BarrierStatus.Down;
				}
			}

			// Lights
			bool blink = (Time.time % 1.0f) < 0.5f;
			if (_status == BarrierStatus.Up)
			{
				if (blink)
				{
					_lightWhiteMaterial.EnableKeyword("_EMISSION");
				}
				else
				{
					_lightWhiteMaterial.DisableKeyword("_EMISSION");
				}

				_lightRedLMaterial.DisableKeyword("_EMISSION");
				_lightRedRMaterial.DisableKeyword("_EMISSION");
				_lightBarrierMaterial.DisableKeyword("_EMISSION");
			}
			else
			{
				if (blink)
				{
					_lightRedLMaterial.EnableKeyword("_EMISSION");
					_lightRedRMaterial.DisableKeyword("_EMISSION");
					_lightBarrierMaterial.EnableKeyword("_EMISSION");
				}
				else
				{
					_lightRedLMaterial.DisableKeyword("_EMISSION");
					_lightRedRMaterial.EnableKeyword("_EMISSION");
					_lightBarrierMaterial.DisableKeyword("_EMISSION");
				}

				_lightWhiteMaterial.DisableKeyword("_EMISSION");
			}

			// Sounds
			if ((_status == BarrierStatus.Lowering || _status == BarrierStatus.Rising) && _soundEnabled)
			{
				if (!_motorSound.isPlaying)
				{
					_motorSound.Play();
				}
			}
			else if (_motorSound.isPlaying)
			{
				_motorSound.Stop();
			}

			if ((_status == BarrierStatus.Lowering || _status == BarrierStatus.Warning) && !_bellLoopSound.isPlaying && _soundEnabled)
			{
				_bellLoopSound.Play();
			}

			if (!(_status == BarrierStatus.Lowering || _status == BarrierStatus.Warning) && _bellLoopSound.isPlaying)
			{
				_bellLoopSound.Stop();
				if (_soundEnabled)
					_bellSound.Play();
			}
		}
	}
}
