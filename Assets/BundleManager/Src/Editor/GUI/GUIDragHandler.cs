using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

internal class GUIDragHandler
{
	public class DragDatas
	{
		public string[] dragPaths = null;
		public UnityEngine.Object[] dragObjects = null;
		public object customDragData = null;
	}
	
	public enum DragState{Receiving, Rejecting, Received, None};
	
	public delegate bool CanRecieveDragDelegate(DragDatas recieverData, DragDatas dragData);
	public delegate void RecieveDelegate(DragDatas recieverData, DragDatas dragData);
	
	public string dragIdentifier = "";
	public Rect detectRect;
	public bool dragAble = true;
	public CanRecieveDragDelegate canRecieveCallBack = null;
	public RecieveDelegate reciveDragCallBack = null;
	public DragAndDropVisualMode dragVisualMode = DragAndDropVisualMode.Move;
	
	public DragDatas dragData = new DragDatas();
	
	private HashSet<string> recieveIdentifiers = new HashSet<string>(){null};
	
	// Only called in OnGUI()
	// Return value indecate if it can recieve some thing
	public DragState GUIDragUpdate()
	{
		if(!detectRect.Contains(Event.current.mousePosition))
			return DragState.None;
		
		switch(Event.current.type)
		{
		case EventType.MouseDrag:
			startDrage();
			break;
		case EventType.DragPerform:
			if( tryReciveDrag() )
				return DragState.Received;
			break;
		case EventType.DragUpdated:
			if( dectectRecieve() )
				return DragState.Receiving;
			else
				return DragState.Rejecting;
		}
		
		return DragState.None;
	}
	
	public bool AddRecieveIdentifier(string identify)
	{
		if(recieveIdentifiers.Contains(identify))
		{
			return false;
		}
		else
		{
			recieveIdentifiers.Add(identify);
			return true;
		}
	}
	
	private void startDrage()
	{
		if(!dragAble)
			return;
		
		DragAndDrop.PrepareStartDrag();
		
		DragAndDrop.paths = new string[]{};
		DragAndDrop.objectReferences = new Object[]{};
		
		if(dragIdentifier != "")
			DragAndDrop.SetGenericData("Drag Identifier", (object)dragIdentifier);
		
		if(dragData.customDragData != null)
			DragAndDrop.SetGenericData("CustomDrag Data", (object)dragData.customDragData);
		
		DragAndDrop.StartDrag((string)dragData.customDragData);
		
		Event.current.Use();
	}
	
	private bool dectectRecieve()
	{
		if(canReciveThisDrag())
		{
			DragAndDrop.visualMode = dragVisualMode;
			return true;
		}
		else
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			return false;
		}
	}
	
	private bool tryReciveDrag()
	{
		if(canReciveThisDrag())
		{
			reciveDragCallBack(dragData, getCurrentDragData());
			DragAndDrop.AcceptDrag();
			return true;
		}
		
		return false;
	}
	
	private bool canReciveThisDrag()
	{
		string curIdentifier = (string)DragAndDrop.GetGenericData("Drag Identifier");
		return recieveIdentifiers.Contains(curIdentifier) 
			&& canRecieveCallBack(dragData, getCurrentDragData());
	}
	
	private DragDatas getCurrentDragData()
	{
		DragDatas dragData = new DragDatas();
		dragData.dragPaths = DragAndDrop.paths;
		dragData.dragObjects = DragAndDrop.objectReferences;
		dragData.customDragData = DragAndDrop.GetGenericData("CustomDrag Data");
		
		return dragData;
	}
}
