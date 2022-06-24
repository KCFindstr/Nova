using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    public class InputManager : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string BindingsFilePath => Path.Combine(InputFilesDirectory, "bindings.json");

        public InputActionAsset defaultActionAsset;
        public ActionAssetData actionAsset { get; private set; }

        private void Load()
        {
            if (File.Exists(BindingsFilePath))
            {
                var json = File.ReadAllText(BindingsFilePath);
                actionAsset = new ActionAssetData(InputActionAsset.FromJson(json));
            }
        }

        public void Save()
        {
            var json = actionAsset.data.ToJson();
            Directory.CreateDirectory(InputFilesDirectory);
            File.WriteAllText(BindingsFilePath, json);
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            Save();
        }

        /// <summary>
        /// Must be called before accessing any other members.
        /// </summary>
        public void Init()
        {
            if (actionAsset != null) return;

            EnhancedTouchSupport.Enable();
            actionAsset = new ActionAssetData(InputActionAsset.FromJson(defaultActionAsset.ToJson()));
            Load();
        }

        public void SetEnableGroup(AbstractKeyGroup group)
        {
            foreach (var actionMap in actionAsset.GetActionMaps(group))
                actionMap.Enable();

            foreach (var actionMap in actionAsset.GetActionMaps(AbstractKeyGroup.All ^ group))
                actionMap.Disable();
        }

        public void SetEnable(AbstractKey key, bool enable)
        {
            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Nova: Missing action key: {key}");
                return;
            }

            if (enable)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }

        /// <summary>
        /// Checks whether an abstract key is triggered.<br/>
        /// Only activates once. To check whether a key is held, use <see cref="InputAction.IsPressed"/>.
        /// </summary>
        public bool IsTriggered(AbstractKey key)
        {
#if !UNITY_EDITOR
            if (ActionAssetData.IsEditorOnly(key))
            {
                return false;
            }
#endif

            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Nova: Missing action key: {key}");
                return false;
            }

            return action.triggered;
        }

        public void SetActionAsset(InputActionAsset asset)
        {
            var enabledState = new KeyStatus();
            GetEnabledState(enabledState);
            actionAsset?.data.Disable();
            actionAsset = new ActionAssetData(InputActionAsset.FromJson(asset.ToJson()));
            SetEnabledState(enabledState);
        }

        public void GetEnabledState(KeyStatus status)
        {
            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                if (actionAsset.TryGetAction(key, out var action))
                {
                    status[key] = action.enabled;
                }
            }
        }

        public void SetEnabledState(KeyStatus status)
        {
            foreach (var key in status.Keys)
            {
                SetEnable(key, status[key]);
            }
        }
    }
}