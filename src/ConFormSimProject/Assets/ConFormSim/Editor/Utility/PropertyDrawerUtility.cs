using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace ConFormSim.Editor
{
	public class PropertyDrawerUtility
	{
		public static T GetActualObjectForSerializedProperty<T>(FieldInfo fieldInfo, SerializedProperty property, string[] ignoreInPath=null) where T : class
		{
			T obj;
			try
			{
				obj = (T) fieldInfo.GetValue(property.serializedObject.targetObject);
			}
			catch (ArgumentException)
			{
				object targetObject = GetParent(property, ignoreInPath);
				obj = targetObject as T;
			}
	
			return obj;
		}

		public static object GetParent(SerializedProperty prop, string[] ignoreInPath=null)
		{
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			if (ignoreInPath != null)
			{
				foreach(string s in ignoreInPath)
				path = path.Replace(s, String.Empty);
			}
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach(var element in path.Split('.'))
			{
				if(element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[","").Replace("]",""));
					obj = GetValue(obj, elementName, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}
			return obj;
		}
		
		public static object GetValue(object source, string name)
		{
			if(source == null)
				return null;
			var type = source.GetType();
			var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			FieldInfo[] fieldInfos = type.GetFields();
			PropertyInfo[] propInfos = type.GetProperties();
			if(f == null)
			{
				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if(p == null)
					return null;
				return p.GetValue(source, null);
			}
			return f.GetValue(source);
		}
		
		public static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as IEnumerable<object>;
			var enm = enumerable.GetEnumerator();
			while(index-- >= 0)
				enm.MoveNext();
			return enm.Current;
		}		
	}
}
