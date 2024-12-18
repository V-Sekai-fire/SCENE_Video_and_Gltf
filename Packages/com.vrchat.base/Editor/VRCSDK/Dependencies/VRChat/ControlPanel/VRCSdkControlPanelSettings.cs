﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.Core;
using VRC.SDKBase.Editor;

// This file handles the Settings tab of the SDK Panel

public partial class VRCSdkControlPanel : EditorWindow
{
    bool UseDevApi
    {
        get
        {
            return VRC.Core.API.GetApiUrl() == VRC.Core.API.devApiUrl;
        }
    }
    
    Vector2 settingsScroll;

    void ShowSettings()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.Width(SdkWindowWidth - 8));

        EditorGUILayout.BeginVertical(boxGuiStyle);
        EditorGUILayout.LabelField("Developer", EditorStyles.boldLabel);

        VRCSettings.DisplayAdvancedSettings = EditorGUILayout.ToggleLeft("Show Extra Options on account page", VRCSettings.DisplayAdvancedSettings);
        
        bool prevDisplayHelpBoxes = VRCSettings.DisplayHelpBoxes;
        VRCSettings.DisplayHelpBoxes = EditorGUILayout.ToggleLeft("Show Help Boxes on SDK components", VRCSettings.DisplayHelpBoxes);
        if (VRCSettings.DisplayHelpBoxes != prevDisplayHelpBoxes)
        {
            Editor[] editors = (Editor[])Resources.FindObjectsOfTypeAll<Editor>();
            for (int i = 0; i < editors.Length; i++)
            {
                editors[i].Repaint();
            }
        }

        // API logging
        {
            bool isLoggingEnabled = UnityEditor.EditorPrefs.GetBool("apiLoggingEnabled");
            bool enableLogging = EditorGUILayout.ToggleLeft("API Logging Enabled", isLoggingEnabled);
            if (enableLogging != isLoggingEnabled)
            {
                if (enableLogging)
                    VRC.Core.Logger.AddDebugLevel(DebugLevel.API);
                else
                    VRC.Core.Logger.RemoveDebugLevel(DebugLevel.API);

                UnityEditor.EditorPrefs.SetBool("apiLoggingEnabled", enableLogging);
            }
        }

        EditorGUILayout.Space();

        // DPID based mipmap generation
        bool prevDpidMipmaps = VRCSettings.DpidMipmaps;
        GUIContent dpidContent = new GUIContent("Override Kaiser mipmapping with Detail-Preserving Image Downscaling (BETA)", 
                "Use a state of the art algorithm (DPID) for mipmap generation when Kaiser is selected. This can improve the quality of mipmaps.");
        VRCSettings.DpidMipmaps = EditorGUILayout.ToggleLeft(dpidContent, VRCSettings.DpidMipmaps);

        bool prevDpidConservative = VRCSettings.DpidConservative;
        GUIContent dpidConservativeContent = new GUIContent("Use conservative settings for DPID mipmapping", 
                "Use conservative settings for DPID mipmapping. This can avoid issues with over-emphasis of details.");
        VRCSettings.DpidConservative = EditorGUILayout.ToggleLeft(dpidConservativeContent, VRCSettings.DpidConservative);
        
        // When DPID setting changed, mark all textures as dirty
        if (VRCSettings.DpidMipmaps != prevDpidMipmaps || 
                (VRCSettings.DpidMipmaps && VRCSettings.DpidConservative != prevDpidConservative))
        {
            VRC.Core.Logger.Log("DPID mipmaps setting changed, marking all textures as dirty");
            string[] guids = AssetDatabase.FindAssets("t:Texture");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.mipmapFilter == TextureImporterMipFilter.KaiserFilter)
                {
                    importer.SaveAndReimport();
                }
            }
        }
        
        EditorGUILayout.Space();

        // Running VRChat constraints in edit mode
        bool prevVrcConstraintsInEditMode = VRCSettings.VrcConstraintsInEditMode;
        GUIContent vrcConstraintsInEditModeContent = new GUIContent("Execute VRChat Constraints in Edit Mode", 
            "Allow VRChat Constraints to run while Unity is in Edit mode.");
        VRCSettings.VrcConstraintsInEditMode = EditorGUILayout.ToggleLeft(vrcConstraintsInEditModeContent, prevVrcConstraintsInEditMode);

        if (VRCSettings.VrcConstraintsInEditMode != prevVrcConstraintsInEditMode)
        {
            VRC.Dynamics.VRCConstraintManager.CanExecuteConstraintJobsInEditMode = VRCSettings.VrcConstraintsInEditMode;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        ShowSettingsOptionsForBuilders();

        // debugging
        if (APIUser.CurrentUser != null && APIUser.CurrentUser.hasSuperPowers)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(boxGuiStyle);

            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);

            // All logging
            {
                bool isLoggingEnabled = UnityEditor.EditorPrefs.GetBool("allLoggingEnabled");
                bool enableLogging = EditorGUILayout.ToggleLeft("All Logging Enabled", isLoggingEnabled);
                if (enableLogging != isLoggingEnabled)
                {
                    if (enableLogging)
                        VRC.Core.Logger.AddDebugLevel(DebugLevel.All);
                    else
                        VRC.Core.Logger.RemoveDebugLevel(DebugLevel.All);

                    UnityEditor.EditorPrefs.SetBool("allLoggingEnabled", enableLogging);
                }
            }
            EditorGUILayout.EndVertical();
        }
        else
        {
            // if (UnityEditor.EditorPrefs.GetBool("apiLoggingEnabled"))
            //     UnityEditor.EditorPrefs.SetBool("apiLoggingEnabled", false);
            if (UnityEditor.EditorPrefs.GetBool("allLoggingEnabled"))
                UnityEditor.EditorPrefs.SetBool("allLoggingEnabled", false);
        }


        if (APIUser.CurrentUser != null)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(boxGuiStyle);

            // custom vrchat install location
            OnVRCInstallPathGUI();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    static void OnVRCInstallPathGUI()
    {
        EditorGUILayout.LabelField("VRChat Client", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Installed Client Path: ", clientInstallPath);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("");
        if (GUILayout.Button("Edit"))
        {
            string initPath = "";
            if (!string.IsNullOrEmpty(clientInstallPath))
                initPath = clientInstallPath;

            clientInstallPath = EditorUtility.OpenFilePanel("Choose VRC Client Exe", initPath, "exe");
            SDKClientUtilities.SetVRCInstallPath(clientInstallPath);
        }
        if (GUILayout.Button("Revert to Default"))
        {
            clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();
    }
}
