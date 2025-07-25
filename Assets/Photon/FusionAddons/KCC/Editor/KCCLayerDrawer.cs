namespace Fusion.Addons.KCC.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	[CustomPropertyDrawer(typeof(KCCLayerAttribute))]
	public sealed class KCCLayerDrawer : PropertyDrawer
	{
		// PRIVATE MEMBERS

		private int[]        _layerIDs;
		private GUIContent[] _layerNames;

		// PropertyDrawer INTERFACE

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_layerNames == null)
			{
				List<int>        layerIDs   = new List<int>();
				List<GUIContent> layerNames = new List<GUIContent>();

				for (int i = 0; i < 32; ++i)
				{
					string layerName = LayerMask.LayerToName(i);
					if (string.IsNullOrEmpty(layerName) == false)
					{
						layerIDs.Add(i);
						layerNames.Add(new GUIContent(layerName));
					}
				}

				_layerIDs   = layerIDs.ToArray();
				_layerNames = layerNames.ToArray();
			}

			int storedLayerIndex   = _layerIDs.IndexOf(property.intValue);
			int selectedLayerIndex = EditorGUI.Popup(position, label, storedLayerIndex, _layerNames);

			if (selectedLayerIndex >= 0 && selectedLayerIndex != storedLayerIndex)
			{
				property.intValue = _layerIDs[selectedLayerIndex];

				EditorUtility.SetDirty(property.serializedObject.targetObject);
			}
		}
	}
}
