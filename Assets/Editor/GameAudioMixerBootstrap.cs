using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace ModularChess.EditorTools
{
    [InitializeOnLoad]
    static class GameAudioMixerBootstrap
    {
        const string Path = "Assets/Audio/GameAudio.mixer";
        const string ResourcesPath = "Assets/Resources/Audio/GameAudio.mixer";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Stat = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static GameAudioMixerBootstrap()
        {
            EditorApplication.delayCall += Ensure;
        }

        static void Ensure()
        {
            try
            {
                if (AssetDatabase.LoadAssetAtPath<AudioMixer>(Path) == null)
                    CreateMixer();
                CopyToResources();
            }
            catch (Exception)
            {
            }
        }

        static void CreateMixer()
        {
            Type mixerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
            MethodInfo createAtPath = mixerType?.GetMethod("CreateMixerControllerAtPath", Stat);
            if (createAtPath == null)
                return;

            object mixer = createAtPath.Invoke(null, new object[] { Path });
            if (mixer == null)
                return;

            MethodInfo createGroup = mixerType.GetMethod("CreateNewGroup", Instance);
            MethodInfo addChild = mixerType.GetMethod("AddChildToParent", Instance);
            PropertyInfo masterProp = mixerType.GetProperty("masterGroup", Instance)
                ?? mixerType.GetProperty("MasterGroup", Instance);
            if (createGroup == null || addChild == null || masterProp == null)
            {
                Save((UnityEngine.Object)mixer);
                return;
            }

            object master = masterProp.GetValue(mixer);
            object music = createGroup.Invoke(mixer, new object[] { "Music", false });
            object sfx = createGroup.Invoke(mixer, new object[] { "SFX", false });
            if (master != null)
            {
                if (music != null)
                    addChild.Invoke(mixer, new object[] { music, master });
                if (sfx != null)
                    addChild.Invoke(mixer, new object[] { sfx, master });
            }

            ExposeVolume(mixerType, mixer, music, "MusicVol");
            ExposeVolume(mixerType, mixer, sfx, "SfxVol");
            Save((UnityEngine.Object)mixer);
        }

        static void ExposeVolume(Type mixerType, object mixer, object group, string paramName)
        {
            if (group == null)
                return;

            MethodInfo getVolume = group.GetType().GetMethod("GetGUIDForVolume", Instance);
            MethodInfo addExposed = mixerType.GetMethod("AddExposedParameter", Instance);
            Type pathType = Type.GetType("UnityEditor.Audio.AudioGroupParameterPath, UnityEditor");
            if (getVolume == null || addExposed == null || pathType == null)
                return;

            object guid = getVolume.Invoke(group, null);
            object path = Activator.CreateInstance(pathType, group, guid);
            addExposed.Invoke(mixer, new[] { path });
            RenameExposed(mixerType, mixer, guid, paramName);
        }

        static void RenameExposed(Type mixerType, object mixer, object guid, string paramName)
        {
            PropertyInfo exposedProp = mixerType.GetProperty("exposedParameters", Instance);
            Array arr = exposedProp?.GetValue(mixer) as Array;
            if (arr == null || arr.Length == 0)
                return;

            Type element = arr.GetType().GetElementType();
            Array copy = Array.CreateInstance(element, arr.Length);
            FieldInfo guidField = element.GetField("guid", Instance);
            FieldInfo nameField = element.GetField("name", Instance);
            PropertyInfo guidProp = element.GetProperty("guid", Instance);
            PropertyInfo nameProp = element.GetProperty("name", Instance);
            for (int i = 0; i < arr.Length; i++)
            {
                object item = arr.GetValue(i);
                object itemGuid = guidField != null ? guidField.GetValue(item) : guidProp?.GetValue(item);
                if (Equals(itemGuid, guid))
                {
                    if (nameField != null)
                        nameField.SetValue(item, paramName);
                    else
                        nameProp?.SetValue(item, paramName);
                }

                copy.SetValue(item, i);
            }

            exposedProp.SetValue(mixer, copy);
        }

        static void CopyToResources()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(Path) == null)
                return;
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(ResourcesPath) == null)
                AssetDatabase.CopyAsset(Path, ResourcesPath);
        }

        static void Save(UnityEngine.Object mixer)
        {
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
        }
    }
}
