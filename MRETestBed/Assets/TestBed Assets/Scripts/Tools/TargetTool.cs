// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public class TargetTool : Tool
	{
		private GrabTool _grabTool = new GrabTool();
		private TargetBehavior _currentTargetBehavior;

		public GameObject Target { get; private set; }

		public bool TargetGrabbed => _grabTool.GrabActive;

		protected Vector3? CurrentTargetPoint { get; private set; }

		public TargetTool()
		{
			_grabTool.GrabStateChanged += OnGrabStateChanged;
		}

		public override void CleanUp()
		{
			_grabTool.GrabStateChanged -= OnGrabStateChanged;
		}

		public override void OnToolHeld(InputSource inputSource)
		{
			base.OnToolHeld(inputSource);

			Vector3? hitPoint;
			var newTarget = FindTarget(inputSource, out hitPoint);
			if (newTarget == null)
			{
				return;
			}

			var newBehavior = newTarget.GetBehavior<TargetBehavior>();
			var mwUser = newBehavior.GetMWUnityUser(inputSource.UserGameObject);
			if (mwUser != null)
			{
				newBehavior.Context.StartTargeting(mwUser, hitPoint.Value);
			}

			CurrentTargetPoint = hitPoint.Value;
			OnTargetChanged(null, newTarget, inputSource);
			Target = newTarget;
			_currentTargetBehavior = newBehavior;
		}

		public override void OnToolDropped(InputSource inputSource)
		{
			base.OnToolDropped(inputSource);

			CurrentTargetPoint = null;
			Target = null;
			_currentTargetBehavior = null;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			if (_currentTargetBehavior?.Grabbable ?? false)
			{
				_grabTool.Update(inputSource, Target);
				if (_grabTool.GrabActive)
				{
					// If a grab is active, nothing should change about the current target.
					return;
				}
			}
			
			var mwUser = _currentTargetBehavior.GetMWUnityUser(inputSource.UserGameObject);
			if (mwUser == null)
			{
				return;
			}

			Vector3? hitPoint;
			var newTarget = FindTarget(inputSource, out hitPoint);
			if (Target == newTarget)
			{
				CurrentTargetPoint = hitPoint;
				_currentTargetBehavior.Context.UpdateTargetPoint(mwUser, CurrentTargetPoint.Value);
				return;
			}

			if (Target != null && _currentTargetBehavior != null)
			{
				if (mwUser != null)
				{
					_currentTargetBehavior.Context.EndTargeting(mwUser, CurrentTargetPoint.Value);
				}
			}

			TargetBehavior newBehavior = null;
			if (newTarget != null)
			{
				newBehavior = newTarget.GetBehavior<TargetBehavior>();

				if (newBehavior.GetDesiredToolType() != inputSource.CurrentTool.GetType())
				{
					inputSource.HoldTool(newBehavior.GetDesiredToolType());
				}
				else
				{
					if (mwUser != null)
					{
						newBehavior.Context.StartTargeting(mwUser, hitPoint.Value);
					}

					CurrentTargetPoint = hitPoint.Value;
					OnTargetChanged(Target, newTarget, inputSource);
					Target = newTarget;
					_currentTargetBehavior = newBehavior;
				}
			}
		}

		protected virtual void OnTargetChanged(GameObject oldTarget, GameObject newTarget, InputSource inputSource)
		{

		}

		protected virtual void OnGrabStateChanged(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{

		}

		private void OnGrabStateChanged(object sender, GrabStateChangedArgs args)
		{
			OnGrabStateChanged(args.OldGrabState, args.NewGrabState, args.InputSource);
		}

		private GameObject FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			RaycastHit hitInfo;
			var gameObject = inputSource.gameObject;
			hitPoint = null;

			// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
			// You still want to hit all layers, but only interact with these.
			int layerMask = (1 << 0) | (1 << 5) | (1 << 10);

			if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward, out hitInfo, Mathf.Infinity))
			{
				for (var transform = hitInfo.transform; transform; transform = transform.parent)
				{
					if (transform.GetComponents<TargetBehavior>().FirstOrDefault() != null
						&& ((1 << transform.gameObject.layer) | layerMask) != 0)
					{
						hitPoint = hitInfo.point;
						return transform.gameObject;
					}
				}
			}

			return null;
		}

		void OnDestroy()
		{
			_grabTool.Dispose();
		}
	}
}
