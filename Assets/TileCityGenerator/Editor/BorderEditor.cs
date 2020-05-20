using UnityEngine;
using UnityEditor;
using System.Collections;

/** \brief Editor customization for Border script*/
[CustomEditor(typeof(Border))]
public class BorderEditor :Editor
{
	/** Update with each inspector event*/
    public override void OnInspectorGUI () 
	{
		serializedObject.Update();
		
		EditorGUILayout.LabelField("Number of connections by side");
		
		SerializedProperty newSize = serializedObject.FindProperty("size");
		EditorGUILayout.PropertyField(newSize);	
		
		SerializedProperty leftside = serializedObject.FindProperty("left");
		SerializedProperty topside = serializedObject.FindProperty("top");
        SerializedProperty rightside = serializedObject.FindProperty("right");
		SerializedProperty downside = serializedObject.FindProperty("down");
		
		SerializedProperty leftlist = leftside.FindPropertyRelative("values");
		SerializedProperty toplist = topside.FindPropertyRelative("values");
		SerializedProperty rightlist = rightside.FindPropertyRelative("values");
		SerializedProperty downlist = downside.FindPropertyRelative("values");
		
		EditorGUILayout.LabelField("Select tile connections");
		
		//set border global size
		leftlist.FindPropertyRelative("Array.size").intValue = newSize.intValue;
		toplist.FindPropertyRelative("Array.size").intValue = newSize.intValue;
		rightlist.FindPropertyRelative("Array.size").intValue = newSize.intValue;
		downlist.FindPropertyRelative("Array.size").intValue = newSize.intValue;
		
		//display list values		        		
		EditorGUIUtility.labelWidth = 2;
		EditorGUILayout.BeginVertical();
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		GUILayout.FlexibleSpace();
		for (int i = 0; i < newSize.intValue ; ++i)
		{
			toplist.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.Toggle(" ",toplist.GetArrayElementAtIndex(i).boolValue);
        }
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
        
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		for (int i = 0; i < newSize.intValue ; ++i)
		{
			leftlist.GetArrayElementAtIndex((newSize.intValue - 1) - i).boolValue = EditorGUILayout.Toggle(" ",leftlist.GetArrayElementAtIndex((newSize.intValue - 1) - i).boolValue);
        }
		EditorGUILayout.EndVertical();
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginVertical();
        for (int i = 0; i < newSize.intValue ; ++i)
        {			
			rightlist.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.Toggle(" ",rightlist.GetArrayElementAtIndex(i).boolValue);
        }
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
        
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		GUILayout.FlexibleSpace();
		for (int i = 0; i < newSize.intValue ; ++i)
		{
			downlist.GetArrayElementAtIndex((newSize.intValue - 1) - i).boolValue = EditorGUILayout.Toggle(" ",downlist.GetArrayElementAtIndex((newSize.intValue - 1) - i).boolValue);
        }
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
        
		EditorGUILayout.EndVertical();
        
		serializedObject.ApplyModifiedProperties();
    }
}