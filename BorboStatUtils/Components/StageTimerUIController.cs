using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils.Components
{
    public class StageTimerUIController : MonoBehaviour
    {
        public RunTimerUIController otherTimerUiController;
        public TimerText timerTextController;
		private HGTextMeshProUGUI hgtextMeshProUGUI;
		const float verticalOffset = 5.5f;
		bool isEnabled = false;
		private void Start()
		{
			if (this.timerTextController && this.timerTextController.TryGetComponent(out HGTextMeshProUGUI tmp))
			{
				this.hgtextMeshProUGUI = tmp;
				Run.instance.GetStageStopwatch(out bool isFirstStage);
				SetBothTimerPositions(isFirstStage);
			}
		}

		private void Update()
		{
			double seconds = 0;
			bool isFirstStage = false;
			if (Run.instance)
				seconds = Run.instance.GetStageStopwatch(out isFirstStage);

			SetBothTimerPositions(isFirstStage);

			if (this.timerTextController)
			{
				this.timerTextController.seconds = seconds;
				return;
			}
		}

		void SetBothTimerPositions(bool hideStageStopwatch)
        {
			float offset = hideStageStopwatch ? 0 : verticalOffset;
			isEnabled = hideStageStopwatch == false;
			hgtextMeshProUGUI.enabled = isEnabled;

			Vector3 pos = otherTimerUiController.transform.localPosition;
			otherTimerUiController.transform.localPosition = new Vector3(pos.x, 0 + offset, pos.z);
			pos = transform.localPosition;
			transform.localPosition = new Vector3(pos.x, -14.5f+offset, pos.z);
		}
	}
}
