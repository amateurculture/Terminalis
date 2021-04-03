using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UMA.Examples;

namespace UMA.CharacterSystem.Examples
{
	public class StTestCustomizerDD : TestCustomizerDD
	{
		public float bodyDistance = 3f;
		public float faceDistance = 1f;
		
		public new void ShowHideWardrobeDropdowns()
		{
			if (wardrobeDropdownPanel.activeSelf)
			{
				wardrobeDropdownPanel.SetActive(false);
			}
			else
			{
				SetUpWardrobeDropdowns();
				if (Orbitor != null)
				{
					TargetBody();
				}
				wardrobeDropdownPanel.SetActive(true);
				colorDropdownPanel.SetActive(false);
				faceEditor.transform.parent.gameObject.SetActive(false);
				bodyEditor.transform.parent.gameObject.SetActive(false);
			}
		}

		public new void ShowHideColorDropdowns()
		{
			if (colorDropdownPanel.activeSelf)
			{
				colorDropdownPanel.SetActive(false);
			}
			else
			{
				SetUpColorDropdowns();
				if (Orbitor != null)
				{
					TargetBody();
				}
				colorDropdownPanel.SetActive(true);
				wardrobeDropdownPanel.SetActive(false);
				faceEditor.transform.parent.gameObject.SetActive(false);
				bodyEditor.transform.parent.gameObject.SetActive(false);
			}
		}

		public new void ShowHideFaceDNA()
		{
			if (faceEditor.transform.parent.gameObject.activeSelf)
			{
				faceEditor.transform.parent.gameObject.SetActive(false);
				if (Orbitor != null)
				{
					TargetBody();
				}
			}
			else
			{
				faceEditor.Initialize(Avatar);
				if (Orbitor != null)
				{
					TargetFace();
				}
				faceEditor.transform.parent.gameObject.SetActive(true);
				bodyEditor.transform.parent.gameObject.SetActive(false);
				colorDropdownPanel.SetActive(false);
				wardrobeDropdownPanel.SetActive(false);
			}
		}

		public new void ShowHideBodyDNA()
		{
			if (bodyEditor.transform.parent.gameObject.activeSelf)
			{
				bodyEditor.transform.parent.gameObject.SetActive(false);
			}
			else
			{
				bodyEditor.Initialize(Avatar);
				if (Orbitor != null)
				{
					TargetBody();
				}
				bodyEditor.transform.parent.gameObject.SetActive(true);
				faceEditor.transform.parent.gameObject.SetActive(false);
				colorDropdownPanel.SetActive(false);
				wardrobeDropdownPanel.SetActive(false);
			}
		}
		
		public new void SetUpColorDropdowns()
		{
			UMA.UMAData umaData = Avatar.umaData;
			var currentColorDropdowns = colorDropdownPanel.transform.GetComponentsInChildren<CSColorChangerDD>(true);
			List<string> activeColorDropdowns = new List<string>();
			//foreach (DynamicCharacterAvatar.ColorValue colorType in Avatar.characterColors.Colors)
			//using new colorvaluestuff
			foreach (OverlayColorData colorType in Avatar.characterColors.Colors)
			{
				if (colorType.name.StartsWith("colorDNA"))
				{
					continue;
				}
				activeColorDropdowns.Add(colorType.name);
				bool dropdownExists = false;
				foreach (CSColorChangerDD colorDropdown in currentColorDropdowns)
				{
					if (colorDropdown.colorToChange == colorType.name)
					{
						dropdownExists = true;
						colorDropdown.gameObject.SetActive(true);
						SetUpColorDropdownValue(colorDropdown, colorType);
						break;
					}
				}
				if (!dropdownExists)
				{
					GameObject thisColorDropdown = Instantiate(colorDropdownPrefab) as GameObject;
					thisColorDropdown.transform.SetParent(colorDropdownPanel.transform, false);
					thisColorDropdown.GetComponent<CSColorChangerDD>().customizerScript = this;
					thisColorDropdown.GetComponent<CSColorChangerDD>().colorToChange = colorType.name;
					thisColorDropdown.name = colorType.name + "DropdownHolder";
					thisColorDropdown.transform.Find("SlotLabel").GetComponent<Text>().text = colorType.name + " Color";
					thisColorDropdown.GetComponent<DropdownWithColor>().onValueChanged.AddListener(thisColorDropdown.GetComponent<CSColorChangerDD>().ChangeColor);
					SetUpColorDropdownValue(thisColorDropdown.GetComponent<CSColorChangerDD>(), colorType);
				}
			}
			foreach (CSColorChangerDD colorDropdown in colorDropdownPanel.transform.GetComponentsInChildren<CSColorChangerDD>())
			{
				bool keepOptionActive = false;
				foreach (UMA.OverlayColorData ucd in umaData.umaRecipe.sharedColors)
				{
					if (colorDropdown.colorToChange == ucd.name)
					{
						keepOptionActive = true;
						break;
					}
				}
				if (!keepOptionActive)
				{
					colorDropdown.gameObject.SetActive(false);
				}
			}
		}

		public new void TargetBody()
		{
			if (Orbitor != null)
			{
				Orbitor.distance = bodyDistance;
				Orbitor.TargetBone = MouseOrbitImproved.targetOpts.Chest;
			}
		}

		public new void TargetFace()
		{
			if (Orbitor != null)
			{
				Orbitor.distance = faceDistance;
				Orbitor.TargetBone = MouseOrbitImproved.targetOpts.Head;
			}
		}
	}
}
