// Based off the ingame class 'VRUIListItemTemplate'
using System;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace ModLoader
{
    class ListItemTemplate : MonoBehaviour
    {
		private TextMeshProUGUI text;
		private int index;
		private Action<int> onClickAction;

		public void Setup(string label, int index, Action<int> onClickAction)
		{
			text = GetComponentInChildren<TextMeshProUGUI>();
			text.text = label;
			this.index = index;
			this.onClickAction = onClickAction;
			AddInteract();
		}

		private void AddInteract()
		{
			VRInteractable interactable = transform.GetChild(1).gameObject.AddComponent<VRInteractable>();
			interactable.interactableName = text.text;
			interactable.OnInteract = new UnityEvent();
			interactable.OnInteract.AddListener(OnClick);
		}

		public void OnClick()
		{
			onClickAction?.Invoke(index);
		}
	}
}
