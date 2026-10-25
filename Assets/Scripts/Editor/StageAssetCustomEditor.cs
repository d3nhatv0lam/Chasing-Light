using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChasingLight.Editor
{
    /// <summary>
    /// Custom Inspector for StageAsset providing quick access to the Level Editor
    /// and showing live validation rules directly in the Inspector.
    /// </summary>
    [CustomEditor(typeof(StageAsset))]
    public sealed class StageAssetCustomEditor : UnityEditor.Editor
    {
        private bool _showDefaultInspector = false;

        public override void OnInspectorGUI()
        {
            var stage = (StageAsset)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header Banner
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField($"Stage: {stage.Difficulty} - Stage {stage.StageNumber}", headerStyle);
            EditorGUILayout.Space(4);

            // Buttons: Open in Level Editor & Play This Stage
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.95f);
            if (GUILayout.Button("Open in Level Editor", GUILayout.Height(36)))
            {
                LevelEditorWindow.OpenWithAsset(stage);
            }

            GUI.backgroundColor = new Color(0.35f, 0.8f, 0.35f);
            if (GUILayout.Button("Play This Stage", GUILayout.Height(36)))
            {
                PlayStage(stage);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Summary Information
            EditorGUILayout.LabelField("Stage Summary:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"• ID: {stage.Id}");
            EditorGUILayout.LabelField($"• Grid Dimensions: {stage.Width} x {stage.Height}");
            EditorGUILayout.LabelField($"• Placed Cells: {(stage.Cells != null ? stage.Cells.Count : 0)}");
            EditorGUILayout.LabelField($"• Shadow Delay Steps: {stage.ShadowSpawnDelaySteps}");
            EditorGUILayout.LabelField($"• Mechanisms: {(stage.Switches?.Count ?? 0)} Switches, {(stage.Gates?.Count ?? 0)} Gates, {(stage.Teleports?.Count ?? 0)} Teleports");

            EditorGUILayout.Space(4);

            // Quick Validation Check
            bool isValid = stage.ValidateStage(out var errors, out var warnings);
            if (isValid)
            {
                EditorGUILayout.HelpBox("Stage is valid.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {errors.Count} error(s):", MessageType.Error);
                foreach (var err in errors)
                {
                    EditorGUILayout.LabelField($"• {err}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (warnings.Count > 0)
            {
                foreach (var warn in warnings)
                {
                    EditorGUILayout.LabelField($"⚠ {warn}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Foldout to access raw serialized fields
            _showDefaultInspector = EditorGUILayout.Foldout(_showDefaultInspector, "Raw Properties (Default Inspector)", true);
            if (_showDefaultInspector)
            {
                EditorGUILayout.Space(2);
                DrawDefaultInspector();
            }
        }

        public static void PlayStage(StageAsset asset)
        {
            if (asset == null) return;

            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != "Game")
            {
                if (EditorUtility.DisplayDialog(
                    "Open Game Scene",
                    "Testing this stage requires the 'Game' scene. Switch to 'Assets/Scenes/Game.unity' now?",
                    "Open & Play",
                    "Cancel"))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
                }
                else
                {
                    return;
                }
            }

            var controller = UnityEngine.Object.FindAnyObjectByType<StageController>();
            if (controller != null)
            {
                controller.CurrentStageAsset = asset;
                EditorUtility.SetDirty(controller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

                if (EditorApplication.isPlaying)
                {
                    controller.InitializeStage(asset);
                }
                else
                {
                    EditorApplication.isPlaying = true;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Notice", "Could not find StageController in the Game scene.", "OK");
            }
        }
    }
}
