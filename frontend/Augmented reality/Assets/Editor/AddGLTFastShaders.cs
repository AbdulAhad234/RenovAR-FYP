using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AddGLTFastShaders : EditorWindow
{
    [MenuItem("Tools/Fix Pink AR Materials (Add GLTFast Shaders)")]
    public static void AddShaders()
    {
        string[] shaderNames = new string[] 
        {
            "glTF/PbrMetallicRoughness",
            "glTF/PbrSpecularGlossiness",
            "glTF/Unlit"
        };

        // Load the Graphics Settings
        SerializedObject graphicsSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        int addedCount = 0;

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Shader Fix] Could not find shader: {shaderName}. Make sure GLTFast is installed properly.");
                continue;
            }

            bool alreadyExists = false;
            for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
            {
                SerializedProperty element = alwaysIncludedShaders.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == shader)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                alwaysIncludedShaders.arraySize++;
                SerializedProperty newElement = alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1);
                newElement.objectReferenceValue = shader;
                Debug.Log($"[Shader Fix] Added {shaderName} to Always Included Shaders.");
                addedCount++;
            }
            else
            {
                Debug.Log($"[Shader Fix] {shaderName} is already in Always Included Shaders.");
            }
        }

        if (addedCount > 0)
        {
            graphicsSettings.ApplyModifiedProperties();
            Debug.Log("[Shader Fix] SUCCESS! The required GLTFast shaders have been added. You can now build your project without pink materials.");
        }
        else
        {
            Debug.Log("[Shader Fix] Everything is already set up correctly. No new shaders needed to be added.");
        }
    }
}
