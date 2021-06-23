using System;
using System.IO;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Serializer Test Object", menuName = "Serializer Test Object", order = 1)]
public class SerializerTestObject : ScriptableObject
{
    [System.Serializable]
    public class Data
    {
        public byte[] bytessss;
    }

    public Data data;
    public string text;
}

[CustomEditor(typeof(SerializerTestObject))]
public class SerializerTestObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = ((SerializerTestObject)target);
        DrawDefaultInspector();
        if (GUILayout.Button("1. Data -> Text"))
        {
            t.text = JsonUtility.ToJson(t.data);
        }

        if (GUILayout.Button("2. Text -> File"))
        {
            File.WriteAllText(Path.Combine(Application.dataPath,"byttessss.txt"),t.text);
        }

        if (GUILayout.Button("3. File -> Text"))
        {
            t.text = File.ReadAllText(Path.Combine(Application.dataPath,"byttessss.txt"));
        }

        if (GUILayout.Button("4. Text -> Data"))
        {
            t.data = JsonUtility.FromJson<SerializerTestObject.Data>(t.text);
        }
    }
}