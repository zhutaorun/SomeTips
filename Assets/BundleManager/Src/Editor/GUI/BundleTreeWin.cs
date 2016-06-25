using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

internal class BundleTreeWin : EditorWindow 
{
	private Rect m_rect;
		
	private List<string> m_Selections = new List<string>();
	
	private List<string> m_LastTimeShowingBundles = new List<string>();
	private List<string> m_CurrentShowingBundles = new List<string>();
	
	private string m_CurrentRecieving = "";
	private string m_CurrentEditing = "";
	
	// Bundle name edit members
	private string m_EditWaitBundle = "";
	private bool m_EditNeedFocus = true;
	private string m_EditString = "";
	private double m_EditWaitStartTime = -1;
	
	private const string m_EditTextFeildName = "nameTextFeild";
		
	private Dictionary<string, bool> m_BundleFoldDict = new Dictionary<string, bool>();
	
	private Vector2 m_ScrollPos = Vector2.zero;
	
	private const float m_IndentWidth = 22f;
	private const float m_NoToggleIndent = 12f;
	private const float m_ItemHeight = 20f;
	
	private GUIDragHandler m_DragHandler = null;
	
	public string LastSelection()
	{
		if(m_Selections.Count > 0)
			return m_Selections[0];
		else
			return "";
	}
	
	public string lastTimeSelection = "";
	
	public Rect Rect()
	{
		return m_rect;
	}
	
	bool HasFocuse()
	{
		return this == EditorWindow.focusedWindow;
	}
	
	void Update ()
	{
		if(lastTimeSelection != LastSelection())
		{	
			lastTimeSelection = LastSelection();
			BundleEditorDrawer.ShowBundle( BundleManager.GetBundleData(lastTimeSelection) );
		}
		
		if(m_EditWaitBundle != "" && m_EditWaitStartTime > 0)
		{
			// See if we can start edit
			if(EditorApplication.timeSinceStartup - m_EditWaitStartTime > 0.6)
			{
				StartEditBundleName(m_EditWaitBundle);
			}
		}
	}
	
	void OnGUI()
	{
		if(m_DragHandler == null)
		{
			// Setup GUI handler
			m_DragHandler = new GUIDragHandler();
			m_DragHandler.dragIdentifier = "BundleTreeView";
			m_DragHandler.AddRecieveIdentifier(m_DragHandler.dragIdentifier);
			m_DragHandler.canRecieveCallBack = OnCanRecieve;
			m_DragHandler.reciveDragCallBack = OnRecieve;
		}
		
		if( Event.current.type == EventType.MouseDown || Event.current.type == EventType.DragUpdated  || !HasFocuse())
		{
			// Any mouse down msg or lose focuse will cancle the edit waiting process
			m_EditWaitStartTime = -1;
			m_EditWaitBundle = "";
		}
	
		Rect curWindowRect = EditorGUILayout.BeginVertical(BMGUIStyles.GetStyle("OL Box"));
		{
			// Update rect info
			if(Event.current.type != EventType.Layout)
				m_rect = curWindowRect;
			
			// Toobar
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
			{
				// Create drop down
				Rect createBtnRect = GUILayoutUtility.GetRect(new GUIContent("Create"), EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
				if( GUI.Button( createBtnRect, "Create", EditorStyles.toolbarDropDown ) )
				{
					GenericMenu menu = new GenericMenu();
					if(m_Selections.Count <= 1)
					{
						menu.AddItem(new GUIContent("Scene Bundle"), false, CreateSceneBundle);
						menu.AddItem(new GUIContent("Asset Bundle"), false, CreateAssetBundle);
					}
					else
					{
						menu.AddItem(new GUIContent("Scene Bundle"), false, null);
						menu.AddItem(new GUIContent("Asset Bundle"), false, null);
					}
					menu.DropDown(createBtnRect);
				}
				
				// Build button
				Rect buildBtnRect = GUILayoutUtility.GetRect(new GUIContent("Build"), EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
				if( GUI.Button( buildBtnRect, "Build", EditorStyles.toolbarDropDown ) )
				{
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Build Selection"), false, BuildSelection);
					menu.AddItem(new GUIContent("Rebuild Selection"), false, RebuildSelection);
					menu.AddItem(new GUIContent("Build All"), false, BuildAll);
					menu.AddItem(new GUIContent("Rebuild All"), false, RebuildAll);
					menu.AddItem(new GUIContent("Clear"), false, ClearOutputs);
					menu.DropDown(buildBtnRect);
				}
				
				GUILayout.FlexibleSpace();

				if(GUILayout.Button("Settings", EditorStyles.toolbarButton))
					BMSettingsEditor.Show();
			}
			EditorGUILayout.EndHorizontal();
			
			// Tree view
			m_ScrollPos = EditorGUILayout.BeginScrollView (m_ScrollPos);
			{
				m_CurrentShowingBundles.Clear();
				
				foreach(BundleData rootBundle in BundleManager.Roots)
				{
					if(!GUI_TreeItem(0, rootBundle.name))
					{
						Repaint();
						break;
					}
				}
				
				m_LastTimeShowingBundles.Clear();
				m_LastTimeShowingBundles.AddRange(m_CurrentShowingBundles);

				if(m_CurrentEditing == "")
				{
					ArrowKeyProcess();
					HotkeyProcess();
				}

				// Empty space for root selection
				Rect spaceRect = EditorGUILayout.BeginVertical(BMGUIStyles.GetStyle("Space"));
				GUILayout.Space(m_ItemHeight);
				EditorGUILayout.EndVertical();
				RootSpaceProcess(spaceRect);
				
			}EditorGUILayout.EndScrollView();

			Rect scrollViewRect = GUILayoutUtility.GetLastRect();
			if(scrollViewRect.height != 1)
				UpdateScrollBarBySelection(scrollViewRect.height);
			
		}EditorGUILayout.EndVertical();
	}
	
	bool GUI_TreeItem(int indent, string bundleName)
	{
		if(!m_CurrentShowingBundles.Contains(bundleName))
			m_CurrentShowingBundles.Add(bundleName);
		
		BundleData bundleData = BundleManager.GetBundleData(bundleName);
		if(bundleData == null)
		{
			Debug.LogError("Cannot find bundle : " + bundleName);
			return true;
		}
		
		Rect itemRect = GUI_DrawItem(bundleData, indent);

		if(EditProcess(itemRect, bundleName))
			return false;
		
		if( DragProcess(itemRect, bundleName) )
			return false;
		
		SelectProcess(itemRect, bundleName);
		
		RightClickMenu(itemRect);
		
		return GUI_DrawChildren(bundleName, indent);
	}
	
	Rect GUI_DrawItem(BundleData bundle, int indent)
	{
		bool isEditing = m_CurrentEditing == bundle.name;
		bool isRecieving = m_CurrentRecieving == bundle.name;
		bool isSelected = m_Selections.Contains(bundle.name);
		
		GUIStyle currentLableStyle = BMGUIStyles.GetStyle("TreeItemUnSelect");
		if(isRecieving)
			currentLableStyle = BMGUIStyles.GetStyle("receivingLable");
		else if(isSelected && !isEditing)
			currentLableStyle = HasFocuse() ? BMGUIStyles.GetStyle("TreeItemSelectBlue") : BMGUIStyles.GetStyle("TreeItemSelectGray");
		
		Rect itemRect = EditorGUILayout.BeginHorizontal(currentLableStyle);
		
		if(bundle.children.Count == 0)
		{
			GUILayout.Space(m_IndentWidth * indent + m_NoToggleIndent);
		}
		else
		{
			GUILayout.Space(m_IndentWidth * indent);
			bool fold = !GUILayout.Toggle(!IsFold(bundle.name), "", BMGUIStyles.GetStyle("Foldout"));
			SetFold(bundle.name, fold);
		}
		
		GUILayout.Label(bundle.sceneBundle ? BMGUIStyles.GetIcon("sceneBundleIcon") : BMGUIStyles.GetIcon("assetBundleIcon"), BMGUIStyles.GetStyle("BItemLabelNormal"), GUILayout.ExpandWidth(false));
		
		if(!isEditing)
		{
			GUILayout.Label(bundle.name, isSelected ? BMGUIStyles.GetStyle("BItemLabelActive") : BMGUIStyles.GetStyle("BItemLabelNormal"));
		}
		else
		{
			GUI.SetNextControlName(m_EditTextFeildName);
			m_EditString = GUILayout.TextField(m_EditString, BMGUIStyles.GetStyle("TreeEditField"));
		}
		
		EditorGUILayout.EndHorizontal();
		
		return itemRect;
	}
	
	bool GUI_DrawChildren(string bundleName, int indent)
	{
		BundleData bundleData = BundleManager.GetBundleData(bundleName);
		
		if(bundleData.children.Count == 0 || IsFold(bundleName))
			return true;
		
		for(int i = 0; i < bundleData.children.Count; ++i)
		{
			if(!GUI_TreeItem(indent + 1, bundleData.children[i]))
				return false;
		}
		
		return true;
	}
	
	void GUI_DeleteMenuCallback()
	{
		foreach(string bundle in m_Selections)
			BundleManager.RemoveBundle(bundle);
		
		m_Selections.Clear();
		Repaint();
	}

	void ArrowKeyProcess()
	{
		if(m_LastTimeShowingBundles.Count == 0)
			return;

		KeyCode key = Event.current.keyCode;
		if(Event.current.type != EventType.keyDown)
		{
			if(Event.current.isKey && (key == KeyCode.UpArrow || key == KeyCode.DownArrow || key == KeyCode.LeftArrow || key == KeyCode.RightArrow))
				Event.current.Use(); // Prevent the system warning sound
			return;
		}

		if(key == KeyCode.UpArrow || key == KeyCode.DownArrow)
		{
			string lastSelect = "";
			if(m_Selections.Count > 0)
				lastSelect = m_Selections[m_Selections.Count - 1];

			int lastIndex = m_LastTimeShowingBundles.FindIndex(x=> x == lastSelect);
			int newIndex = lastIndex + (key == KeyCode.UpArrow ? - 1 : +1);
			if(newIndex < 0)
				newIndex = 0;
			else if(newIndex >= m_LastTimeShowingBundles.Count)
				newIndex = m_LastTimeShowingBundles.Count - 1;
			
			string newAddBundle = m_LastTimeShowingBundles[newIndex];
			if(Event.current.shift)
			{
				ShiftSelection(newAddBundle);
			}
			else
			{
				m_Selections.Clear();
				m_Selections.Add(newAddBundle);
			}

			Event.current.Use();
			Repaint();
		}
		else if(key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
		{
			foreach(string selectName in m_Selections)
			{
				SetFold(selectName, key == KeyCode.LeftArrow);
			}

			Event.current.Use();
			Repaint();
		}
	}

	void HotkeyProcess()
	{
		bool deletePressed = (Application.platform == RuntimePlatform.OSXEditor && Event.current.keyCode == KeyCode.Backspace) ||
							 (Application.platform == RuntimePlatform.WindowsEditor && Event.current.keyCode == KeyCode.Delete);

		if(deletePressed && Event.current.type == EventType.KeyDown && Control())
		{
			GUI_DeleteMenuCallback();
			Event.current.Use();
			Repaint();
		}
	}
	
	bool EditProcess(Rect itemRect, string bundleName)
	{
		if(m_CurrentEditing == bundleName)
		{
			// Bundle name is in editing
			
			if(m_EditNeedFocus)
			{
				// First time after edit started. Set focuse for the text field control
				GUI.FocusControl(m_EditTextFeildName);
				m_EditNeedFocus = false;
				Repaint();
				return false;
			}
		
			// If lose focus, end this edit
			bool clickOutSideTheTextField = m_CurrentEditing != "" && Event.current.type == EventType.MouseDown && !IsRectClicked(itemRect);
			bool isFinishedEdit = Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return;
			if(!HasFocuse() || clickOutSideTheTextField || isFinishedEdit)
			{
				if(IsNameValid(m_EditString))
					BundleManager.RenameBundle(bundleName, m_EditString);
				else
					Debug.LogWarning(m_EditString + " is not valid for bundle name. Use only characters, numbers, _ and /");
				
				if(Event.current.type == EventType.Layout)
					return false;
				
				m_CurrentEditing = "";	 
				m_EditString = "";
				Repaint();
				GUIUtility.keyboardControl = 0;
				
				return true;
			}
		}
		else if(IsRectClicked(itemRect) && Event.current.button == 0 && m_Selections.Count == 1 && m_Selections[0] == bundleName && !Control() && !Event.current.shift)
		{
			// Try start edit
			m_EditWaitStartTime = EditorApplication.timeSinceStartup;
			m_EditWaitBundle = bundleName;
		}
		
		return false;
	}

	string m_LastNewSelection = "";
	void UpdateScrollBarBySelection(float viewHeight)
	{
		if(m_Selections.Count == 0)
		{
			m_LastNewSelection = "";
			return;
		}

		string newSelection = m_Selections[m_Selections.Count - 1];
		if(newSelection == m_LastNewSelection)
			return;

		m_LastNewSelection = newSelection;

		int selectionRow = m_LastTimeShowingBundles.FindIndex(x=>x == newSelection);
		if(selectionRow < 0)
			return;

		float selectTopOffset = selectionRow * m_ItemHeight;
		if(selectTopOffset < m_ScrollPos.y)
			m_ScrollPos.y = selectTopOffset;
		else if(selectTopOffset + m_ItemHeight > m_ScrollPos.y + viewHeight)
			m_ScrollPos.y = selectTopOffset + m_ItemHeight - viewHeight;

		Repaint();
	}
	
	void StartEditBundleName(string bundleName)
	{
		m_CurrentEditing = bundleName;
		m_EditString = bundleName;
		m_EditNeedFocus = true;
		
		m_EditWaitStartTime = -1;
		m_EditWaitBundle = "";
		
		Repaint();
	}
	
	void SelectProcess(Rect itemRect, string bundleName)
	{
		if(IsRectClicked(itemRect) && m_CurrentEditing != bundleName)
		{
			if(Control())
			{
				if(m_Selections.Contains(bundleName))
					m_Selections.Remove(bundleName);
				else
					m_Selections.Add(bundleName);
			}
			else if(Event.current.shift)
			{
				ShiftSelection(bundleName);
			}
			else if(Event.current.button == 0 || !m_Selections.Contains(bundleName))
			{
				m_Selections.Clear();
				m_Selections.Add(bundleName);
			}
			
			m_CurrentEditing = "";
			Repaint();
		}
	}
	
	void RightClickMenu(Rect itemRect)
	{
		GenericMenu rightClickMenu = new GenericMenu();
		rightClickMenu.AddItem(new GUIContent("Delete"), false, GUI_DeleteMenuCallback);
		if(IsMouseOn(itemRect) && Event.current.type == EventType.MouseUp && Event.current.button == 1)
		{
			Vector2 mousePos = Event.current.mousePosition;
			rightClickMenu.DropDown(new Rect(mousePos.x, mousePos.y, 0, 0));
		}
	}
	
	bool DragProcess(Rect itemRect, string bundleName)
	{
		if( Event.current.type == EventType.Repaint || itemRect.height <= 0)
			return false;
		
		if( !IsMouseOn(itemRect) )
		{
			if( m_CurrentRecieving != "" && m_CurrentRecieving == bundleName )
			{
				m_CurrentRecieving = "";
				Repaint();
			}
			
			return false;
		}
		
		m_DragHandler.detectRect = itemRect;
		m_DragHandler.dragData.customDragData = (object)bundleName;
		m_DragHandler.dragAble = bundleName != "";
		
		var dragState = m_DragHandler.GUIDragUpdate();
		if(dragState == GUIDragHandler.DragState.Receiving)
		{
			m_CurrentRecieving = bundleName;
			Repaint();
		}
		else if(dragState == GUIDragHandler.DragState.Received)
		{
			BundleEditorDrawer.Refresh();
			m_CurrentRecieving = "";
		}
		else if(m_CurrentRecieving == bundleName)
		{
			// Drag cursor leaved
			m_CurrentRecieving = "";
			Repaint();
		}
		
		return dragState == GUIDragHandler.DragState.Received;
	}
	
	void RootSpaceProcess(Rect spaceRect)
	{
		if(IsRectClicked(spaceRect) && !(Control() || Event.current.shift))
		{
			m_Selections.Clear();
			m_CurrentEditing = "";
			Repaint();
			Event.current.Use();
		}
		
		DragProcess(spaceRect, "");
	}
	
	void ShiftSelection(string newSelect)
	{
		if(m_Selections.Count == 0)
		{
			m_Selections.Add(newSelect);
			return;
		}
		
		int minIndex = int.MaxValue;
		int maxIndex = int.MinValue;
		foreach(string bundle in m_Selections)
		{
			int selIndex = m_LastTimeShowingBundles.IndexOf(bundle);
			if(selIndex == -1)
				continue;
			
			if(minIndex > selIndex)
				minIndex = selIndex;
			if(maxIndex < selIndex)
				maxIndex = selIndex;
		}
		
		if(minIndex == int.MaxValue || maxIndex == int.MinValue)
		{
			m_Selections.Add(newSelect);
			return;
		}
		
		int fromIndex = 0;
		int toIndex = m_LastTimeShowingBundles.IndexOf(newSelect);
		if(toIndex >= minIndex && toIndex <= maxIndex)
			fromIndex = m_LastTimeShowingBundles.IndexOf(m_Selections[0]);
		else if(toIndex < minIndex)
			fromIndex = maxIndex;
		else if(toIndex > maxIndex)
			fromIndex = minIndex;

		int step = toIndex > fromIndex ? 1 : -1;
		m_Selections.Clear();
		while(fromIndex != toIndex + step)
		{
			m_Selections.Add(m_LastTimeShowingBundles[fromIndex]);
			fromIndex += step;
		}
	}
	
	bool IsRectClicked(Rect rect)
	{
		return Event.current.type == EventType.MouseDown && IsMouseOn(rect);
	}
	
	bool Control()
	{
		return (Event.current.control && Application.platform == RuntimePlatform.WindowsEditor) ||
			(Event.current.command && Application.platform == RuntimePlatform.OSXEditor);
	}
	
	bool IsMouseOn(Rect rect)
	{
		return rect.Contains(Event.current.mousePosition);
	}
	
	bool IsFold(string name)
	{
		if(!m_BundleFoldDict.ContainsKey(name))
			m_BundleFoldDict.Add(name, true);
			
		return m_BundleFoldDict[name];
	}
	
	void SetFold(string name, bool isFold)
	{
		m_BundleFoldDict[name] = isFold;
	}

	bool IsNameValid(string name)
	{
		return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z0-9_][A-Za-z0-9_/]*[A-Za-z0-9_]$");
	}
	
	void CreateSceneBundle()
	{
		if(m_Selections.Count == 1)
			newDefBundleTo(m_Selections[0], true);
		else
			newDefBundleTo("", true);
	}
	
	void CreateAssetBundle()
	{
		if(m_Selections.Count == 1)
			newDefBundleTo(m_Selections[0], false);
		else
			newDefBundleTo("", false);
	}
	
	void newDefBundleTo(string parent, bool sceneBundle)
	{
		// Find a new bundle name
		string defBundleName = "EmptyBundle";
		string currentBundleName = defBundleName;
		int index = 0;
		while(BundleManager.GetBundleData(currentBundleName) != null)
		{
			currentBundleName = defBundleName + (++index);
		}
		
		bool created = BundleManager.CreateNewBundle(currentBundleName, parent, sceneBundle);
		if(created)
		{
			if(IsFold( parent ))
				SetFold(parent, false);
			
			m_Selections.Clear();
			m_Selections.Add(currentBundleName);
		}
		
		StartEditBundleName(currentBundleName);
	}
	
	void BuildAll()
	{
		BuildHelper.BuildAll();
		BuildHelper.ExportBMDatasToOutput();
	}
	
	void RebuildAll()
	{
		BuildHelper.RebuildAll();
		BuildHelper.ExportBMDatasToOutput();
	}
	
	void ClearOutputs()
	{
		string outputPath = BuildConfiger.InterpretedOutputPath;
		if( !Directory.Exists(outputPath) )
			return;
		
		foreach(string file in Directory.GetFiles(outputPath) )
		{
			File.Delete(file);
			Debug.Log("Remove " + file);
		}
	}
	
	void BuildSelection()
	{	
		BuildHelper.BuildBundles(m_Selections.ToArray());
		BuildHelper.ExportBMDatasToOutput();
	}
	
	void RebuildSelection()
	{
		foreach(string bundleName in m_Selections)
		{
			BundleBuildState buildState = BundleManager.GetBuildStateOfBundle(bundleName);
			buildState.lastBuildDependencies = null;
		}
		
		BuildHelper.BuildBundles(m_Selections.ToArray());
		BuildHelper.ExportBMDatasToOutput();
	}
	
	bool OnCanRecieve(GUIDragHandler.DragDatas recieverData, GUIDragHandler.DragDatas dragData)
	{
		if(dragData.customDragData == null && dragData.dragPaths.Length != 0)
		{
			foreach(string dragPath in dragData.dragPaths)
			{
				if(dragPath == null)
					continue;
				
				if(BundleManager.CanAddPathToBundle(dragPath, (string)recieverData.customDragData))
					return true;
			}
			
			return false;
		}
		else
		{
			return BundleManager.CanBundleParentTo((string)dragData.customDragData, (string)recieverData.customDragData);
		}
	}
	
	void OnRecieve(GUIDragHandler.DragDatas recieverData, GUIDragHandler.DragDatas dragData)
	{
		if(dragData.customDragData == null && dragData.dragPaths.Length != 0)
		{
			foreach(string dragPath in dragData.dragPaths)
			{
				if(dragPath == null)
					continue;
				
				if(BundleManager.CanAddPathToBundle(dragPath, (string)recieverData.customDragData))
					BundleManager.AddPathToBundle(dragPath, (string)recieverData.customDragData);
			}
		}
		else
			BundleManager.SetParent((string)dragData.customDragData, (string)recieverData.customDragData);
	}
	
	[MenuItem("Window/Bundle Manager")]
	static void Init()
	{
		EditorWindow.GetWindow<BundleTreeWin>("Bundles");
	}
}
