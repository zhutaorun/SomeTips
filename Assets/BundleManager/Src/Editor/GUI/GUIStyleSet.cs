using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

internal class GUIStyleSet : ScriptableObject
{
	public List<Texture2D> icons = new List<Texture2D>();
	public List<GUIStyle> styles = new List<GUIStyle>();
	public List<GUIStyle> freeStyles = new List<GUIStyle>();
}
