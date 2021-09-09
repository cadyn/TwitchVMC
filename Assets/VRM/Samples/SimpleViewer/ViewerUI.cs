﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;
using VRMShaders;


namespace VRM.SimpleViewer
{


    public class ViewerUI : MonoBehaviour
    {
        #region UI
        [SerializeField]
        Text m_version = default;

        [SerializeField]
        Button m_open = default;

        [SerializeField]
        Toggle m_enableLipSync = default;

        [SerializeField]
        Toggle m_enableAutoBlink = default;

        [SerializeField]
        Toggle m_useUrpMaterial = default;
        #endregion

        [SerializeField]
        HumanPoseTransfer m_src = default;

        [SerializeField]
        GameObject m_target = default;

        [SerializeField]
        GameObject Root = default;

        [SerializeField]
        Button m_reset = default;

        [SerializeField]
        TextAsset m_motion;

        [Serializable]
        class TextFields
        {
            [SerializeField, Header("Info")]
            Text m_textModelTitle = default;
            [SerializeField]
            Text m_textModelVersion = default;
            [SerializeField]
            Text m_textModelAuthor = default;
            [SerializeField]
            Text m_textModelContact = default;
            [SerializeField]
            Text m_textModelReference = default;
            [SerializeField]
            RawImage m_thumbnail = default;

            [SerializeField, Header("CharacterPermission")]
            Text m_textPermissionAllowed = default;
            [SerializeField]
            Text m_textPermissionViolent = default;
            [SerializeField]
            Text m_textPermissionSexual = default;
            [SerializeField]
            Text m_textPermissionCommercial = default;
            [SerializeField]
            Text m_textPermissionOther = default;

            [SerializeField, Header("DistributionLicense")]
            Text m_textDistributionLicense = default;
            [SerializeField]
            Text m_textDistributionOther = default;

            public void Start()
            {
                m_textModelTitle.text = "";
                m_textModelVersion.text = "";
                m_textModelAuthor.text = "";
                m_textModelContact.text = "";
                m_textModelReference.text = "";

                m_textPermissionAllowed.text = "";
                m_textPermissionViolent.text = "";
                m_textPermissionSexual.text = "";
                m_textPermissionCommercial.text = "";
                m_textPermissionOther.text = "";

                m_textDistributionLicense.text = "";
                m_textDistributionOther.text = "";
            }

            public async Task UpdateMetaAsync(VRMImporterContext context)
            {
                var meta = await context.ReadMetaAsync(new TaskCaller(), true);

                m_textModelTitle.text = meta.Title;
                m_textModelVersion.text = meta.Version;
                m_textModelAuthor.text = meta.Author;
                m_textModelContact.text = meta.ContactInformation;
                m_textModelReference.text = meta.Reference;

                m_textPermissionAllowed.text = meta.AllowedUser.ToString();
                m_textPermissionViolent.text = meta.ViolentUssage.ToString();
                m_textPermissionSexual.text = meta.SexualUssage.ToString();
                m_textPermissionCommercial.text = meta.CommercialUssage.ToString();
                m_textPermissionOther.text = meta.OtherPermissionUrl;

                m_textDistributionLicense.text = meta.LicenseType.ToString();
                m_textDistributionOther.text = meta.OtherLicenseUrl;

                m_thumbnail.texture = meta.Thumbnail;
            }
        }

        [SerializeField]
        TextFields m_texts = default;

        [Serializable]
        class UIFields
        {
            [SerializeField]
            Toggle ToggleMotionTPose = default;

            [SerializeField]
            Toggle ToggleMotionBVH = default;

            [SerializeField]
            ToggleGroup ToggleMotion = default;

            Toggle m_activeToggleMotion = default;

            public void UpdateToggle(Action onBvh, Action onTPose)
            {
                var value = ToggleMotion.ActiveToggles().FirstOrDefault();
                if (value == m_activeToggleMotion)
                    return;

                m_activeToggleMotion = value;
                if (value == ToggleMotionTPose)
                {
                    onTPose();
                }
                else if (value == ToggleMotionBVH)
                {
                    onBvh();
                }
                else
                {
                    Debug.Log("motion: no toggle");
                }
            }
        }
        [SerializeField]
        UIFields m_ui = default;

        [SerializeField]
        HumanPoseClip m_pose = default;

        private void Reset()
        {
            var buttons = GameObject.FindObjectsOfType<Button>();
            m_open = buttons.First(x => x.name == "Open");

            m_reset = buttons.First(x => x.name == "ResetSpringBone");

            var toggles = GameObject.FindObjectsOfType<Toggle>();
            m_enableLipSync = toggles.First(x => x.name == "EnableLipSync");
            m_enableAutoBlink = toggles.First(x => x.name == "EnableAutoBlink");

            var texts = GameObject.FindObjectsOfType<Text>();
            m_version = texts.First(x => x.name == "Version");

            m_src = GameObject.FindObjectOfType<HumanPoseTransfer>();

            m_target = GameObject.FindObjectOfType<TargetMover>().gameObject;
        }

        HumanPoseTransfer m_loaded;
        VRMBlendShapeProxy m_proxy;

        AIUEO m_lipSync;
        bool m_enableLipSyncValue;
        bool EnableLipSyncValue
        {
            set
            {
                if (m_enableLipSyncValue == value) return;
                m_enableLipSyncValue = value;
                if (m_lipSync != null)
                {
                    m_lipSync.enabled = m_enableLipSyncValue;
                }
            }
        }

        Blinker m_blink;
        bool m_enableBlinkValue;
        bool EnableBlinkValue
        {
            set
            {
                if (m_blink == value) return;
                m_enableBlinkValue = value;
                if (m_blink != null)
                {
                    m_blink.enabled = m_enableBlinkValue;
                }
            }
        }

        private void Start()
        {
            m_version.text = string.Format("VRMViewer {0}.{1}",
                VRMVersion.MAJOR, VRMVersion.MINOR);
            m_open.onClick.AddListener(OnOpenClicked);

            m_reset.onClick.AddListener(OnResetClicked);

            // load initial bvh
            if (m_motion != null)
            {
                LoadMotion(m_motion.text);
            }

            string[] cmds = System.Environment.GetCommandLineArgs();
            if (cmds.Length > 1)
            {
                LoadModelAsync(cmds[1]);
            }

            m_texts.Start();
        }

        private void LoadMotion(string source)
        {
            var context = new UniHumanoid.BvhImporterContext();
            context.Parse("tmp.bvh", source);
            context.Load();
            SetMotion(context.Root.GetComponent<HumanPoseTransfer>());
        }

        private void Update()
        {
            EnableLipSyncValue = m_enableLipSync.isOn;
            EnableBlinkValue = m_enableAutoBlink.isOn;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Root != null) Root.SetActive(!Root.activeSelf);
            }

            m_ui.UpdateToggle(EnableBvh, EnableTPose);

            if (m_proxy != null)
            {
                m_proxy.Apply();
            }
        }

        void EnableBvh()
        {
            if (m_loaded != null)
            {
                m_loaded.Source = m_src;
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
            }
        }

        void EnableTPose()
        {
            if (m_loaded != null)
            {
                m_loaded.PoseClip = m_pose;
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseClip;
            }
        }

        void OnResetClicked()
        {
            if (m_loaded == null)
            {
                return;
            }
            foreach (var spring in m_loaded.GetComponentsInChildren<VRMSpringBone>())
            {
                spring.Setup();
            }
        }

        void OnOpenClicked()
        {
#if UNITY_STANDALONE_WIN
            var path = FileDialogForWindows.FileDialog("open VRM", "vrm", "glb", "bvh", "gltf", "zip");
#elif UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Open VRM", "", "vrm");
#else
            var path = Application.dataPath + "/default.vrm";
#endif
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".gltf":
                case ".glb":
                case ".vrm":
                case ".zip":
                    LoadModelAsync(path);
                    break;

                case ".bvh":
                    LoadMotion(path);
                    break;
            }
        }

        static IMaterialDescriptorGenerator GetGltfMaterialGenerator(bool useUrp)
        {
            if (useUrp)
            {
                return new GltfUrpMaterialDescriptorGenerator();
            }
            else
            {
                return new GltfMaterialDescriptorGenerator();
            }
        }

        static IMaterialDescriptorGenerator GetVrmMaterialGenerator(bool useUrp, VRM.glTF_VRM_extensions vrm)
        {
            if (useUrp)
            {
                return new VRM.VRMUrpMaterialDescriptorGenerator(vrm);
            }
            else
            {
                return new VRM.VRMMaterialDescriptorGenerator(vrm);
            }
        }

        async void LoadModelAsync(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Debug.LogFormat("{0}", path);
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".vrm":
                    {
                        var data = new GlbFileParser(path).Parse();
                        var vrm = new VRMData(data);
                        using (var context = new VRMImporterContext(vrm, materialGenerator: GetVrmMaterialGenerator(m_useUrpMaterial.isOn, vrm.VrmExtension)))
                        {
                            await m_texts.UpdateMetaAsync(context);
                            var loaded = await context.LoadAsync();
                            loaded.EnableUpdateWhenOffscreen();
                            loaded.ShowMeshes();
                            SetModel(loaded.gameObject);
                        }
                        break;
                    }

                case ".glb":
                    {
                        var data = new GlbFileParser(path).Parse();

                        var context = new UniGLTF.ImporterContext(data, materialGenerator: GetGltfMaterialGenerator(m_useUrpMaterial.isOn));
                        var loaded = context.Load();
                        loaded.EnableUpdateWhenOffscreen();
                        loaded.ShowMeshes();
                        SetModel(loaded.gameObject);
                        break;
                    }

                case ".gltf":
                    {
                        var data = new GltfFileWithResourceFilesParser(path).Parse();

                        var context = new UniGLTF.ImporterContext(data, materialGenerator: GetGltfMaterialGenerator(m_useUrpMaterial.isOn));
                        var loaded = context.Load();
                        loaded.EnableUpdateWhenOffscreen();
                        loaded.ShowMeshes();
                        SetModel(loaded.gameObject);
                        break;
                    }
                case ".zip":
                    {
                        var data = new ZipArchivedGltfFileParser(path).Parse();

                        var context = new UniGLTF.ImporterContext(data, materialGenerator: GetGltfMaterialGenerator(m_useUrpMaterial.isOn));
                        var loaded = context.Load();
                        loaded.EnableUpdateWhenOffscreen();
                        loaded.ShowMeshes();
                        SetModel(loaded.gameObject);
                        break;
                    }

                default:
                    Debug.LogWarningFormat("unknown file type: {0}", path);
                    break;
            }
        }

        void SetModel(GameObject go)
        {
            // cleanup
            var loaded = m_loaded;
            m_loaded = null;

            if (loaded != null)
            {
                Debug.LogFormat("destroy {0}", loaded);
                GameObject.Destroy(loaded.gameObject);
            }

            if (go != null)
            {
                var lookAt = go.GetComponent<VRMLookAtHead>();
                if (lookAt != null)
                {
                    m_loaded = go.AddComponent<HumanPoseTransfer>();
                    m_loaded.Source = m_src;
                    m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;

                    m_lipSync = go.AddComponent<AIUEO>();
                    m_blink = go.AddComponent<Blinker>();

                    lookAt.Target = m_target.transform;
                    lookAt.UpdateType = UpdateType.LateUpdate; // after HumanPoseTransfer's setPose
                }

                var animation = go.GetComponent<Animation>();
                if (animation && animation.clip != null)
                {
                    animation.Play(animation.clip.name);
                }

                m_proxy = go.GetComponent<VRMBlendShapeProxy>();
            }
        }

        void SetMotion(HumanPoseTransfer src)
        {
            m_src = src;
            src.GetComponent<Renderer>().enabled = false;

            EnableBvh();
        }
    }
}
